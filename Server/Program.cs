using Common;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        /// <summary>
        /// Ip-адрес сервера
        /// </summary>
        public static IPAddress IPAddress;
        /// <summary>
        /// Порт сервера
        /// </summary>
        public static int Port;

        public const string Path = "server=127.0.0.1;uid=root;pwd=;database=ftp_server;";
        public static Dictionary<int, User> ActiveUsers = new Dictionary<int, User>();

        static void Main(string[] args)
        {
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

        public static User AutoriztionUser(string login, string password)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Path))
                {
                    conn.Open();
                    string sql = "SELECT id, login, password, src FROM users WHERE login=@l AND password=@p";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@l", login);
                    cmd.Parameters.AddWithValue("@p", password);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (!reader.Read())
                        return null;

                    return new User(
                        reader.GetInt32("id"),
                        reader.GetString("login"),
                        reader.GetString("password"),
                        reader.GetString("src"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static void LogCommand(int userId, string command)
        {
            if (userId == -1) return;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Path))
                {
                    conn.Open();

                    string sql = "INSERT INTO log (user_id, command) VALUES (@u, @c);";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@u", userId);
                    cmd.Parameters.AddWithValue("@c", command);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
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
                            User user = AutoriztionUser(DataMessage[1], DataMessage[2]);
                            if (user != null)
                            {
                                ActiveUsers[user.id] = user;
                                viewModelMessage = new ViewModelMessage("autorization", user.id.ToString());
                                viewModelSend.Id = user.id;
                                LogCommand(user.id, "connect" + DataMessage[1]);
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
                                User user = ActiveUsers[viewModelSend.Id];
                                List<string> FoldersFiles = new List<string>();
                                if (DataMessage.Length == 1)
                                {
                                    user.temp_src = user.src;
                                    FoldersFiles = GetDirectory(user.src);
                                }
                                else
                                {
                                    string cdFolder = "";
                                    for (int i = 1; i < DataMessage.Length; i++)
                                        if (cdFolder == "")
                                            cdFolder += DataMessage[i];
                                        else
                                            cdFolder += " " + DataMessage[i];
                                    user.temp_src = user.temp_src + cdFolder;
                                    FoldersFiles = GetDirectory(user.temp_src);
                                }
                                if (FoldersFiles.Count == 0)
                                    viewModelMessage = new ViewModelMessage("cd", "Директория пуста или не существует");
                                else
                                    viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FoldersFiles));
                                LogCommand(viewModelSend.Id, viewModelSend.Message);
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
                                User user = ActiveUsers[viewModelSend.Id];
                                string FullPath = user.temp_src + getFile;
                                if(!File.Exists(FullPath))
                                {
                                    viewModelMessage = new ViewModelMessage("message", "Файл не найден");
                                }
                                else
                                {
                                    byte[] byteFile = File.ReadAllBytes(FullPath);
                                    viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));

                                    LogCommand(viewModelSend.Id, viewModelSend.Message);
                                }
                            }
                            else
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться");
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);  
                        }
                        else
                        {
                            User user = ActiveUsers[viewModelSend.Id];
                            if (viewModelSend.Id != -1)
                            {
                                string[] DataMessage = viewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                                FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(viewModelSend.Message);
                                File.WriteAllBytes(user.temp_src + @"\" + SendFileInfo.Name, SendFileInfo.Data);
                                viewModelMessage = new ViewModelMessage("message", "Файл загружен");

                                LogCommand(viewModelSend.Id, "set " + SendFileInfo.Name);
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
