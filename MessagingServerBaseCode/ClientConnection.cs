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

        private Socket clientSocket;
        public string Name
        {
            get;
            private set;
        }

        public bool StillConnected => clientSocket.Connected;

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

                // check to see if this is the end of the transmission.
                if (buffer[byteCount - 1] == Message.EOM)
                {
                    recievedName = true;
                    byteCount--;
                }

                Name += Encoding.UTF8.GetString(buffer, 0, byteCount);
            }

            ReadQueue = new List<Message>();
            SendQueue = new List<Message>();

            ReadThread = new Thread(new ThreadStart(ReadRun));
            SendThread = new Thread(new ThreadStart(SendRun));

            ReadThread.Start();
            SendThread.Start();

            SendMessage(string.Format("Hello, {0}", Name));
        }

        public void SendMessage(string message, string sender = "server")
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            if (!bytes.Contains(Message.SOT))
            {
                message = string.Format("{0}{1}{2}", Name, (char)Message.SOT, message);
                bytes = Encoding.UTF8.GetBytes(message);
            }
            Message msg = new Message(bytes, sender);
            SendMessage(msg);
        }

        public bool HasMessages => ReadQueue.Count > 0;

        public Message GetNextMessage()
        {
            if (ReadQueue.Count > 0)
            {
                Message message = ReadQueue[0];
                ReadQueue.RemoveAt(0);

                return message;
            }

            return null;
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
            if (this.StillConnected)
            {
                foreach (Message message in SendQueue)
                {
                    clientSocket.Send(message.OutData);
                    clientSocket.Send(new byte[] { Message.EOM });
                }

                byte[] goodbyte = Encoding.UTF8.GetBytes("Goodbye!" + (char)Message.EOM);

                clientSocket.Send(goodbyte);

                clientSocket.Close();
                clientSocket.Dispose();
            }
        }

        private void ReadRun()
        {
            while(!EndProcess && this.StillConnected)
            {
                byte[] buffer = new byte[1024];
                bool messageRecieved = false;
                while (this.StillConnected && !messageRecieved)
                {
                    string readStr = "";
                    try
                    {
                        int byteCount = clientSocket.Receive(buffer);

                        if(buffer.Contains(Message.EOM))
                        {
                            messageRecieved = true;
                            byteCount--;
                        }

                        readStr += Encoding.UTF8.GetString(buffer, 0, byteCount);
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} seems to have lost connection...", Name);
                        Console.WriteLine(e.Message);
                    }

                    Message newMessage = new Message(Encoding.UTF8.GetBytes(readStr), Name);
                    ReadQueue.Add(newMessage);
                    
                }
            }
        }

        private void SendRun()
        {
            while(!EndProcess && this.StillConnected)
            {
                if(SendQueue.Count > 0)
                {
                    try
                    {
                        Message message = SendQueue[0];
                        clientSocket.Send(message.OutData);
                        clientSocket.Send(new byte[] { Message.EOM });

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
