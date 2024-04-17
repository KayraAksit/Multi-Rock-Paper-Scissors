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
<<<<<<< Updated upstream
=======
        List<Socket> waitingQueue = new List<Socket>(); // Queue for excess connections
>>>>>>> Stashed changes

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
            while(listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept();
<<<<<<< Updated upstream
                    clientSockets.Add(newClient);
                    logs.AppendText("A client is connected.\n");

                    Thread receiveThread = new Thread(() => Receive(newClient)); // updated
                    receiveThread.Start();
=======

                    // Check if maximum players are already connected
                    if (clientSockets.Count < maxClients)
                    {
                        clientSockets.Add(newClient);
                        logs.AppendText("A player has connected.\n");
                        NotifyClientGameStart(newClient);

                        Thread receiveThread = new Thread(() => Receive(newClient));
                        receiveThread.Start();
                    }
                    else
                    {
                        waitingQueue.Add(newClient);
                        logs.AppendText("A player entered the waiting queue.\n");
                        NotifyClientQueueStatus(newClient);
                    }
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
        private void Receive(Socket thisClient) // updated
=======
        private void NotifyClientGameStart(Socket client)
        {
            string gameStartMsg = "The game has started!\n";
            Byte[] gameStartBuffer = Encoding.Default.GetBytes(gameStartMsg);
            client.Send(gameStartBuffer);
        }

        private void NotifyClientQueueStatus(Socket client)
        {
            string queueMsg = "You are in the waiting queue...\n";
            Byte[] queueBuffer = Encoding.Default.GetBytes(queueMsg);
            client.Send(queueBuffer);
        }

        private void Receive(Socket thisClient)
>>>>>>> Stashed changes
        {
            bool connected = true;

            while(connected && !terminating)
            {
                try
                {
                    Byte[] buffer = new Byte[64];
                    int receivedByteCount = thisClient.Receive(buffer);
                    if (receivedByteCount > 0)
                    {
                        string incomingMessage = Encoding.Default.GetString(buffer).Substring(0, receivedByteCount);
                        logs.AppendText(incomingMessage + "\n");

<<<<<<< Updated upstream
                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    logs.AppendText(incomingMessage + "\n");

                    //Sends all recieved messages back to all clients, i think
                    foreach(Socket socket in clientSockets)
                    {
                        Byte[] bufferClient = Encoding.Default.GetBytes("BROADCAST: " + incomingMessage);
                        socket.Send(bufferClient);
=======
                        // Broadcast the message to all clients in the game
                        foreach (Socket socket in clientSockets)
                        {
                            if (socket != thisClient)
                            {
                                Byte[] bufferClient = Encoding.Default.GetBytes("BROADCAST: " + incomingMessage);
                                socket.Send(bufferClient);
                            }
                        }
                    }
                    else
                    {
                        throw new SocketException(); // Assume the connection is closed if no data is received
>>>>>>> Stashed changes
                    }
                }
                catch
                {
                    if(!terminating)
                    {
<<<<<<< Updated upstream
                        logs.AppendText("A client has disconnected\n");
=======
                        logs.AppendText("A player has disconnected\n");
>>>>>>> Stashed changes
                    }

                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                    connected = false;

                    // Check if waiting players can join
                    if (waitingQueue.Count > 0)
                    {
                        Socket waitingClient = waitingQueue[0];
                        waitingQueue.RemoveAt(0);
                        clientSockets.Add(waitingClient);
                        NotifyClientGameStart(waitingClient);

                        Thread receiveThread = new Thread(() => Receive(waitingClient));
                        receiveThread.Start();
                    }
                }
            }
        }

<<<<<<< Updated upstream
        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)
=======
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
>>>>>>> Stashed changes
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
