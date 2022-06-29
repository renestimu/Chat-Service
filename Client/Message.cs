using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Message
    {
        public int ClientId { get; set; }
        public string Text { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
       public Message(int clientId, string text, string name)
        {
            ClientId = clientId;
            Text = text;
            Name = name;
        }  
        public Message()
        {
            this.Text = "";
        }
    }

}
