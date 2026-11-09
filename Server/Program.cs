using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {

        }
        /// <summary>
        /// Список пользователей
        /// </summary>
        public static List<User> Users = new List<User>();
        /// <summary>
        /// Ip-адрес сервера
        /// </summary>
        public static IPAddress IpAddress;
        /// <summary>
        /// Порт сервера
        /// </summary>
        public static int Port;

        
    }
}
