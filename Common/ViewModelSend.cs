namespace Common
{
    public class ViewModelSend
    {

        /// <summary>
        /// Сообщение отправлямое сервером
        /// </summary>
        public string Message {  get; set; }
       /// <summary>
       /// Код пользователя
       /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Конструктор для заполнения класса 
        /// </summary>
        public ViewModelSend(string message, int id)
        {
            this.Message = message;
            this.Id = id;
        }
    }
}   
