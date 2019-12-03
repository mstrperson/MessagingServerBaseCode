
#define FUNMODE

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

        public static readonly byte SOT = 0x0002; // UTF8 Start of Text.
        public static readonly byte EOM = 0x0004; // UTF8 End of Transmission.

        public byte[] OutData
        {
            get
            {
                List<byte> bytes = Encoding.UTF8.GetBytes(source).ToList();
                bytes.Add(SOT);
                bytes.AddRange(Data);
#if FUNMODE
                // the server can alter bytes in the message....
                if(bytes.Contains(0x006f))
                {
                    for(int i = 0; i < bytes.Count; i++)
                    {
                        if(bytes[i] == 0x006f)
                        {
                            bytes[i] = 0x00f6;
                        }
                    }
                }
#endif
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

        public string Text => new string(Encoding.UTF8.GetChars(Data));
        
        public Message(byte[] message, string from)
        {
            TimeStamp = DateTime.Now;

            this.source = from;
            List<byte> dest = new List<byte>();
            List<byte> data = new List<byte>();
            bool readData = false;
            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == SOT)
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

            destinations = (new string(Encoding.UTF8.GetChars(dest.ToArray()))).Split(',');

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
