using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MessagingServerBaseCode
{
    class Program
    {
        private static volatile Dictionary<string, ClientConnection> clients = new Dictionary<string, ClientConnection>();

        private static Socket listener;

        private static Thread NewConnectionThread;

        private static Thread MessageProcessingThread;

        private static Thread ServerMessageThread;

        private static volatile List<Message> serverMessages;

        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            // Establish the local endpoint  
            // for the socket. Dns.GetHostName 
            // returns the name of the host  
            // running the application. 
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = new IPAddress(new byte[] { 10, 0, 4, 86 });//IPAddress.Loopback;//ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 12345);

            // Creation TCP/IP Socket using  
            // Socket Class Costructor 
            listener = new Socket(ipAddr.AddressFamily,
                         SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEndPoint);

            listener.Listen(15);

            serverMessages = new List<Message>();

            NewConnectionThread = new Thread(new ThreadStart(RegisterClients));
            NewConnectionThread.Start();

            MessageProcessingThread = new Thread(new ThreadStart(ProcessMessages));
            MessageProcessingThread.Start();

            ServerMessageThread = new Thread(new ThreadStart(ProcessServerMessages));
            ServerMessageThread.Start();

            Console.WriteLine("GAME ON!");
            while(true)
            {
                // the Game is ON!

                Console.WriteLine(">> ");
                string input = Console.ReadLine();

                if (input.Equals("quit"))
                    break;
            }
            try
            {
                Thread.BeginCriticalRegion();
                foreach (string client in clients.Keys)
                {
                    clients[client].SendMessage("Server is shutting down now.");
                    clients[client].Dispose();
                }
                Thread.EndCriticalRegion();
            }
            catch
            {
                Console.WriteLine("May not have notified all clients that I'm shutting down...");
            }
            MessageProcessingThread.Abort();
            // Stop Listening before aborting the Connection thread.
            listener.Close();
            NewConnectionThread.Abort();
            ServerMessageThread.Abort();
        }

        private static void RegisterClients()
        {
            Console.WriteLine("Waiting for clients...");
            while (true)
            {
                Socket clientSocket = listener.Accept();
                Console.WriteLine("New Client connecting...");
                Thread newClient = new Thread(new ParameterizedThreadStart(delegate
                {
                    ClientConnection clientConnection = new ClientConnection(ref clientSocket);
                    Thread.BeginCriticalRegion();
                    clients.Add(clientConnection.Name, clientConnection);
                    Thread.EndCriticalRegion();

                    Console.WriteLine("{0} Connected!", clientConnection.Name);
                }));

                newClient.Start();

                Thread.Sleep(20);
            }
        }

        private static void ProcessMessages()
        {
            while(true)
            {
                try
                {
                    foreach (string client in clients.Keys)
                    {
                        if(!clients[client].StillConnected)
                        {
                            Thread.BeginCriticalRegion();
                            clients.Remove(client);
                            clients[client].Dispose();
                            Thread.EndCriticalRegion();
                            foreach(string c in clients.Keys)
                            {
                                if(!c.Equals(client))
                                {
                                    clients[c].SendMessage(string.Format("{0} has left the server.", client));
                                }
                            }
                            continue;
                        }
                        if (!clients[client].HasMessages) continue;

                        Message nextMessage = clients[client].GetNextMessage();

                        foreach (string recipient in nextMessage.destinations)
                        {
                            if (recipient.Equals("server"))
                            {
                                serverMessages.Add(nextMessage);
                                continue;
                            }

                            if (clients.ContainsKey(recipient))
                            {
                                clients[recipient].SendMessage(nextMessage);
                            }
                            else
                            {
                                clients[client].SendMessage(string.Format("{0} doesn't seem to be connected.", recipient));
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private static void ProcessCommand(Message message)
        {

        }

        private static void ProcessServerMessages()
        {
            while(true)
            {
                if(serverMessages.Count > 0)
                {
                    Console.WriteLine("[{0}]\t{1}:  {2}", serverMessages[0].TimeStamp, serverMessages[0].source, serverMessages[0].Text);

                    ProcessCommand(serverMessages[0]);

                    serverMessages.RemoveAt(0);
                }
            }
        }
    }

}
