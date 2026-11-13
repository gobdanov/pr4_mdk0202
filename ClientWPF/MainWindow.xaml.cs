using Common;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string serverIp = "127.0.0.1";
        public int serverPort = 5000;
        public int Id = -1;
        public string lastCommand = "cd";
        public ObservableCollection<FileItem> items = new ObservableCollection<FileItem>();
        public MainWindow()
        {
            InitializeComponent();
            lvFiles.ItemsSource = items;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Login())
            {
                Close();
                return;
            }
            RefreshList();
        }

        public ViewModelMessage SendToServer(string message)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(endPoint);
                    ViewModelSend send = new ViewModelSend(message, Id);
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(send));
                    socket.Send(bytes);
                    byte[] buffer = new byte[10485760];
                    int received = socket.Receive(buffer);
                    string json = Encoding.UTF8.GetString(buffer, 0, received);

                    return JsonConvert.DeserializeObject<ViewModelMessage>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка связи с сервером: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private bool Login()
        {
            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() != true) return false;

            serverIp = loginWindow.ServerIp;
            serverPort = loginWindow.ServerPort;

            var reply = SendToServer($"connect {loginWindow.Login} {loginWindow.Password}");
            if (reply == null) return false;

            if (reply.Command == "autorization")
            {
                Id = int.Parse(reply.Data);
                lblPath.Text = "/";
                lastCommand = "cd";
                return true;
            }

            MessageBox.Show(reply.Data, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void RefreshList()
        {
            if (Id == -1 && !Login()) return;

            var reply = SendToServer(lastCommand);
            if (reply == null) return;

            if (reply.Command != "cd")
            {
                MessageBox.Show(reply.Data, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowList(reply.Data);
        }

        private void LvFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvFiles.SelectedItem is not FileItem item) return;

            if (item.IsFolder)
            {
                lastCommand = "cd " + item.Name;
                var reply = SendToServer(lastCommand);
                if (reply == null) return;

                if (reply.Command == "cd")
                {
                    lblPath.Text = lblPath.Text.TrimEnd('/') + "/" + item.Name;
                    ShowList(reply.Data);
                }
                else
                {
                    MessageBox.Show(reply.Data, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                DownloadFile(item.Name);
            }
        }

        private void ShowList(string data)
        {
            items.Clear();

            List<string> names;
            try
            {
                names = JsonConvert.DeserializeObject<List<string>>(data);
            }
            catch
            {
                return;
            }

            if (names == null) return;

            foreach (string name in names)
            {
                bool isFolder = name.EndsWith("/");
                items.Add(new FileItem
                {
                    Name = name.TrimEnd('/'),
                    Type = isFolder ? "Папка" : "Файл",
                    FullName = name
                });
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (lvFiles.SelectedItem is not FileItem item)
            {
                MessageBox.Show("Выберите файл в списке.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (item.IsFolder)
            {
                MessageBox.Show("Это папка, а не файл.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DownloadFile(item.Name);
        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (Id == -1 && !Login()) return;

            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != true) return;

            FileInfo fi = new FileInfo(dialog.FileName);
            FileInfoFTP info = new FileInfoFTP(File.ReadAllBytes(dialog.FileName), fi.Name);

            string message = JsonConvert.SerializeObject(info);
            var reply = SendToServer(message);
            if (reply == null) return;

            MessageBox.Show(reply.Data, "Сервер", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshList();
        }

        private void DownloadFile(string name)
        {
            var reply = SendToServer("get " + name);
            if (reply == null) return;

            if (reply.Command != "file")
            {
                MessageBox.Show(reply.Data, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog { FileName = name };
            if (saveDialog.ShowDialog() != true) return;

            byte[] data = JsonConvert.DeserializeObject<byte[]>(reply.Data);
            File.WriteAllBytes(saveDialog.FileName, data);

            MessageBox.Show("Файл сохранён:\n" + saveDialog.FileName, "Готово",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}