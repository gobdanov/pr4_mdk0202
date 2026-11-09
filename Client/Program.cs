using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        public static IPAddress IpAddress;
        public static int Port;
        public static int Id = -1;
        static void Main(string[] args)
        {

        }
        public static bool CheckCommand(string message)
        {
            bool Bcommand = false;
            string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

            if (DataMessage.Length > 0)
            {
                string Command = DataMessage[0];
                if (Command == "connect")
                {
                    if (DataMessage.Length != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: connect [login] [password]\nПример: connect User1 P@ssword");
                        Bcommand = false;
                    }
                    else
                    {
                        Bcommand = true;
                    }
                }
                else if (Command == "cd")
                {
                    Bcommand = true;
                }
                else if (Command == "get")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                        Bcommand = false;
                    }
                    else
                    {
                        Bcommand = true;
                    }
                }
                else if (Command == "set")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: set [NameFile]\nПример: set Test.txt");
                        Bcommand = false;
                    }
                    else
                    {
                        Bcommand = false;
                    }
                }
            }
            return Bcommand;
        }

        public static void ConnectServer()
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);
                Socket socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                socket.Connect(endPoint);
                if (socket.Connected)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    string message = Console.ReadLine();
                    if (CheckCommand(message))
                    {
                        ViewModelSend viewModelSend = new ViewModelSend(message, Id);
                        if (message.Split(new string[1] { " " }, StringSplitOptions.None)[0] == "set")
                        {
                            string[] DataMessage = message.Split(new string[1] { " " }, StringSplitOptions.None);

                            string NameFile = "";
                            for (int i = 1; i < DataMessage.Length; i++)
                            {
                                if (NameFile == "")
                                {
                                    NameFile += DataMessage[i];
                                }
                                else
                                {
                                    NameFile += " " + DataMessage[i];
                                }
                            }
                            if (File.Exists(NameFile))
                            {
                                FileInfo fileInfo = new FileInfo(NameFile);
                                FileInfoFTP NewFileInfo = new FileInfoFTP(File.ReadAllBytes(NameFile), FileInfo.Name);
                                viewModelSend = new ViewModelSend(JsonConvert.SerializeObject(NewFileInfo), Id);
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Указанный файл не существует");
                            }
                        }
                        byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                        int BytesSend = socket.Send(messageByte);
                        byte[] bytes = new byte[10485760];
                        int BytesRec = socket.Receive(bytes);
                        string MessageeServer = Encoding.UTF8.GetString(bytes, 0, BytesRec);
                        ViewModelMessage viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(MessageeServer);

                        if (viewModelMessage.Command == "authorization")
                        {
                            Id = int.Parse(viewModelMessage.Data);
                        }
                        else if (viewModelMessage.Command == "message")
                        {
                            Console.WriteLine(viewModelMessage.Data);
                        }
                        else if (viewModelMessage.Command == "cd")
                        {
                            List<string> FolderFies = new List<string>();

                            //тут писать дальше

                        }
                        

                    }
                }
            }
        }
    }
}
