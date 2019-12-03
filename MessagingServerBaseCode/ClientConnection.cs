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
    public class ClientConnection : IDisposable
    {
        private static readonly string EOM = "<EOM>";

        private Socket clientSocket;
        public string Name
        {
            get;
            private set;
        }
        
        private volatile List<Message> ReadQueue;
        private volatile List<Message> SendQueue;

        private Thread ReadThread;
        private Thread SendThread;

        private bool EndProcess = false;

        public ClientConnection(ref Socket cs)
        {
            clientSocket = cs;

            byte[] buffer = new byte[1024];
            bool recievedName = false;

            while(!recievedName)
            {
                int byteCount = clientSocket.Receive(buffer);

                Name += Encoding.ASCII.GetString(buffer, 0, byteCount);

                if(Name.EndsWith(EOM))
                {
                    Name = Name.Replace(EOM, "");
                    recievedName = true;
                }
            }

            ReadThread = new Thread(new ThreadStart(ReadRun));
            SendThread = new Thread(new ThreadStart(SendRun));

            ReadThread.Start();
            SendThread.Start();
        }

        public void SendMessage(string message, string sender = "server")
        {
            Message msg = new Message(Encoding.ASCII.GetBytes(message), sender);
            SendMessage(msg);
        }

        public Message GetNextMessage()
        {
            Message message = ReadQueue[0];
            ReadQueue.RemoveAt(0);

            return message;
        }

        public void SendMessage(Message message)
        {
            SendQueue.Add(message);
        }

        public void Dispose()
        {
            EndProcess = true;
            ReadThread.Abort();
            SendThread.Abort();

            foreach(Message message in SendQueue)
            {
                clientSocket.Send(message.Data);
                clientSocket.Send(Encoding.ASCII.GetBytes(EOM));
            }

            byte[] goodbyte = Encoding.ASCII.GetBytes("Goodbye!" + EOM);

            clientSocket.Send(goodbyte);

            clientSocket.Close();
            clientSocket.Dispose();
        }

        private void ReadRun()
        {
            while(!EndProcess)
            {
                byte[] buffer = new byte[1024];
                
                while (true)
                {
                    string readStr = "";
                    try
                    {
                        int byteCount = clientSocket.Receive(buffer);

                        readStr += Encoding.ASCII.GetString(buffer, 0, byteCount);
                        if(readStr.EndsWith(EOM))
                        {
                            readStr = readStr.Replace(EOM, "");
                            Message newMessage = new Message(Encoding.ASCII.GetBytes(readStr), Name);
                            ReadQueue.Add(newMessage);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} seems to have lost connection...", Name);
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private void SendRun()
        {
            while(!EndProcess)
            {
                if(SendQueue.Count > 0)
                {
                    try
                    {
                        Message message = SendQueue[0];
                        clientSocket.Send(message.Data);
                        clientSocket.Send(Encoding.ASCII.GetBytes(EOM));

                        SendQueue.RemoveAt(0);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("{0} seems to have lost connection...", Name);
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}
