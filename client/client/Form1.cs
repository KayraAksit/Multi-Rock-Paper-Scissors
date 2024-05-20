using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {

        bool terminating = false;
        bool connected = false;
        Socket clientSocket;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;

            int portNum;
            if (Int32.TryParse(textBox_port.Text, out portNum))
            {
                try
                {
                    clientSocket.Connect(IP, portNum);
                    button_connect.Enabled = false;
                    playerMove.Enabled = false;
                    button_send.Enabled = false;
                    button_leavegame.Enabled = true;
                    connected = true;
                    logs.AppendText("Connected to the server!\n");

                    //Send name to server when connected for the first time
                    string pName = textBox_name.Text;
                    Byte[] buffer = Encoding.Default.GetBytes(pName);
                    clientSocket.Send(buffer);


                    Thread receiveThread = new Thread(Receive);
                    receiveThread.Start();

                }
                catch
                {
                    logs.AppendText("Could not connect to the server!\n");
                }
            }
            else
            {
                logs.AppendText("Check the port\n");
            }

        }

        private void Receive()
        {
            while (connected)
            {
                try
                {
                    byte[] buffer = new byte[10000];
                    int receivedByteCount = clientSocket.Receive(buffer);
                    if (receivedByteCount > 0)
                    {
                        string incomingMessage = Encoding.Default.GetString(buffer).Substring(0, receivedByteCount);

                        // A change has happened in the leaderboard. This if check is only for message displays on the log
                        if(!incomingMessage.Contains("LeaderboardUpdate"))
                        {
                            logs.AppendText(incomingMessage + "\n");
                        }
                        else
                        {
                            logs.AppendText("Leaderboard Updated \n");
                        }

                        // When in the queue, ignore other messages
                        if (incomingMessage.Contains("waiting queue")) // client is put in the queue
                        {
                            playerMove.Enabled = false;
                            button_send.Enabled = false;
                        }
                        if (incomingMessage.Contains("your move you have")) // client made a move
                        {
                            playerMove.Enabled = true;
                            button_send.Enabled = true;
                        }
                        if (incomingMessage.Contains("The next round is starting")) // client is ascending to next round
                        {
                            playerMove.Enabled = false;
                            button_send.Enabled = false;
                        }
                        if (incomingMessage.StartsWith("LeaderboardUpdate:")) // Handling leaderboard updates
                        {
                            UpdateLeaderboardClient(incomingMessage.Replace("LeaderboardUpdate:", ""));
                        }
                    }
                    else
                    {
                        throw new SocketException(); // Assume the connection is closed if no data is received
                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("The server has disconnected\n");
                        button_connect.Enabled = true;
                        playerMove.Enabled = false;
                        button_send.Enabled = false;
                    }

                    clientSocket.Close();
                    connected = false;
                }

            }
        }

        // Gets the leaderboard from serverside, updates leaderboard on this client
        private void UpdateLeaderboardClient(string leaderboardData)
        {
            leaderboard.Items.Clear();
            leaderboard.Items.Add("LEADERBOARD: \n");

            // Split the leaderboard data into an array of players
            string[] players = leaderboardData.Split(',');

            // Create a list to hold player details
            List<string[]> playerDetails = new List<string[]>();

            // Populate the player details list
            foreach (string player in players)
            {
                string[] details = player.Split(':');
                playerDetails.Add(details);
            }

            // Sort the player details list based on the score in descending order
            playerDetails.Sort((x, y) => int.Parse(y[1]).CompareTo(int.Parse(x[1])));

            // Add sorted player details to the leaderboard
            foreach (string[] details in playerDetails)
            {
                leaderboard.Items.Add($"Player: {details[0]}, Wins: {details[1]}, Losses: {details[2]}, Played: {details[3]}\n");
            }
        }


        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string message = textBox_name.Text + " " + playerMove.Text;

            if (message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
            }

            if (playerMove.Text != "")
            {
                playerMove.Enabled = false;
                button_send.Enabled = false;
            }

        }

        // Player decided to DISCONNECT
        private void button_leavegame_Click(object sender, EventArgs e)
        {
            string message = textBox_name.Text + " " + "leavegame";

            playerMove.Enabled = false;
            button_send.Enabled = false;
            button_connect.Enabled = true;
            button_leavegame.Enabled = false;

            if (message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
            }
        }
    }
}