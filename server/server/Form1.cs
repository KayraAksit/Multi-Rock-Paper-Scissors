#region IMPORT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
#endregion

namespace server
{
    
    public partial class Form1 : Form
    {
        #region VARIABLES
        //Initialize the dictionary to store player names
        List<PlayerInfo> players = new List<PlayerInfo>();

        const int maxClients = 4; //Define max number of players playing simultaneously
        int activeMaxClients = maxClients; //Define max number of players playing simultaneously in the current round

        Socket serverSocket;
        Dictionary<string, PlayerStatistics> playerStats;

        bool isSecondRound = false;
        bool terminating = false;
        bool listening = false;
        #endregion

        #region INIT, LISTEN, ACCEPT
        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            playerStats = ReadWinCountsFromFile();
            UpdateLeaderboard(playerStats);
            BroadcastLeaderboard(playerStats);
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;

            if(Int32.TryParse(textBox_port.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);
                serverSocket.Listen(3);

                listening = true;
                button_listen.Enabled = false;
                textBox_message.Enabled = true;
                button_send.Enabled = true;

                Thread acceptThread = new Thread(Accept);
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while (listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();

                    // Receive the username from the client
                    byte[] buffer = new byte[64]; // assuming username will not exceed 64 bytes
                    int received = newClient.Receive(buffer);
                    string username = Encoding.Default.GetString(buffer, 0, received);

                    // Check if the username is already taken
                    if(players.Any(item => item.name == username))
                    {
                        // Notify the client that the username is already taken, ask for trying with a new one and disconnect the client
                        string usernameTakenMsg = "Username is already taken. Please try with a new one.\n";
                        byte[] usernameTakenBuffer = Encoding.Default.GetBytes(usernameTakenMsg);
                        newClient.Send(usernameTakenBuffer);
                        newClient.Close();
                    }
                    else
                    {
                        Thread receiveThread = new Thread(() => MyReceive(newClient));
                        receiveThread.Start();

                        PlayerStatistics playerStatistics = ReadWinCountsFromFile().TryGetValue(username, out playerStatistics) ? playerStatistics : new PlayerStatistics(0, 0, 0);
                        PlayerInfo newPlayer = new PlayerInfo(username, newClient, playerStatistics);
                        SendLeaderboard(playerStats, newPlayer);


                        // Check if the game has enough players
                        int inGameCount = players.Count(p => p.isInGame == true);
                        if(inGameCount < activeMaxClients && !isSecondRound)
                        {
                            NotifyPlayerEnteredGame(username);

                            newPlayer.isInGame = true;
                            logs.AppendText(username + " has connected to the game.\n");

                            //Check if there are enough players to start the game now
                            if (inGameCount +1 == activeMaxClients)
                            {
                                players.Add(newPlayer);
                                foreach (PlayerInfo pInf in players) //Notify players of game start
                                {
                                    NotifyClientGameStart(pInf.socket);
                                }

                                //Test play the game
                                Thread gameThread = new Thread(new ThreadStart(PlayTheGame));
                                gameThread.Start();

                            }
                            else {
                                players.Add(newPlayer);
                                foreach (PlayerInfo pInf in players) //Notify players of not enough players
                                {
                                    NotifyClientNotEnoughPlayers(pInf.socket);
                                }
                            }
                        }
                        else
                        {
                            logs.AppendText("A player entered the waiting queue.\n");
                            NotifyClientQueueStatus(newClient);
                            players.Add(newPlayer);
                        }
                    }
                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }
                }
            }
        }
        #endregion

        #region NOTIFICATIONS

        private void BroadCastMessage(string message)
        {
            byte[] messageBuffer = Encoding.Default.GetBytes(message);
            foreach (PlayerInfo pInf in players)
            {
                pInf.socket.Send(messageBuffer);
            }
        }
        private void NotifyClientGameStart(Socket client)
        {
            string gameStartMsg = "The game will start in shortly!\n";
            byte[] gameStartBuffer = Encoding.Default.GetBytes(gameStartMsg);
            client.Send(gameStartBuffer);
        }

        private void NotifyPlayerEnteredGame(string username)
        {
            string plEnteredMsg = username + " has entered the game!\n";
            byte[] plEnteredBuffer = Encoding.Default.GetBytes(plEnteredMsg);
            foreach (PlayerInfo pInf in players)
            {
                if (pInf.name != username && pInf.isInGame)
                {
                    pInf.socket.Send(plEnteredBuffer);
                }
            }
        }

        private void NotifyClientNotEnoughPlayers(Socket client)
        {
            string gameStartMsg = "Not Enough Players to start the game, waiting for more players\n";
            byte[] gameStartBuffer = Encoding.Default.GetBytes(gameStartMsg);
            client.Send(gameStartBuffer);
        }

        private void NotifyClientQueueStatus(Socket client)
        {
            string queueMsg = "You are in the waiting queue...\n";
            byte[] queueBuffer = Encoding.Default.GetBytes(queueMsg);
            client.Send(queueBuffer);
        }
        #endregion

        #region GAMEPLAY
        //Second boolean argumant is for not taking anyone from the waiting queue when the second round starts
        private bool StartCountDown(int playerNum, bool isSecondRound)
        {
            int countdownTime = 15; // 10 seconds
            while (countdownTime > 0)
            {
                Thread.Sleep(1000); // wait for 1 second
                countdownTime--;
                // Check current inGame count
                int inGameCount = players.Count(p => p.isInGame == true);

                while (inGameCount < playerNum && !isSecondRound)
                {
                    // Check if there are any players in the waiting queue
                    var waitingPlayer = players.FirstOrDefault(p => p.isInGame == false && p.isLeft == false);

                    if (waitingPlayer != null)// Check if waiting players can join evaluates to false if there is no waiting player
                    {
                        waitingPlayer.isInGame = true;
                        NotifyClientGameStart(waitingPlayer.socket);
                        inGameCount = players.Count(p => p.isInGame == true);
                    }
                    else
                    {
                        break;
                    }
                }

                if (inGameCount != playerNum && !isSecondRound)
                {
                    string notEnoughPlayersMsg = "Player left. Waiting for more players.\n";
                    byte[] notEnoughPlayersBuffer = Encoding.Default.GetBytes(notEnoughPlayersMsg);
                    foreach (PlayerInfo pInf in players)
                    {
                        if (pInf.isInGame)
                        {
                            pInf.socket.Send(notEnoughPlayersBuffer);
                        }
                    }
                    return false;
                }
                else
                {
                    string timeLeftMsg = "Game starting in " + countdownTime + " seconds!\n";
                    byte[] timeLeftBuffer = Encoding.Default.GetBytes(timeLeftMsg);
                    foreach (PlayerInfo pInf in players)
                    {
                        if (pInf.isInGame)
                        {
                            pInf.socket.Send(timeLeftBuffer);
                        }
                    }
                }
            }
            return true;
        }

        private void PlayTheGame() //isSecondRound will be passed into the StartCountDown to prevent taking players from the waiting queue
        {

            //Someone disconnected during the countdown therefore the game cannot be played
            if (!StartCountDown(activeMaxClients, isSecondRound))
            {
                return;
            }

            string askInp = "Please enter your move you have 10 seconds";
            byte[] askInpBuffer = Encoding.Default.GetBytes(askInp);
            foreach (PlayerInfo pInf in players)
            {
                if (pInf.isInGame)
                {
                    pInf.socket.Send(askInpBuffer);
                }
            }

            bool isAllInputTaken = true;
            int countdownTime = 60; // 10 seconds
            while (countdownTime > 0)
            {
                Thread.Sleep(1000); // wait for 1 second
                countdownTime--;
                // Send message to clients to notify remaining time
                string timeLeftMsg = "Time left: " + countdownTime + " seconds\n";
                byte[] timeLeftBuffer = Encoding.Default.GetBytes(timeLeftMsg);

                int inGameCount = players.Count(p => p.isInGame == true);

                //Everybody disconnected except one player hükmen galip
                if(inGameCount == 1)
                {
                    var winner = players.FirstOrDefault(p => p.isInGame == true);
                    string winMsg = winner.name + " has won the game by default!\n";
                    UpdateWinCount(winner.name);
                    byte[] winBuffer = Encoding.Default.GetBytes(winMsg);

                    //Announce the winner to all players
                    foreach (PlayerInfo pInf in players)
                    {
                        pInf.socket.Send(winBuffer);
                    }
                    //One player won, restart the game if enough players left
                    isSecondRound = false;
                    activeMaxClients = maxClients;
                    int currentPlayer = players.Count;
                    if (currentPlayer >= maxClients)
                    {
                        foreach (PlayerInfo pInf in players)
                        {
                            pInf.isInputTaken = false;
                            pInf.move = "";
                            pInf.isInGame = false;
                            pInf.inGameScore = 0;
                        }
                        //Test play the game              
                        PlayTheGame();
                    }
                    //return;
                }
                
                isAllInputTaken = true;
                foreach (PlayerInfo pInf in players)
                {
                    if (pInf.isInGame && !pInf.isInputTaken)
                    {
                        isAllInputTaken = false;
                        pInf.socket.Send(timeLeftBuffer);
                    }
                }
                if (isAllInputTaken)
                {
                    break;
                }
            }

            if (!isAllInputTaken)
            {
                var notInpTakenPlayers = players.Where(p => p.isInGame && !p.isInputTaken).ToList();
                foreach (PlayerInfo pInf in notInpTakenPlayers)
                {
                    string notInpTakenMsg = "You did not enter your move in time. You are eliminated from the game.\n";
                    byte[] notInpTakenBuffer = Encoding.Default.GetBytes(notInpTakenMsg);
                    pInf.socket.Send(notInpTakenBuffer);
                    pInf.isInGame = false;
                    pInf.isInputTaken = false;
                    pInf.move = "";
                    pInf.inGameScore = 0;
                }
            }

            //Decide the winner
            var scores = new Dictionary<string, int>();
            foreach(var entry in players)
            {
                if (entry.isInGame)
                {
                    if(entry.move == "Rock")
                    {
                        var plInf = entry;
                        plInf.inGameScore = players.Count(p => p.move == "Scissors");
                        scores.Add(entry.name, plInf.inGameScore);
                    }
                    else if(entry.move == "Paper")
                    {
                        var plInf = entry;
                        plInf.inGameScore = players.Count(p => p.move == "Rock");
                        scores.Add(entry.name, plInf.inGameScore);
                    }
                    else if (entry.move == "Scissors")
                    {
                        var plInf = entry;
                        plInf.inGameScore = players.Count(p => p.move == "Paper");
                        scores.Add(entry.name, plInf.inGameScore);
                    }
                }
            }

            List<string> winners = new List<string>();

            //stupid 2 same choice case
            List<int> sortedScores = scores.Values.ToList();
            sortedScores.Sort();
            if (sortedScores.SequenceEqual(new List<int> { 1, 1, 1, 2 }))
            {
                var pairMove = players.Where(p => p.isInGame).GroupBy(p => p.move).Where(g => g.Count() > 1).Select(g => g.Key).ToList()[0];
                winners = players.Where(p => p.isInGame && p.move == pairMove).Select(p => p.name).ToList();
            }//Logical cases
            else
            {
                winners = scores.Where(s => s.Value == scores.Values.Max()).Select(s => s.Key).ToList();
            }

            foreach (var entry in scores)
            {
                var pl = players.FirstOrDefault(item => item.name == entry.Key);
                string moveMsg = pl.name + " played " + pl.move + ".\n";
                BroadCastMessage(moveMsg);
            }

            if (winners.Count == 1)
            {
                //Active max players back to the original
                activeMaxClients = maxClients;
                isSecondRound = false;

                string winMsg = winners[0] + " has won the game!\n";
                UpdateWinCount(winners[0]);
                byte[] winBuffer = Encoding.Default.GetBytes(winMsg);
                foreach (PlayerInfo pInf in players)
                {
                    if (pInf.isInGame)
                    {
                        pInf.socket.Send(winBuffer);
                    }
                }

                //One player won, restart the game if enough players left
                int currentPlayer = players.Count;
                if (currentPlayer >= maxClients)
                {
                    foreach (PlayerInfo pInf in players)
                    {
                        pInf.isInputTaken = false;
                        pInf.move = "";
                        pInf.isInGame = false;
                        pInf.inGameScore = 0;
                    }
                    //Test play the game
                    PlayTheGame();
                }
            }
            else
            {
                string winMsg = "The winners are: " + string.Join(", ", winners) + "\n" + "The next round is starting.\n";
                byte[] winBuffer = Encoding.Default.GetBytes(winMsg);
                foreach (PlayerInfo pInf in players)
                {
                    if (pInf.isInGame)
                    {
                        pInf.socket.Send(winBuffer);
                    }
                }
                foreach(var pl in players)
                {
                    if (!winners.Contains(pl.name))
                    {
                        var plInf = pl;
                        plInf.isInGame = false;
                        plInf.isInputTaken = false;
                        plInf.move = "";
                        plInf.inGameScore = 0;
                    }
                }
                foreach (var winner in winners)
                {
                    var plInf = players.FirstOrDefault(item => item.name == winner);
                    plInf.isInputTaken = false;
                    plInf.move = "";
                }

                isSecondRound = true;
                activeMaxClients = winners.Count;
                PlayTheGame();
            }

        }
        #endregion

        #region LEADERBOARD FUNCTIONS
        // Helper method to read win counts from file
        private Dictionary<string, PlayerStatistics> ReadWinCountsFromFile()
        {
            Dictionary<string, PlayerStatistics> playerStats = new Dictionary<string, PlayerStatistics>();
            try
            {
                string[] lines = File.ReadAllLines("../../leaderboard.txt");
                foreach (string line in lines)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 4)
                    {
                        string username = parts[0];
                        if (int.TryParse(parts[1], out int wins) && int.TryParse(parts[2], out int losses) && int.TryParse(parts[3], out int totalGamesPlayed))
                        {
                            playerStats[username] = new PlayerStatistics(wins, losses, totalGamesPlayed);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.AppendText("Could not read win counts file: " + e.Message);
            }
            return playerStats;
        }

        // Helper method to write win counts to file
        private void WriteWinCountsToFile(Dictionary<string, PlayerStatistics> playerStats)
        {
            List<string> lines = new List<string>();
            foreach (var pair in playerStats)
            {
                lines.Add($"{pair.Key},{pair.Value.winCount},{pair.Value.lossCount},{pair.Value.totalGamesPlayed}");
            }
            try
            {
                File.WriteAllLines("../../leaderboard.txt", lines);
            }
            catch (Exception e)
            {
                logs.AppendText("Could not write win counts file: " + e.Message);
            }
        }

        // Call this method after determining the winner to update their win count
        private void UpdateWinCount(string winnerName)
        {
            var playerStats = ReadWinCountsFromFile();
            if (!playerStats.ContainsKey(winnerName))
            {
                playerStats[winnerName] = new PlayerStatistics(0, 0, 0); // Add new player statistics if not already present
            }

            foreach (var player in players)
            {
                if (player.isInGame)
                {
                    if (player.name == winnerName)
                    {
                        playerStats[winnerName].IncrementWin();
                    }
                    else
                    {
                        if (!playerStats.ContainsKey(player.name))
                        {
                            playerStats[player.name] = new PlayerStatistics(0, 0, 0); // Add new player statistics if not already present
                        }
                        playerStats[player.name].IncrementLoss();
                    }
                }
            }
            WriteWinCountsToFile(playerStats);
            UpdateLeaderboard(playerStats); 
            BroadcastLeaderboard(playerStats); 
        }

        // Method to update the leaderboard shown in the server GUI
        private void UpdateLeaderboard(Dictionary<string, PlayerStatistics> playerStats)
        {
            leaderboard.Items.Clear();
            leaderboard.Items.Add("LEADERBOARD:\n");
            foreach (var entry in playerStats.OrderByDescending(pair => pair.Value.winCount))
            {
                leaderboard.Items.Add($"{entry.Key}: Wins: {entry.Value.winCount}, Losses: {entry.Value.lossCount}, Played: {entry.Value.totalGamesPlayed}");
            }
        }

        // Add this method in the server's Form1 class
        private void BroadcastLeaderboard(Dictionary<string, PlayerStatistics> playerStats)
        {
            string leaderboardString = "LeaderboardUpdate:" + string.Join(",", playerStats.Select(x => $"{x.Key}:{x.Value.winCount}:{x.Value.lossCount}:{x.Value.totalGamesPlayed}"));
            byte[] buffer = Encoding.Default.GetBytes(leaderboardString);
            foreach (PlayerInfo player in players)
            {
                try
                {
                    player.socket.Send(buffer);
                }
                catch
                {
                    // Optional: handle errors, e.g., log this event or remove the player from the list.
                }
            }
        }
        private void SendLeaderboard(Dictionary<string, PlayerStatistics> playerStats, PlayerInfo target_player)
        {
            string leaderboardString = "LeaderboardUpdate:" + string.Join(",", playerStats.Select(x => $"{x.Key}:{x.Value.winCount}:{x.Value.lossCount}:{x.Value.totalGamesPlayed}"));

            byte[] buffer = Encoding.Default.GetBytes(leaderboardString);

            try
            {
                target_player.socket.Send(buffer);
                Thread.Sleep(200);
            }
            catch { }
        }

        private void leaderboard_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        #endregion

        #region MAIN FLOW
        // Main thread for activating clients
        private void MyReceive(Socket thisClient)
        {
            bool connected = true;

            while (connected && !terminating)
            {
                try
                {
                    byte[] buffer = new byte[64];
                    int receivedByteCount = thisClient.Receive(buffer);
                    if (receivedByteCount > 0)
                    {

                        string incomingMessage = Encoding.Default.GetString(buffer).Substring(0, receivedByteCount);
                        logs.AppendText(incomingMessage + "\n");

                        var nameMovePair = incomingMessage.Split(' ');

                        // Player wants to LEAVE the game
                        if (nameMovePair[1] == "leavegame") 
                        {
                            var plInf = players.FirstOrDefault(item => item.name == nameMovePair[0]);
                            plInf.isInGame = false;
                            plInf.isLeft = true;
                            plInf.isInputTaken = false;
                            plInf.move = "";
                            plInf.inGameScore = 0;

                            //Broadcast the left player
                            string leaveMessage = "Player " + nameMovePair[0] + " left the game";
                            BroadCastMessage(leaveMessage);
                        }
                        
                        //Player wants to REJOIN game
                        if (nameMovePair[1] == "rejoingame")
                        {
                            var plInf = players.FirstOrDefault(item => item.name == nameMovePair[0]);
                            plInf.isLeft = false;
                        }

                        //IN GAME LOGIC
                        if (nameMovePair[1] == "Rock" || nameMovePair[1] == "Paper" || nameMovePair[1] == "Scissors")
                        {
                            string inputTakenMsg = "Your move " + nameMovePair[1] +" is taken.\n Waiting for other players.\n";
                            byte[] inputTakenBuffer = Encoding.Default.GetBytes(inputTakenMsg);
                            thisClient.Send(inputTakenBuffer);
                            var plInf = players.FirstOrDefault(item => item.name == nameMovePair[0]);
                            plInf.isInputTaken = true;
                            plInf.move = nameMovePair[1];
                            
                        }   
                    }
                    else
                    {
                        throw new SocketException(); // Assume the connection is closed if no data is received
                    }
                }
                catch
                {
                    //If this client is in the game
                    if (players.FirstOrDefault(p => p.socket == thisClient).isInGame) //Active Player has left
                    {
                        logs.AppendText("Active player has disconnected\n");

                        //Remove from the list
                        string username = players.FirstOrDefault(p => p.socket == thisClient).name;
                        players.RemoveAll(p => p.name == username);

                    }
                    else
                    {
                        logs.AppendText("Waiting player has disconnected\n");
                        //Remove from the dictionary
                        players.RemoveAll(p => p.socket == thisClient);
                    }

                    thisClient.Close();
                    connected = false;

                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (listening) 
            {
                listening = false;
                terminating = true;
                
                foreach(PlayerInfo pInf in players)
                {
                    pInf.socket.Close();
                }

                serverSocket.Close();
                Environment.Exit(0);
            }
        }
        #endregion

        #region WHATEVER
        // If server sends message
        private void button_send_Click(object sender, EventArgs e)
        {
            string message = "Server: " + textBox_message.Text;
            logs.AppendText(message + "\n");
            if (message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                foreach (PlayerInfo pInf in players)
                {
                    try
                    {
                        pInf.socket.Send(buffer);
                    }
                    catch
                    {
                        logs.AppendText("There is a problem! Check the connection.\n");
                        terminating = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        textBox_port.Enabled = true;
                        button_listen.Enabled = true;
                        serverSocket.Close();
                    }

                }
            }
        }

        private void logs_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox_port_TextChanged(object sender, EventArgs e)
        {

        }
        #endregion

    }
}

public class PlayerInfo
{
    public string name;
    public Socket socket;
    public bool isInGame;
    public bool isInputTaken;
    public bool isLeft;
    public string move;
    public int inGameScore;
    public PlayerStatistics playerStats;

    public PlayerInfo(string _name, Socket _socket, PlayerStatistics _playerStats)
    {
        name = _name;
        socket = _socket;
        isInGame = false;
        isInputTaken = false;
        isLeft = false;
        move = "";
        inGameScore = 0;
        playerStats = _playerStats;
    }

}

public class PlayerStatistics
{
    public int winCount;
    public int lossCount;
    public int totalGamesPlayed;

    public PlayerStatistics(int _winCount, int _lossCount, int _totalGamesPlayed)
    {
        this.winCount = _winCount;
        this.lossCount = _lossCount;
        this.totalGamesPlayed = _totalGamesPlayed;
    }

    public void IncrementWin()
    {
        winCount++;
        totalGamesPlayed++;
    }

    public void IncrementLoss()
    {
        lossCount++;
        totalGamesPlayed++;
    }
}