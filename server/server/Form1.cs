using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server
{
    public struct PlayerInfo
    {
        public string name;
        public Socket socket;
        public bool isInGame;
    }
    public partial class Form1 : Form
    {

        //Initialize the dictionary to store player names
        Dictionary<string, PlayerInfo> players = new Dictionary<string, PlayerInfo>();

        const int maxClients = 2; //Define max number of players playing simultaneously

        Socket serverSocket;
        //List<Socket> clientSockets = new List<Socket>();
        //List<Socket> waitingQueue = new List<Socket>(); // Queue for excess connections

        bool terminating = false;
        bool listening = false;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                    if (players.ContainsKey(username))
                    {
                        // Notify the client that the username is already taken, ask for trying with a new one and disconnect the client
                        string usernameTakenMsg = "Username is already taken. Please try with a new one.\n";
                        byte[] usernameTakenBuffer = Encoding.Default.GetBytes(usernameTakenMsg);
                        newClient.Send(usernameTakenBuffer);
                        newClient.Close();
                    }
                    else
                    {
                        PlayerInfo newPlayer;
                        newPlayer.name = username;
                        newPlayer.socket = newClient;
                        
                        // Check if the game has enough players
                        int inGameCount = players.Count(p => p.Value.isInGame == true);
                        if(inGameCount < maxClients)
                        {
                            NotifyPlayerEnteredGame(username);

                            newPlayer.isInGame = true;
                            logs.AppendText(username + " has connected to the game.\n");

                            //Check if there are enough players to start the game now
                            if (inGameCount +1 == maxClients)
                            {
                                foreach (PlayerInfo pInf in players.Values) //Notify players of game start
                                {
                                    NotifyClientGameStart(pInf.socket);
                                }
                                NotifyClientGameStart(newClient);
                            }
                            else { 
                                foreach (PlayerInfo pInf in players.Values) //Notify players of not enough players
                                {
                                    NotifyClientNotEnoughPlayers(pInf.socket);
                                }
                                NotifyClientNotEnoughPlayers(newClient);
                            }
                        }
                        else
                        {
                            newPlayer.isInGame = false;
                            logs.AppendText("A player entered the waiting queue.\n");
                            NotifyClientQueueStatus(newClient);
                        }
                        players.Add(username, newPlayer);
                        Thread receiveThread = new Thread(() => MyReceive(newClient));
                        receiveThread.Start();
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

        private void NotifyClientGameStart(Socket client)
        {
            string gameStartMsg = "The game has started!\n";
            byte[] gameStartBuffer = Encoding.Default.GetBytes(gameStartMsg);
            client.Send(gameStartBuffer);
        }
        private void NotifyPlayerEnteredGame(string username)
        {
            string plEnteredMsg = username + " has entered the game!\n";
            byte[] plEnteredBuffer = Encoding.Default.GetBytes(plEnteredMsg);
            foreach (PlayerInfo pInf in players.Values)
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

                        // Broadcast the message to all clients in the game
                        //foreach (Socket socket in clientSockets)
                        //{
                        //    if (socket != thisClient)
                        //    {
                        //        byte[] bufferClient = Encoding.Default.GetBytes("BROADCAST: " + incomingMessage);
                        //        socket.Send(bufferClient);
                        //    }
                        //}
                    }
                    else
                    {
                        throw new SocketException(); // Assume the connection is closed if no data is received
                    }
                }
                catch
                {
                    //If this client is in the game
                    if (players.Values.FirstOrDefault(p => p.socket == thisClient).isInGame) //Active Player has left
                    {
                        logs.AppendText("Active player has disconnected\n");
                               
                        //Check if there are any players in the waiting queue
                        var waitingPlayer = players.FirstOrDefault(p => p.Value.isInGame == false);

                        if (!waitingPlayer.Equals(default(KeyValuePair<string,PlayerInfo>)))// Check if waiting players can join evaluates to false if there is no waiting player
                        {
                            var waitingPlayerInf = waitingPlayer.Value;
                            waitingPlayerInf.isInGame = true;
                            players[waitingPlayer.Key] = waitingPlayerInf;
                            NotifyClientGameStart(waitingPlayerInf.socket);
                        }
                        else
                        {
                            foreach (PlayerInfo pInf in players.Values) //Notify players of game start
                            {
                                NotifyClientNotEnoughPlayers(pInf.socket);
                            }
                        }
                    }
                    else
                    {
                        logs.AppendText("Waiting player has disconnected\n");
                    }

                    //Find the username of the player
                    string username = players.FirstOrDefault(p => p.Value.socket == thisClient).Key;
                    players.Remove(username); //Remove from the dictionary


                    //if (!terminating) NIYE TERMINATING CHECK VAR KI BURDA 
                    //{
                    //    logs.AppendText("A player has disconnected\n");
                    //}

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
                
                foreach(PlayerInfo pInf in players.Values)
                {
                    pInf.socket.Close();
                }

                serverSocket.Close();
                Environment.Exit(0);
            }
        }

    private void button_send_Click(object sender, EventArgs e)
        {
            string message = "Server: " + textBox_message.Text;
            logs.AppendText(message + "\n");
            if (message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                foreach (PlayerInfo pInf in players.Values)
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
    }
}
