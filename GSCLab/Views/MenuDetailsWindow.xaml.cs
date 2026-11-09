using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GSCLab.Infrastructure;
using Microsoft.Web.WebView2.Wpf;

namespace GSCLab.Views
{
    public partial class MenuDetailsWindow : Window
    {
        private UploadPayload _menuData;

        public MenuDetailsWindow(UploadPayload menuData)
        {
            InitializeComponent();
            _menuData = menuData;

            VideoWebView.EnsureCoreWebView2Async();

            Discord.UpdatePresence("By: " + menuData.Author, "Viewing " + menuData.Title);

            TxtTitle.Text = menuData.Title;
            TxtAuthor.Text = menuData.Author;
            TxtVersion.Text = $"v{menuData.Version}";
            TxtDescription.Text = menuData.Description;
            TxtChangelog.Text = string.IsNullOrWhiteSpace(menuData.Changelog) ? "No changelog provided." : menuData.Changelog;

            bool isFree = string.IsNullOrEmpty(menuData.PriceType) || menuData.PriceType.Contains("Free");

            if (isFree)
            {
                TxtPrice.Text = "Free";
                TxtPrice.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));
                BtnAction.Content = "⚡ Inject Menu";
                BtnAction.Style = (Style)FindResource("PrimaryActionBtn");
            }
            else
            {
                string displayPrice = string.IsNullOrWhiteSpace(menuData.Price) ? "0.00" : menuData.Price;

                TxtPrice.Text = $"${displayPrice}";
                TxtPrice.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745"));
                BtnAction.Content = "🛒 Purchase";
                BtnAction.Style = (Style)FindResource("PrimaryActionBtn");
            }

