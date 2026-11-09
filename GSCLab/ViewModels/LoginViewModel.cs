using GSCLab.Views;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GSCLab.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;
        private string _statusMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _statusColor = "#CC3333";
        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        private bool _rememberMe;
        public bool RememberMe
        {
            get => _rememberMe;
            set { _rememberMe = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Infrastructure.RelayCommand(async (param) => await ExecuteLogin(param)!);
        }

        private async Task<string> ReadFullResponseAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[2048];
            using (var ms = new MemoryStream())
            {
                int bytesRead;
                while (stream.DataAvailable && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private async Task ExecuteLogin(object parameter)
        {
            Infrastructure.Session.Username = Username;
            var window = parameter as System.Windows.Window;
            StatusMessage = "";
            StatusColor = "#CC3333";

            if (string.IsNullOrEmpty(Username))
            {
                StatusMessage = "Please enter a username.";
                return;
            }

            var passwordBox = window?.FindName("PassBox") as System.Windows.Controls.PasswordBox;
            string password = passwordBox?.Password ?? "";

            if (string.IsNullOrEmpty(password))
            {
                StatusMessage = "Please enter a password.";
                return;
            }

            StatusMessage = "Connecting to GSCLab...";

            try
            {
                string vpsIp = "45.90.116.64";
                int vpsPort = 8888;

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(vpsIp, vpsPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        StatusMessage = "Connection Timed Out.";
                        return;
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string hwid = Infrastructure.Security.GetFingerprint();
                        string loginData = $"LOGIN|{Username}|{password}|{hwid}";
                        byte[] data = Encoding.UTF8.GetBytes(loginData);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        string[] parts = response.Split('|');
                        string status = parts[0];

                        switch (status)
                        {
                            case "SUCCESS":
                                StatusColor = "#4BB543";
                                StatusMessage = "Login Successful!";

                                if (parts.Length >= 5)
                                {
                                    Infrastructure.Session.Username = Username;
                                    Infrastructure.Session.Token = parts[1];
                                    Infrastructure.Session.Role = parts[2];
                                    Infrastructure.Session.Stars = double.TryParse(parts[3], out double s) ? s : 1.0;
                                    Infrastructure.Session.IsTester = parts[4] == "1";
                                }

                                Infrastructure.AppSettings.Save(RememberMe ? Username : "", RememberMe ? password : "", RememberMe);

                                StatusMessage = "Checking for updates...";
                                string currentVersion = "0.0.4";

                                try
                                {
                                    using (TcpClient updateClient = new TcpClient())
                                    {
                                        await updateClient.ConnectAsync("45.90.116.64", 8888);
                                        using (NetworkStream updateStream = updateClient.GetStream())
                                        {
                                            byte[] cmd = Encoding.UTF8.GetBytes("GET_VERSION");
                                            await updateStream.WriteAsync(cmd, 0, cmd.Length);

                                            await Task.Delay(100);

                                            string json = await ReadFullResponseAsync(updateStream);

                                            if (!json.StartsWith("ERR_"))
                                            {
                                                var serverData = System.Text.Json.JsonSerializer.Deserialize<UpdateInfo>(json);

                                                if (serverData != null)
                                                {
                                                    bool updateAvailable = new Version(serverData.Version) > new Version(currentVersion);
                                                    if (updateAvailable)
                                                    {
                                                        var uiHistory = new List<UpdateItem>
                                                        {
                                                            new UpdateItem
                                                            {
                                                                VersionTag = $"UPDATE {serverData.Version}",
                                                                StatusColor = "#28a745",
                                                                Date = DateTime.Now.ToString("MMMM yyyy").ToUpper(),
                                                                Description = serverData.Changelog,
                                                                DownloadUrl = "45.90.116.64:8888"
                                                            }
                                                        };

                                                        var updateWindow = new UpdateView(uiHistory);
                                                        updateWindow.Show();
                                                        window?.Close();
                                                        return;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                                }

                                await Task.Delay(500);

                                var mainDash = new GSCLab.Views.MainView();
                                mainDash.Show();
                                window?.Close();
                                break;

                            case "ERR_NOT_TESTER": StatusMessage = "Unauthorized: Tester access only."; break;
                            case "ERR_USER": StatusMessage = "User not found."; break;
                            case "ERR_PASS": StatusMessage = "Invalid password."; break;
                            case "ERR_HWID_MISMATCH": StatusMessage = "Account locked to another PC."; break;
                            case "ERR_BANNED": StatusMessage = "Account permanently banned."; break;
                            default: StatusMessage = "Invalid Credentials."; break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Server is offline.";
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        public class UpdateInfo
        {
            public string Version { get; set; }
            public string Changelog { get; set; }
            public string DownloadUrl { get; set; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}