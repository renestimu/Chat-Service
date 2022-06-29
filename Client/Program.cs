using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Client
{
    internal class Program
    {
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


        private static Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static byte[] receivedBuf = new byte[1024];
        static string name = "";

        static Message message = new Message();

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(Handler, true);
            Console.WriteLine("Welcome to the chat.");

            ReadName();
        }

        private static void ReadName()
        {
            Console.WriteLine("Please enter your name.(Do not use special characters.) ");

            while (true)
            {
                string deger = Console.ReadLine();
                if (Regex.Replace(deger, @"\s+", "") != "")
                {
                    name = Regex.Replace(deger, @"/[^a - zA - Z0 - 9] / g", "");
                    message.Name = name;
                    message.ClientId = -1;
                    message.Text = "";
                    message.Status = Status.Connect;
                    break;
                }
                else
                {
                    Console.WriteLine("Please do not enter an empty value");
                }
            }
            LoopConnect();
            Console.WriteLine("Please enter the message you want to write");
            CreateMessage();

            Console.ReadLine();
        }

        private static void CreateMessage()
        {

            while (true)
            {
                Console.Write(name + " : ");
                string deger = Console.ReadLine();
                if (Regex.Replace(deger, @"\s+", "") != "")
                {
                    Send_Message(deger);
                  
                }
                else
                {
                    Console.WriteLine("Please do not enter an empty value");
                }
            }
        }

        private static void ReceiveData(IAsyncResult ar)
        {


            try
            {

                Socket socket = (Socket)ar.AsyncState;
                int received = socket.EndReceive(ar);
                byte[] dataBuf = new byte[received];
                Array.Copy(receivedBuf, dataBuf, received);

                Message msg = Converter.FromBytes(dataBuf);
                _clientSocket.BeginReceive(receivedBuf, 0, receivedBuf.Length, SocketFlags.None, new AsyncCallback(ReceiveData), _clientSocket);
                if (msg.Status == Status.Id)
                {
                    message.ClientId = msg.ClientId;
                }
                else if (msg.Status == Status.Connect)
                {
                    Console.WriteLine("\r" + msg.Name + " is online now");
                    CreateMessage();

                }else if (msg.Status == Status.Offline)
                {
                    Console.WriteLine("\r" + msg.Name + " is offline now");
                    CreateMessage();

                }
                else if (msg.Status == Status.Message)
                {
                    Console.WriteLine("\r" + msg.Name + " :" + msg.Text);
                    CreateMessage();
                }
                else if (msg.Status == Status.Warning)
                {
                    Console.WriteLine("\r You are trying to send a lot of messages in a short time. In the next case, you will be kicked out of the chat.");
                    CreateMessage();
                }
                else if (msg.Status == Status.Close)
                {
                    Console.WriteLine("\r The app will now close because the server is down.");
                    CloseApp();

                }
                else if (msg.Status == Status.Kick)
                {
                    Console.WriteLine("\r You've been kicked out of the chat.");
                    CloseApp();

                }


               


            }
            catch (Exception e)
            {
                Console.WriteLine("The app will now close because the server is down. ");
                CloseApp();
            }

        }

        private static void LoopConnect()
        {
            int attempts = 0;
            while (!_clientSocket.Connected)//server çalışmıyorsa(çalışısaya kadar döngü döner)
            {
                try
                {
                    attempts++;
                    _clientSocket.Connect("127.0.0.1", 100);//127.0.0.1=IPAddress.Loopback demek 100 portuna bağlan
                }
                catch (SocketException ex)
                {
                    if (attempts > 10)
                    {
                        Console.WriteLine("Could not connect to the server. Please try again later.");
                        CloseApp();
                    }
                    else
                    {
                        Console.WriteLine("Unable to connect to server");
                        Console.WriteLine("Reconnecting: " + attempts.ToString());
                        Thread.Sleep(20);
                    }
                }

            }

            _clientSocket.BeginReceive(receivedBuf, 0, receivedBuf.Length, SocketFlags.None, new AsyncCallback(ReceiveData), _clientSocket);

            byte[] buffer = Converter.ToBytes(message);

            _clientSocket.Send(buffer);
            Console.WriteLine("Connected to the server!");

        }

        private static void CloseApp()
        {

            if (_clientSocket.Connected)
            {
                message.Text = "";
                message.Status = Status.Close;

                byte[] buffer = Converter.ToBytes(message);
                _clientSocket.Send(buffer);
                Thread.Sleep(20);
            }

            Console.WriteLine("The app will close in 5 seconds.");
            for (int i = 5; i >= 0; i--)
            {
                Thread.Sleep(1000);
                Console.Write("\r{0} seconds..", i);

            }

            Environment.Exit(0);
        }

        private static void Send_Message(string msj)
        {

            if (_clientSocket.Connected)
            {

                message.Text = msj;
                message.Status = Status.Message;

                byte[] buffer = Converter.ToBytes(message);
                _clientSocket.Send(buffer);
                Thread.Sleep(20);


            }
            else
            {
                Console.WriteLine("Bağalantı koptu");
            }
        }




    }
}
