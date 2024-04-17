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
        Queue<Socket> waitingQueue = new Queue<Socket>(); // Queue for excess connections
        int connectedClientsCount = 0;
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
                    if (connectedClientsCount < maxClients)
                    {
                        waiting = false;
                        clientSockets.Add(newClient);
                        connectedClientsCount++;
                        logs.AppendText("A client is connected.\n");

                        // Send response to client: "Connected"
                        Byte[] connectedResponse = Encoding.Default.GetBytes("Connect");
                        newClient.Send(connectedResponse);
                        

                        Thread receiveThread = new Thread(() => Receive(newClient)); // updated
                        receiveThread.Start();
                    }
                    else
                    {
                        waitingQueue.Enqueue(newClient);
                        logs.AppendText("A client is in the queue.\n");

                        // Send response to client: "Queue"
                        Byte[] queueResponse = Encoding.Default.GetBytes("Queue");
                        newClient.Send(queueResponse);

                        waiting = true;
                        Thread dequeueThread = new Thread(() => WaitConnection(newClient)); // updated
                        dequeueThread.Start();
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

        private void WaitConnection(Socket thisClient)
        {
            while (connectedClientsCount >= 2)
            {

            }

            Thread receiveThread = new Thread(() => Receive(thisClient)); // updated
            receiveThread.Start();
        }

        private void Receive(Socket thisClient) // updated
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

                    //Sends all recieved messages back to all clients, i think
                    //foreach (Socket socket in clientSockets)
                    //{
                    //    Byte[] bufferClient = Encoding.Default.GetBytes("BROADCAST: " + incomingMessage);
                    //    socket.Send(bufferClient);
                    //}
                }
                catch
                {
                    if(!terminating)
                    {
                        logs.AppendText("A client has disconnected\n");
                        connectedClientsCount--;
                        clientSockets.Remove(thisClient);

                        // Try to connect a client from the waiting queue
                        ConnectFromQueue();
                    }
                    thisClient.Close();
                    clientSockets.Remove(thisClient);
                    connected = false;
                }
            }
        }

        private void ConnectFromQueue()
        {
            if (waitingQueue.Count > 0)
            {
                Socket client = waitingQueue.Dequeue();
                clientSockets.Add(client);
                connectedClientsCount++;
                logs.AppendText("A client from the queue is connected.\n");

                Thread receiveThread = new Thread(() => Receive(client));
                receiveThread.Start();
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
