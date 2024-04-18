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
    public partial class Form1 : Form
    {
        const int maxClients = 2; //Define max number of players playing simultaneously

        Socket serverSocket;
        List<Socket> clientSockets = new List<Socket>();
        List<Socket> waitingQueue = new List<Socket>(); // Queue for excess connections

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

                    // Check if maximum players are already connected
                    if (clientSockets.Count < maxClients)
                    {
                        clientSockets.Add(newClient);
                        logs.AppendText("A player has connected.\n");

                        if (clientSockets.Count == maxClients) //If game has enough players
                        {
                            foreach(Socket socket in clientSockets) //Notify players of game start
                            {
                                NotifyClientGameStart(socket);
                            }
                            foreach (Socket socket in waitingQueue) //Notify waiters of game start
                            {
                                NotifyClientGameStart(socket);
                            }
                        }
                        else //Game does not have enough players
                        {
                            foreach (Socket socket in clientSockets) //Notify players of game start
                            {
                                NotifyClientNotEnoughPlayers(socket);
                            }
                        }

                        Thread receiveThread = new Thread(() => MyReceive(newClient));
                        receiveThread.Start();
                    }
                    else //Max players joined, add new joiners to the queue
                    {
                        waitingQueue.Add(newClient);
                        logs.AppendText("A player entered the waiting queue.\n");
                        Thread receiveThread = new Thread(() => MyReceive(newClient));
                        receiveThread.Start();
                        NotifyClientQueueStatus(newClient);
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
                    if (clientSockets.Contains(thisClient)) //Active Player has left
                    {
                        logs.AppendText("Active player has disconnected\n");
                        clientSockets.Remove(thisClient); //Remove from the list
                                                          
                        if (waitingQueue.Count > 0)// Check if waiting players can join
                        {
                            Socket waitingClient = waitingQueue[0];
                            waitingQueue.RemoveAt(0);
                            clientSockets.Add(waitingClient);
                            NotifyClientGameStart(waitingClient);

                            Thread receiveThread = new Thread(() => MyReceive(waitingClient));
                            receiveThread.Start();
                        }
                        else
                        {
                            foreach (Socket socket in clientSockets) //Notify players of game start
                            {
                                NotifyClientNotEnoughPlayers(socket);
                            }
                        }
                    }
                    else
                    {
                        logs.AppendText("Waiting player has disconnected\n");
                        waitingQueue.Remove(thisClient); //Remove from the list
                    }


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
                foreach (Socket client in clientSockets)
                    client.Close();

                foreach (Socket client in waitingQueue)
                    client.Close();

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
                foreach (Socket client in clientSockets)
                {
                    try
                    {
                        client.Send(buffer);
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
