using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class User
    {
        public int id {  get; set; }
        /// <summary>
        /// Логин пользователя
        /// </summary>
        public string login {  get; set; }
        /// <summary>
        /// Пароль пользователя
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// Директория пользователя
        /// </summary>
        public string src { get; set; }
        /// <summary>
        /// Директория пользователя в которой он находится
        /// </summary>
        public string temp_src { get; set; }
        /// <summary>
        /// Конструктор для заполнения класса
        /// </summary>
        public User(int id, string login, string password, string src)
        {
            this.id = id;
            this.login = login;
            this.password = password;
            this.src = src;

            temp_src = src;
        }
    }
}
