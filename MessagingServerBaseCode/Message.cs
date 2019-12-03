using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServerBaseCode
{
    public class Message : IComparable
    {
        public string source;
        public string[] destinations;

        private static readonly byte separator = Encoding.ASCII.GetBytes("|")[0];

        public byte[] OutData
        {
            get
            {
                List<byte> bytes = Encoding.ASCII.GetBytes(source).ToList();
                bytes.Add(separator);
                bytes.AddRange(Data);
                return bytes.ToArray();
            }
        }

        public byte[] Data
        {
            get;
            private set;
        }

        public DateTime TimeStamp
        {
            get;
            private set;
        }

        public string Text => new string(Encoding.ASCII.GetChars(Data));
        
        public Message(byte[] message, string from)
        {
            TimeStamp = DateTime.Now;

            this.source = from;
            List<byte> dest = new List<byte>();
            List<byte> data = new List<byte>();
            bool readData = false;
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == separator)
                {
                    readData = true;
                    continue;
                }

                if (!readData)
                {
                    dest.Add(message[i]);
                }
                else
                {
                    data.Add(message[i]);
                }
            }

            destinations = (new string(Encoding.ASCII.GetChars(dest.ToArray()))).Split(',');

            Data = data.ToArray();
        }
        
        public int CompareTo(Message other)
        {
            return this.CompareTo(other.TimeStamp);
        }

        public int CompareTo(DateTime other)
        {
            return TimeStamp.CompareTo(other);
        }

        public int CompareTo(object obj)
        {
            if (obj is Message)
                return this.CompareTo((Message)obj);

            if (obj is DateTime)
                return this.CompareTo((DateTime)obj);

            return 0;
        }
    }
}
