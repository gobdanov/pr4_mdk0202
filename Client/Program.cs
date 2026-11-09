using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            if(DataMessage.Length > 0)
            {
                string Command = DataMessage[0];
                if (Command == "connect")
                {
                    if(DataMessage.Length != 3)
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
                else if (Command = "get")
            }
        }
    }
}
