using GSCLab.Infrastructure;
using GSCLab.Views;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace GSCLab.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        CompilerView Compiler = new CompilerView();
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private async Task PerformLogoutAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("45.90.116.64", 8888);

                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) == connectTask)
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] data = Encoding.UTF8.GetBytes($"LOGOUT|{Session.Username}");
                            await stream.WriteAsync(data, 0, data.Length);
                            await Task.Delay(100);
                        }
                    }
                }
            }
            catch { _ = Discord.SendDiscordError("Error: Logging " + Session.Username + " Out"); }
        }

        public ICommand NavCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            CurrentView = new HomeViewModel();

            NavCommand = new RelayCommand((p) => {
                if (p == null) return;
                string destination = p.ToString()!;

                if (destination == "Home")
                    CurrentView = new HomeViewModel();
                else if (destination == "Compiler")
                    CurrentView = Compiler;
                else if (destination == "Library")
                    CurrentView = new LibraryView();
                else if (destination == "Account")
                    CurrentView = new AccountView();
                //else if (destination == "Chat")
                    //CurrentView = new ChatView();
            });

            LogoutCommand = new RelayCommand(async (p) => {
                var currentWin = p as System.Windows.Window;

                await PerformLogoutAsync();

                new Views.LoginView().Show();
                currentWin?.Close();
            });
        }
    }
}