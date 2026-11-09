using GSCLab.Infrastructure;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace GSCLab.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            Discord.Initialize();
            this.DataContext = new ViewModels.MainViewModel();
            //LoggedInAsUsername.Text = "Logged In As " + Session.Username;

            this.Closed += (s, e) => Discord.Shutdown();

            this.SourceInitialized += (s, e) =>
            {
                IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                this.MaxHeight = SystemParameters.WorkArea.Height + 10;
                this.MaxWidth = SystemParameters.WorkArea.Width + 10;
            };
        }

        private void PerformLogout()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect("45.90.116.64", 8888, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(500, true);

                    if (success && client.Connected)
                    {
                        using (NetworkStream stream = client.GetStream())
                        {
                            byte[] data = Encoding.UTF8.GetBytes($"LOGOUT|{Session.Username}");
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
            catch
            {
                _ = Discord.SendDiscordError("Error: Logging " + Session.Username + " Out");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var viewModel = this.DataContext as GSCLab.ViewModels.MainViewModel;

            if (viewModel?.CurrentView is GSCLab.Views.CompilerView compiler)
            {
                if (!compiler.CanCloseProject())
                {
                    e.Cancel = true;
                    return;
                }
            }
            PerformLogout();
            base.OnClosing(e);
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaxBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                MaxBtn.Content = "2";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                MaxBtn.Content = "1";
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    MaxBtn_Click(sender, null);
                }
                else
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        Point mousePos = e.GetPosition(this);
                        double widthPct = mousePos.X / ActualWidth;

                        Point screenPos = PointToScreen(mousePos);

                        this.WindowState = WindowState.Normal;

                        this.Left = screenPos.X - (this.ActualWidth * widthPct);
                        this.Top = screenPos.Y - mousePos.Y;
                    }

                    this.DragMove();
                }
            }
        }

        private void Github_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/zGSCLabs/GSC-Files",
                UseShellExecute = true
            });
        }
    }
}
