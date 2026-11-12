using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            Users.Add(new User("vlad", "123456", @"C:\\Users\\vadim_blagoveshensk\\Documents\\VisualStudoPr4\\"));
            Users.Add(new User("vadim", "123456", @"C:\\Users\\vadim_blagoveshensk\\Documents\\VisualStudoPr4\\"));
            Console.Write("Введите IP сервера: ");
            string sIpAdress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();
            if (IPAddress.TryParse(sIpAdress, out IPAddress) && int.TryParse(sPort, out Port))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Запускаю сервер");
                StartServer();
            }

            Console.Read();
        }
        /// <summary>
        /// Список пользователей
        /// </summary>
        public static List<User> Users = new List<User>();
        /// <summary>
        /// Ip-адрес сервера
        /// </summary>
        public static IPAddress IPAddress;
        /// <summary>
        /// Порт сервера
        /// </summary>
        public static int Port;

        public static bool AutorizationUser(string login, string password)
        {
            User user = null;
            user = Users.Find(x => x.login == login && x.password == password);
            return user != null;
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> FoldersFiles = new List<string>();
            if(Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach(string dir in dirs)
                {
                    string NameDirectory = dir.Replace(src, "");
                    FoldersFiles.Add(NameDirectory + "/");
                }

                string[] files = Directory.GetFiles(src);
                foreach(string file in files)
                {
                    string NameFile = file.Replace(src, "");
                    FoldersFiles.Add(NameFile);
                }
            }

            return FoldersFiles;
        }

        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress, Port);
            Socket sListener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
                );
            sListener.Bind(endPoint);
            sListener.Listen(10);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен");
            while(true)
            {
                try
                {
                    Socket Handler = sListener.Accept();
                    string Data = null;
                    byte[] Bytes = new byte[10485760];
                    int BytesRec = Handler.Receive(Bytes);
                    Data += Encoding.UTF8.GetString(Bytes, 0, BytesRec);
                    Console.Write("Сообщение от пользователя: " + Data + "\n");
                    string Reply = "";
                    ViewModelSend viewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);
                    if(viewModelSend != null)
                    {
                        ViewModelMessage viewModelMessage;
                        string[] DataCommand = viewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                        if (DataCommand[0] == "connect")
                        {
                            string[] DataMessage = viewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                            if (AutorizationUser(DataMessage[1], DataMessage[2]))
                            {
                                int IdUser = Users.FindIndex(x => x.login == DataMessage[1] && x.password == DataMessage[2]);
                               
                                viewModelMessage = new ViewModelMessage("autorization", IdUser.ToString());
                                viewModelSend.Id = IdUser;
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Не правильный логин и пароль пользователя");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "cd")
                        {
                            if (viewModelSend.Id != -1)
                            {
                                string[] DataMessage = viewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                                List<string> FoldersFiles = new List<string>();
                                if (DataMessage.Length == 1)
                                {
                                    Users[viewModelSend.Id].temp_src = Users[viewModelSend.Id].src;
                                    Users[viewModelSend.Id].temp_src = Users[viewModelSend.Id].src;
                                    FoldersFiles = GetDirectory(Users[viewModelSend.Id].src);
                                }
                                else
                                {
                                    string cdFolder = "";
                                    for (int i = 1; i < DataMessage.Length; i++)
                                        if (cdFolder == "")
                                            cdFolder += DataMessage[i];
                                        else
                                            cdFolder += " " + DataMessage[i];
                                    Users[viewModelSend.Id].temp_src = Users[viewModelSend.Id].temp_src + cdFolder;
                                    FoldersFiles = GetDirectory(Users[viewModelSend.Id].temp_src);
                                }
                                if (FoldersFiles.Count == 0)
                                    viewModelMessage = new ViewModelMessage("cd", "Директория пуста или не существует");
                                else
                                    viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FoldersFiles));
                            }
                            else
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "get")
                        {
                            if (viewModelSend.Id != -1)
                            {
                                string[] DataMessage = viewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                                string getFile = "";
                                for (int i = 1; i < DataMessage.Length; i++)
                                    if (getFile == "")
                                        getFile += DataMessage[i];
                                    else
                                        getFile += " " + DataMessage[i];
                                byte[] byteFile = File.ReadAllBytes(Users[viewModelSend.Id].temp_src + getFile);
                                viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));
                            }
                            else
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else
                        {
                            if (viewModelSend.Id != 1)
                            {
                                FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(viewModelSend.Message);
                                File.WriteAllBytes(Users[viewModelSend.Id].temp_src + @"\" + SendFileInfo.Name, SendFileInfo.Data);
                                viewModelMessage = new ViewModelMessage("message", "Файл загружен");
                            }
                            else
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                    }
                }
                catch(Exception  exp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Что-то случилось" + exp.Message);
                }
            }
        }
    }
}
