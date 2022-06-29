using Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Server
{
    public class Program
    {
        private static byte[] _buffer = new byte[1024];
        public static List<string> _names = new List<string>();
        public static List<Socket2h> __ClientSockets { get; set; }

        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

     

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);

        private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    Console.WriteLine("Closing");
                    CloseApp();
                    return false;

                default:
                    return false;
            }
        }

        private static void CloseApp()
        {
            CloseAllSocket();
           
            _serverSocket.Shutdown(SocketShutdown.Send);
        }

        private static void CloseAllSocket()
        {
            for (int j = 0; j < __ClientSockets.Count; j++)
            {
                if (__ClientSockets[j]._Socket.Connected)
                {
                    __ClientSockets[j]._Message.Status = Status.Close;
                    Sendata(__ClientSockets[j]._Socket, __ClientSockets[j]._Message);
                    Thread.Sleep(20);
                }
            }
        }

        static void Main(string[] args)
        {
            __ClientSockets = new List<Socket2h>();
            SetConsoleCtrlHandler(Handler, true);
            SetupServer();
        }

        //AsyncCallback demek thread gibi eş zamansız çalışabılıyor
        private static void SetupServer()
        {
            Console.WriteLine("Server started . . .");

            _serverSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 100));
            _serverSocket.Listen(1);

            _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
            Console.WriteLine("Listening . . . ");
            Console.ReadLine();

        }

        private static void AppceptCallback(IAsyncResult ar)
        {
            Console.WriteLine("Conected");
            Socket socket = _serverSocket.EndAccept(ar);
            __ClientSockets.Add(new Socket2h(socket));


            Console.WriteLine("Connecting socket = " + socket.RemoteEndPoint.ToString());

            Console.WriteLine("Client Connectted. . .");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            Console.WriteLine("ReceiveCallback working");
            _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
            Console.WriteLine("AppceptCallback recursive metod active");
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {

            Socket socket = (Socket)ar.AsyncState;

            if (socket.Connected)
            {
                int received;
                try
                {
                    received = socket.EndReceive(ar);
                }
                catch (Exception)
                {
                    ClientDeleted(socket);
                    return;
                }
                if (received != 0)
                {
                    byte[] dataBuf = new byte[received];

                    Array.Copy(_buffer, dataBuf, received);

                    Message msg = Converter.FromBytes(dataBuf);

                    if (msg != null)
                    {

                        if (msg.Status == Status.Connect)
                        {

                            ClientIdCreate(socket, msg);
                            return;

                        }
                        else if (msg.Status == Status.Close)
                        {
                            ClientDeleted(socket);
                            return;
                        }
                        else if (msg.Status == Status.Message)
                        {
                            if (SpamControl(socket, msg))
                                SendReciveMessage(msg);
                            else
                                KickToClient(socket, msg);
                         
                        }

                    }

                }
                else
                {
                    ClientDeleted(socket);
                }
            }
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        private static void KickToClient(Socket socket, Message msg)
        {
            msg.Status = Status.Kick;
            byte[] data = Converter.ToBytes(msg);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
        }

        private static bool SpamControl(Socket socket, Message msg)
        {
            DateTime time = DateTime.Now;
            for (int i = 0; i < __ClientSockets.Count; i++)
            {
                if (socket.RemoteEndPoint.ToString().Equals(__ClientSockets[i]._Socket.RemoteEndPoint.ToString()))
                {
                    TimeSpan duration = time.Subtract(__ClientSockets[i]._Time);
                    Console.WriteLine(__ClientSockets[i]._Message.Name+" " +duration);
                    if (duration.Seconds < 1.1)
                    {
                        
                        if (__ClientSockets[i]._WarningCount > 0)
                            return false;
                        else
                        {
                            __ClientSockets[i]._WarningCount += 1;
                            SendWarning(socket, __ClientSockets[i]._Message);
                        }
                    }
                    __ClientSockets[i]._Time = DateTime.Now;
                   
                }
            }
            return true;
        }

        private static void SendWarning(Socket socket, Message msg)
        {
            msg.Status = Status.Warning;
            byte[] data = Converter.ToBytes(msg);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
        }

        private static void ClientIdCreate(Socket socket, Message msg)
        {
            int clientid = __ClientSockets.Count > 1 ? (__ClientSockets[__ClientSockets.Count - 2]._Message.ClientId != null ? __ClientSockets[__ClientSockets.Count - 2]._Message.ClientId + 1 : 0) : 0;

            for (int i = 0; i < __ClientSockets.Count; i++)
            {
                if (socket.RemoteEndPoint.ToString().Equals(__ClientSockets[i]._Socket.RemoteEndPoint.ToString()))
                {
                    __ClientSockets[i]._Message.ClientId = clientid;
                    __ClientSockets[i]._Message.Name = msg.Name;
                    Console.WriteLine(msg.Name + " is connected");
                    socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                    SendName(socket, msg);
                    ClientIdSend(socket, __ClientSockets[i]._Message);


                }
            }
        }

        private static void ClientIdSend(Socket socket, Message msg)
        {
            msg.Status = Status.Id;
            byte[] data = Converter.ToBytes(msg);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
        }

        private static void ClientDeleted(Socket socket)
        {
            Message m = new Message();
            for (int i = 0; i < __ClientSockets.Count; i++)
            {
                if (__ClientSockets[i]._Socket.RemoteEndPoint.ToString().Equals(socket.RemoteEndPoint.ToString()))
                {
                    m = __ClientSockets[i]._Message;
                    m.Status = Status.Offline;

                    Console.WriteLine("Client ended " + __ClientSockets[i]._Message.Name);
                    __ClientSockets.RemoveAt(i);
                }
            }
            ClientDeletedMessage(m);
        }


        public static void SendName(Socket socket, Message msg)
        {
            for (int j = 0; j < __ClientSockets.Count; j++)
            {
                if (__ClientSockets[j]._Socket.Connected)
                {
                    if (!__ClientSockets[j]._Socket.RemoteEndPoint.ToString().Equals(socket.RemoteEndPoint.ToString()))
                    {
                        Sendata(__ClientSockets[j]._Socket, msg);
                        Thread.Sleep(20);
                    }

                }
            }
        }



        public static void SendReciveMessage(Message msg)
        {
            Console.WriteLine(msg.Name + " :" + msg.Text);

            try
            {
                for (int j = 0; j < __ClientSockets.Count; j++)
                {
                    if (__ClientSockets[j]._Socket.Connected)
                    {
                        if (__ClientSockets[j]._Message.ClientId != msg.ClientId)
                        {
                            
                            Sendata(__ClientSockets[j]._Socket, msg);
                            Thread.Sleep(20);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Send_Recive_Message() error " + e.Message);
            }
        }

        public static void ClientDeletedMessage(Message msg)
        {
            for (int j = 0; j < __ClientSockets.Count; j++)
            {
                if (__ClientSockets[j]._Socket.Connected)
                {
                    Sendata(__ClientSockets[j]._Socket, msg);
                    Thread.Sleep(20);
                }
            }
        }
        static void Sendata(Socket socket, Message msg)
        {
            byte[] data = Converter.ToBytes(msg);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AppceptCallback), null);
        }
        private static void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

    }
}
