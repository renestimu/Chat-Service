using Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Socket2h
    {
        public Socket _Socket { get; set; }
        //public string _Name { get; set; }
        //public int _ClientId { get; set; }
        public Message _Message { get; set; }
        public int _WarningCount { get; set; }
        public DateTime _Time { get; set; }
        public Socket2h(Socket socket)
        {
            this._Socket = socket;
            this._WarningCount = 0;
            this._Message = new Message();
           this._Time = DateTime.Now;
           
        }
    }
}