            if (string.IsNullOrWhiteSpace(menuData.YoutubeLink))
            {
                BtnYoutube.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(menuData.ImageFileBase64))
            {
                try
                {
                    byte[] imgBytes = Convert.FromBase64String(menuData.ImageFileBase64);
                    using (var ms = new MemoryStream(imgBytes))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        BigCoverImage.Source = bitmap;
                    }
                }
                catch { }
            }

            if (menuData.SupportedGames != null)
            {
                foreach (var game in menuData.SupportedGames)
                {
                    foreach (var console in game.Consoles)
                    {
                        AddPlatformLogo(console);
                    }
                }
            }

            LoadInjectionConfigData();
        }

        private async void LoadInjectionConfigData()
        {
            Discord.UpdatePresence("Getting Ready To Inject", "Configuring " + _menuData.Title + " By: " + _menuData.Author);

            try
            {
                List<SupportedGame> liveGamesList = await FetchSupportedGamesFromServer(_menuData.Title);

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not reach injection server: {ex.Message}", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    throw new Exception($"Server returned invalid protocol data. Raw Header: '{headerText}'");
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

        private void GameListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void CheckReadyState(object sender, SelectionChangedEventArgs e)
        {
            if (GameListBox != null && GameListBox.SelectedItem != null &&
                ConsoleListBox != null && ConsoleListBox.SelectedItem != null &&
                ModeListBox != null && ModeListBox.SelectedItem != null)
            {
                BtnAction.IsEnabled = true;
                BtnAction.Content = "⚡ Inject " + _menuData.Title;
            }
            else
            {
                BtnAction.IsEnabled = true;
            }
        }

        private void AddPlatformLogo(string platformName)
        {
            string imgName = "pc.png";
            if (platformName.ToLower().Contains("ps3")) imgName = "ps3.png";
            else if (platformName.ToLower().Contains("ps4") || platformName.ToLower().Contains("ps5")) imgName = "ps4.png";
            else if (platformName.ToLower().Contains("xbox")) imgName = "xbox.png";

            Border logoContainer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 5, 15, 5),
                Margin = new Thickness(0, 0, 10, 10),
                ToolTip = platformName
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };

            try
            {
                Image icon = new Image
                {
                    Width = 20,
                    Height = 20,
                    Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/{imgName}"))
                };
                RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
                sp.Children.Add(icon);
            }
            catch { }

            sp.Children.Add(new TextBlock
            {
                Text = platformName,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            });

            logoContainer.Child = sp;
            PlatformLogosPanel.Children.Add(logoContainer);
        }

        private async void BtnYoutube_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_menuData.YoutubeLink)) return;

            BigCoverImage.Visibility = Visibility.Collapsed;
            VideoWebView.Visibility = Visibility.Visible;
            BtnBackToImage.Visibility = Visibility.Visible;

            BtnYoutube.IsEnabled = false;
            ((TextBlock)((StackPanel)BtnYoutube.Content).Children[1]).Text = "Loading...";

            string videoId = "";
            var match = Regex.Match(_menuData.YoutubeLink,
                @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/)([^""&?\/\s]{11})");

            if (match.Success)
                videoId = match.Groups[1].Value;

            if (!string.IsNullOrEmpty(videoId))
            {
                try
                {
                    await VideoWebView.EnsureCoreWebView2Async(null);

                    VideoWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "gsclab.local",
                        Path.GetTempPath(),
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
                    );

                    string html = $@"<!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body, html {{ margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; background: #0A0A0A; }}
                    iframe {{ width: 100%; height: 100%; border: none; }}
                </style>
            </head>
            <body>
                <iframe src='https://www.youtube.com/embed/{videoId}?autoplay=1&mute=1' 
                        allow='accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture' 
                        allowfullscreen>
                </iframe>
            </body>
            </html>";

                    string tempFile = Path.Combine(Path.GetTempPath(), $"gsclab_{videoId}.html");
                    File.WriteAllText(tempFile, html);

                    VideoWebView.CoreWebView2.Navigate($"https://gsclab.local/gsclab_{videoId}.html");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not load video player: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _menuData.YoutubeLink, UseShellExecute = true });
                    BtnBackToImage_Click(null, null);
                }
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _menuData.YoutubeLink, UseShellExecute = true });
                BtnBackToImage_Click(null, null);
            }

            BtnYoutube.IsEnabled = true;
            ((TextBlock)((StackPanel)BtnYoutube.Content).Children[1]).Text = "Playing Trailer";
        }

        private void BtnBackToImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (VideoWebView != null && VideoWebView.CoreWebView2 != null)
                {
                    VideoWebView.CoreWebView2.Navigate("about:blank");
                }
            }
            catch { }

            VideoWebView.Visibility = Visibility.Collapsed;
            BigCoverImage.Visibility = Visibility.Visible;
            BtnBackToImage.Visibility = Visibility.Collapsed;

            BtnYoutube.IsEnabled = true;
            if (BtnYoutube.Content is StackPanel sp)
            {
                ((TextBlock)sp.Children[1]).Text = "Trailer";
            }
        }

        private async void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            bool isFree = string.IsNullOrEmpty(_menuData.PriceType) || _menuData.PriceType.Contains("Free");

            if (!isFree)
            {
                MessageBox.Show($"Opening Purchase Gateway...", "GSCLab", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedGame = GameListBox.SelectedItem as SupportedGame;
            string selectedConsole = ConsoleListBox.SelectedItem?.ToString();
            string selectedMode = ModeListBox.SelectedItem?.ToString();

            if (selectedGame == null || string.IsNullOrEmpty(selectedConsole) || string.IsNullOrEmpty(selectedMode))
            {
                MessageBox.Show("Please select a target Game, Console, and Mode in the 'Injection Config' tab before injecting.", "Incomplete Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Discord.UpdatePresence("To " + selectedConsole, "Injecting " + _menuData.Title);

            try
            {
                // Pass the selected game's short name here
                byte[] menuBytes = await FetchMenuBytesFromServer(_menuData.Title, selectedGame.ShortName);

                if (menuBytes == null || menuBytes.Length == 0)
                {
                    GscMessageBox.Show("Failed to download the compiled payload from the server.", "Download Error", MessageBoxButton.OK);
                    return;
                }

                var injectionResult = await GSCLab.Connections.InjectionHandler.ProcessInjection(selectedGame.ShortName, selectedConsole, selectedMode, menuBytes);

                if (injectionResult.Success)
                {
                    GscMessageBox.Show($"Successfully Injected " + _menuData.Title, "Injection Complete", MessageBoxButton.OK);
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
        }

        private async Task<byte[]> FetchMenuBytesFromServer(string menuName, string gameShortName)
        {
            using (TcpClient client = new TcpClient("45.90.116.64", 8888))
            using (NetworkStream stream = client.GetStream())
            {
                string cmd = $"GET_MENU_BYTES|{Infrastructure.Session.Token}|{menuName}|{gameShortName}<EOF>";
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
            if (VideoWebView != null && VideoWebView.CoreWebView2 != null)
            {
                VideoWebView.Dispose();
            }
            this.Close();
        }
    }

    public class GameImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string shortName = (value as string)?.ToLower() ?? "";
            string img = "default.jpg";

            if (shortName.Contains("ghosts") || shortName == "iw6") img = "ghosts.jpg";
            else if (shortName.Contains("aw") || shortName == "s1") img = "aw.jpg";
            else if (shortName.Contains("mwr") || shortName == "h1") img = "mwr.jpg";
            else if (shortName.Contains("ww2") || shortName == "s2") img = "ww2.jpg";
            else if (shortName.Contains("mw2cr") || shortName == "h2") img = "mw2cr.jpg";
            else if (shortName.Contains("bo3")) img = "BO3.jpg";
            else if (shortName.Contains("mw1") || shortName.Contains("cod4")) img = "cod4.jpg";

            return $"pack://application:,,,/Assets/{img}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }

    public class ConsoleImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string console = (value as string)?.ToLower() ?? "";
            string img = "pc.png";

            if (console.Contains("ps4") || console.Contains("ps5")) img = "ps4_logo.png";
            else if (console.Contains("xbox") || console.Contains("xb")) img = "xbox360.png";
            else if (console.Contains("ps3")) img = "ps3.png";

            return $"pack://application:,,,/Assets/{img}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }

    public class ModeImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string mode = (value as string)?.ToLower() ?? "";
            string img = "singleplayer.png";

            if (mode == "mp" || mode == "multiplayer") img = "multiplayer.png";
            else if (mode == "zm" || mode == "zombies") img = "zombies.png";
            else if (mode == "sp" || mode == "campaign" || mode == "ex" || mode == "extinction") img = "singleplayer.png";

            return $"pack://application:,,,/Assets/{img}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
}