using GSCLab.Infrastructure;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GSCLab.Views
{
    public partial class InjectionWindow : Window
    {
        private UploadPayload _payload;

        public InjectionWindow(UploadPayload payload)
        {
            InitializeComponent();
            _payload = payload;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            MainContentGrid.Visibility = Visibility.Collapsed;
            FooterBorder.Visibility = Visibility.Collapsed;

            Discord.UpdatePresence("Getting Ready To Inject", "Configuring " + _payload.Title + " By: " + _payload.Author);

            try
            {
                List<SupportedGame> liveGamesList = await FetchSupportedGamesFromServer(_payload.Title);

                if (liveGamesList != null && liveGamesList.Any())
                {
                    GameListBox.ItemsSource = liveGamesList;

                    if (liveGamesList.Count == 1)
                    {
                        GameListBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    MessageBox.Show("No supported games found for this menu on the server.", "GSCLab", MessageBoxButton.OK, MessageBoxImage.Warning);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not reach injection server: {ex.Message}", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                MainContentGrid.Visibility = Visibility.Visible;
                FooterBorder.Visibility = Visibility.Visible;
            }
        }

        private async Task<List<SupportedGame>> FetchSupportedGamesFromServer(string menuName)
        {
            if (string.IsNullOrEmpty(Infrastructure.Session.Token))
            {
                throw new Exception("No active session. Please log in again.");
            }

            using (TcpClient client = new TcpClient("45.90.116.64", 8888))
            using (NetworkStream stream = client.GetStream())
            {
                string cmd = $"GET_SUPPORTED_GAMES|{Infrastructure.Session.Token}|{menuName}<EOF>";
                byte[] cmdData = Encoding.UTF8.GetBytes(cmd);
                await stream.WriteAsync(cmdData, 0, cmdData.Length);

                byte[] header = new byte[4];
                int read = await stream.ReadAsync(header, 0, 4);
                if (read < 4) throw new Exception("Invalid server response: Header too short.");

                string headerText = Encoding.UTF8.GetString(header);

                if (headerText == "ERRO")
                {
                    byte[] errorBuffer = new byte[1024];
                    int errorRead = await stream.ReadAsync(errorBuffer, 0, errorBuffer.Length);
                    string fullError = Encoding.UTF8.GetString(errorBuffer, 0, errorRead);
                    throw new Exception(fullError.Replace("|", ""));
                }

                int responseSize = BitConverter.ToInt32(header, 0);

                if (responseSize <= 0 || responseSize > 5 * 1024 * 1024)
                {
                    throw new Exception($"Server returned invalid protocol data. Raw Header: '{headerText}' (Tried to allocate: {responseSize} bytes)");
                }

                byte[] responseData = new byte[responseSize];
                int totalRead = 0;
                while (totalRead < responseSize)
                {
                    int r = await stream.ReadAsync(responseData, totalRead, responseSize - totalRead);
                    if (r == 0) break;
                    totalRead += r;
                }

                string jsonResponse = Encoding.UTF8.GetString(responseData);
                return JsonSerializer.Deserialize<List<SupportedGame>>(jsonResponse);
            }
        }

        private void GameListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedGame = GameListBox.SelectedItem as SupportedGame;

            if (selectedGame != null)
            {
                ConsoleListBox.ItemsSource = selectedGame.Consoles;
                ModeListBox.ItemsSource = selectedGame.Modes;

                if (selectedGame.Consoles != null && selectedGame.Consoles.Count == 1)
                    ConsoleListBox.SelectedIndex = 0;

                if (selectedGame.Modes != null && selectedGame.Modes.Count == 1)
                    ModeListBox.SelectedIndex = 0;
            }
            else
            {
                ConsoleListBox.ItemsSource = null;
                ModeListBox.ItemsSource = null;
            }

            CheckReadyState(null, null);
        }

        private void CheckReadyState(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (GameListBox.SelectedItem != null &&
                ConsoleListBox.SelectedItem != null &&
                ModeListBox.SelectedItem != null)
            {
                BtnInject.IsEnabled = true;
                BtnInject.Content = "INJECT " + _payload.Title.ToUpper();
            }
            else
            {
                BtnInject.IsEnabled = false;
                BtnInject.Content = "WAITING FOR SELECTION...";
            }
        }

        private async void BtnInject_Click(object sender, RoutedEventArgs e)
        {
            var selectedGame = GameListBox.SelectedItem as SupportedGame;
            string selectedConsole = ConsoleListBox.SelectedItem.ToString();
            string selectedMode = ModeListBox.SelectedItem.ToString();

            MainContentGrid.Visibility = Visibility.Collapsed;
            FooterBorder.Visibility = Visibility.Collapsed;
            LoadingOverlay.Visibility = Visibility.Visible;
            ((TextBlock)((StackPanel)LoadingOverlay.Children[0]).Children[1]).Text = "INJECTING PLEASE WAIT....";

            Discord.UpdatePresence("To" + selectedConsole, "Injecting " + _payload.Title);

            try
            {
                byte[] menuBytes = await FetchMenuBytesFromServer(_payload.Title);

                if (menuBytes == null || menuBytes.Length == 0)
                {
                    GscMessageBox.Show("Failed to download the compiled payload from the server.", "Download Error", MessageBoxButton.OK);
                    this.Close();
                    return;
                }

                ((TextBlock)((StackPanel)LoadingOverlay.Children[0]).Children[1]).Text = "ESTABLISHING CONSOLE CONNECTION...";

                var injectionResult = await GSCLab.Connections.InjectionHandler.ProcessInjection( selectedGame.ShortName, selectedConsole, selectedMode, menuBytes
                );

                if (injectionResult.Success)
                {
                    GscMessageBox.Show($"Successfully Injected " + _payload.Title, "Injection Complete", MessageBoxButton.OK);
                }
                else
                {
                    GscMessageBox.Show($"Injection failed.\n\n{injectionResult.Message}", "Injection Error", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                GscMessageBox.Show($"An error occurred during injection: {ex.Message}", "Fatal Error", MessageBoxButton.OK);
            }

            this.Close();
        }

        private async Task<byte[]> FetchMenuBytesFromServer(string menuName)
        {
            using (TcpClient client = new TcpClient("45.90.116.64", 8888))
            using (NetworkStream stream = client.GetStream())
            {
                string cmd = $"GET_MENU_BYTES|{Infrastructure.Session.Token}|{menuName}<EOF>";
                byte[] cmdData = Encoding.UTF8.GetBytes(cmd);
                await stream.WriteAsync(cmdData, 0, cmdData.Length);

                byte[] header = new byte[4];
                int read = await stream.ReadAsync(header, 0, 4);
                if (read < 4) throw new Exception("Invalid server response.");

                string headerText = Encoding.UTF8.GetString(header);
                if (headerText == "ERRO")
                {
                    byte[] errorBuffer = new byte[1024];
                    int errorRead = await stream.ReadAsync(errorBuffer, 0, errorBuffer.Length);
                    throw new Exception(Encoding.UTF8.GetString(errorBuffer, 0, errorRead).Replace("|", ""));
                }

                int fileSize = BitConverter.ToInt32(header, 0);
                if (fileSize <= 0 || fileSize > 15 * 1024 * 1024)
                    throw new Exception("Payload size is invalid or exceeds maximum allowed size.");

                byte[] fileData = new byte[fileSize];
                int totalRead = 0;
                while (totalRead < fileSize)
                {
                    int r = await stream.ReadAsync(fileData, totalRead, fileSize - totalRead);
                    if (r == 0) break;
                    totalRead += r;
                }

                return fileData;
            }
        }

        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    
}