using GSCLab.Infrastructure;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GSCLab.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private string _regStatusMessage;
        public string RegStatusMessage
        {
            get => _regStatusMessage;
            set
            {
                _regStatusMessage = value;
                OnPropertyChanged();
            }
        }

        private string _regStatusColor = "#CC3333";
        public string RegStatusColor
        {
            get => _regStatusColor;
            set { _regStatusColor = value; OnPropertyChanged(); }
        }

        private string _regUsername;
        public string RegUsername
        {
            get => _regUsername;
            set
            {
                _regUsername = value;
                OnPropertyChanged();
            }
        }

        private bool _acceptedTerms;
        public bool AcceptedTerms
        {
            get => _acceptedTerms;
            set
            {
                _acceptedTerms = value;
                OnPropertyChanged();
            }
        }

        public ICommand ViewTermsCommand { get; }
        public ICommand RegisterCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(async (p) => await ExecuteRegister(p));

            ViewTermsCommand = new RelayCommand((p) => {
                var termsWindow = new GSCLab.Views.TermsView();
                termsWindow.Owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().SingleOrDefault(x => x.IsActive);
                termsWindow.ShowDialog();
            });
        }

        

        private async Task ExecuteRegister(object parameter)
        {
            RegStatusMessage = "Processing...";
            RegStatusColor = "#007ACC";

            var window = parameter as System.Windows.Window;
            var pBox = window?.FindName("RegPass") as System.Windows.Controls.PasswordBox;
            var cBox = window?.FindName("RegPassConfirm") as System.Windows.Controls.PasswordBox;

            string pass = pBox?.Password ?? "";
            string confirm = cBox?.Password ?? "";

            if (string.IsNullOrEmpty(RegUsername) || string.IsNullOrEmpty(pass))
            {
                RegStatusColor = "#CC3333";
                RegStatusMessage = "All fields are required.";
                return;
            }

            if(RegUsername.Length < 3)
            {
                RegStatusColor = "#CC3333";
                RegStatusMessage = "Username is to short.";
            }

            if(pass.Length <= 3)
            {
                RegStatusColor = "#CC3333";
                RegStatusMessage = "Password is to short.";
                return;
            }

            if (pass != confirm)
            {
                RegStatusColor = "#CC3333";
                RegStatusMessage = "Passwords do not match.";
                return;
            }

            if(!AcceptedTerms)
            {
                RegStatusColor = "#CC3333";
                RegStatusMessage = "Please accept the Terms of Service to continue.";
                return;
            }

            try
            {
                string hwid = Infrastructure.Security.GetFingerprint();
                string vpsIp = "45.90.116.64";
                int vpsPort = 8888;

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(vpsIp, vpsPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        string regData = $"REGISTER|{RegUsername}|{pass}|{hwid}";
                        byte[] data = Encoding.UTF8.GetBytes(regData);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        switch (response)
                        {
                            case "SUCCESS":
                                RegStatusColor = "#4BB543";
                                RegStatusMessage = "Registered! You can now login.";
                            break;
                            case "ERR_TAKEN":
                                RegStatusColor = "#CC3333";
                                RegStatusMessage = "Username already exists.";
                            break;
                            case "ERR_HWID_EXISTS":
                                RegStatusColor = "#CC3333";
                                RegStatusMessage = "This PC is already linked to an account.";
                            break;
                            default:
                                RegStatusColor = "#CC3333";
                                RegStatusMessage = "Registration failed.";
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                RegStatusColor = "#CC3333";
                RegStatusMessage = "Could not connect to server.";
            }
        }
    }
}