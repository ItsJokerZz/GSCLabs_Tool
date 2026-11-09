using GSCLab.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GSCLab.Views
{
    public partial class AccountView : UserControl
    {
        private bool _isLoadingProjects = false;
        private static readonly Dictionary<string, (string FriendlyName, string AssetName)> _gameLookup = new()
        {
            { "IW6", ("Ghosts", "ghosts.jpg") }, { "S1XB", ("Advanced Warfare", "aw.jpg") },
            { "H1", ("Modern Warfare R", "mwr.jpg") }, { "H2PS4", ("MW2 Campaign Remastered", "mw2cr.jpg") },
            { "S2PS4", ("World War 2", "ww2.jpg") }, { "BO3_PS4", ("Black Ops III", "BO3.jpg") },
            { "IW7PS4", ("Infinite Warfare", "iw.jpg") }, { "IW5PS", ("Modern Warfare 3", "mw3.jpg") },
            { "IW8", ("MW2019", "mw2019.jpg") }, { "IW9", ("MW2 2022", "mw22.jpg") },
            { "S4", ("Vanguard", "vanguard.jpg") }
        };

        public AccountView()
        {
            InitializeComponent();
            this.Loaded += AccountView_Loaded;
            Discord.UpdatePresence("Looking Around", "In Account Setting's");
        }

        private async void AccountView_Loaded(object sender, RoutedEventArgs e)
        {
            TxtUsername.Text = Session.Username;
            RoleLabel.Text = Session.Role;
            await FetchLiveStatsAsync();
            await LoadMarketplaceProjects();
            await LoadRecentActivityAsync();
        }

        private void OpenSettingsWindow_Click(object sender, RoutedEventArgs e)
        {
            AccountSettingsView settingsWin = new AccountSettingsView();
            settingsWin.ShowDialog();
            _ = FetchLiveStatsAsync();
        }

        private async Task FetchLiveStatsAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("45.90.116.64", 8888);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask) return;
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] data = Encoding.UTF8.GetBytes($"GET_CLIENT_STATS|{Session.Username}");
                        await stream.WriteAsync(data, 0, data.Length);
                        string response = await ReadFullResponseAsync(stream);
                        string[] parts = response.Split('|');

                        if (parts.Length >= 11)
                        {
                            Application.Current.Dispatcher.Invoke(() => {
                                TxtTotalInjections.Text = parts[2];
                                TxtLibraryCount.Text = parts[3];

                                if (!string.IsNullOrEmpty(parts[8]))
                                {
                                    TxtUserStatus.Text = parts[8];
                                }

                                UpdateLinkStatus(TxtGithubStatus, parts[9]);
                                UpdateLinkStatus(TxtDiscordStatus, parts[10]);
                                if (!string.IsNullOrEmpty(parts[6]) && parts[6] != "NONE:0")
                                {
                                    Debug.WriteLine($"RAW_INJECTIONS: {parts[6]}");
                                    GameInjectionsList.Children.Clear();
                                    foreach (var entry in parts[6].Split(';'))
                                    {
                                        try
                                        {
                                            var split = entry.Split(':');
                                            if (split.Length == 2)
                                            {
                                                string rawKey = split[0].ToUpper();
                                                string assetName = _gameLookup.ContainsKey(rawKey) ? _gameLookup[rawKey].AssetName : "default_game.jpg";
                                                string friendlyName = _gameLookup.ContainsKey(rawKey) ? _gameLookup[rawKey].FriendlyName : rawKey;
                                                AddGameStatCard(friendlyName, int.Parse(split[1]), assetName);
                                            }
                                        }
                                        catch (Exception ex) { Debug.WriteLine($"Error building card: {ex.Message}"); }
                                    }
                                }
                                if (!string.IsNullOrEmpty(parts[7]) && parts[7] != "default.png") _ = DownloadAvatarFromServer(parts[7]);
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Stats Error: {ex.Message}"); }
        }

        private void UpdateLinkStatus(TextBlock txt, string val)
        {
            txt.Text = val == "1" ? " Linked" : " Not Linked";
            txt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(val == "1" ? "#28A745" : "#FF4444"));
        }

        private void AddGameStatCard(string name, int count, string asset)
        {
            StackPanel card = new StackPanel { Margin = new Thickness(0, 0, 20, 10), Width = 100 };

            Border imageBorder = new Border
            {
                Width = 100,
                Height = 140,
                CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent,
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            Image img = new Image();
            img.Stretch = Stretch.Fill;

            try
            {
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/{asset}", UriKind.Absolute));
            }
            catch
            {
                img.Source = new BitmapImage(new Uri($"pack://application:,,,/Assets/default_game.jpg", UriKind.Absolute));
            }

            imageBorder.Child = img;

            card.Children.Add(imageBorder);
            card.Children.Add(new TextBlock { Text = name, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            card.Children.Add(new TextBlock { Text = $"{count} Injections", FontSize = 9, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")), HorizontalAlignment = HorizontalAlignment.Center });

            GameInjectionsList.Children.Add(card);
        }

        private async Task DownloadAvatarFromServer(string fileName)
        {
            try
            {
                string cacheDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "Avatars");
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
                string fullPath = System.IO.Path.Combine(cacheDir, fileName);

                if (File.Exists(fullPath)) { ImgProfileMain.ImageSource = new BitmapImage(new Uri(fullPath)); return; }

                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes($"DOWNLOAD_AVATAR|{fileName}");
                    await stream.WriteAsync(data, 0, data.Length);
                    byte[] buffer = new byte[1024 * 1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response.StartsWith("AVATAR_DATA|"))
                    {
                        byte[] imageBytes = Convert.FromBase64String(response.Split('|')[1]);
                        File.WriteAllBytes(fullPath, imageBytes);
                        Application.Current.Dispatcher.Invoke(() => ImgProfileMain.ImageSource = new BitmapImage(new Uri(fullPath)));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Download error: " + ex.Message); }
        }

        private async Task LoadRecentActivityAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes($"GET_RECENT_ACTIVITY|{Session.Username}");
                    await stream.WriteAsync(data, 0, data.Length);
                    string response = await ReadFullResponseAsync(stream);
                    string[] parts = response.Split('|');

                    if (parts.Length > 1 && parts[0] == "ACTIVITY_SUCCESS")
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            ActivityLogList.Children.Clear();

                            for (int i = 1; i < parts.Length; i++)
                            {
                                if (string.IsNullOrWhiteSpace(parts[i])) continue;

                                DockPanel rowPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 15) };

                                Border iconBorder = new Border
                                {
                                    Width = 30,
                                    Height = 30,
                                    CornerRadius = new CornerRadius(15),
                                    Background = (Brush)new BrushConverter().ConvertFrom("#050505"),
                                    BorderBrush = (Brush)new BrushConverter().ConvertFrom("#222"),
                                    BorderThickness = new Thickness(1),
                                    Margin = new Thickness(0, 0, 12, 0)
                                };

                                TextBlock emojiTxt = new TextBlock
                                {
                                    Text = "⚡",
                                    FontSize = 12,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                };
                                iconBorder.Child = emojiTxt;

                                StackPanel textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

                                TextBlock actionTxt = new TextBlock
                                {
                                    Text = parts[i],
                                    FontSize = 12,
                                    Foreground = (Brush)new BrushConverter().ConvertFrom("#DDD"),
                                    Margin = new Thickness(0, 0, 0, 2)
                                };

                                TextBlock timeTxt = new TextBlock
                                {
                                    Text = "Recent",
                                    Foreground = (Brush)new BrushConverter().ConvertFrom("#555"),
                                    FontSize = 10
                                };

                                textStack.Children.Add(actionTxt);
                                textStack.Children.Add(timeTxt);

                                rowPanel.Children.Add(iconBorder);
                                rowPanel.Children.Add(textStack);

                                ActivityLogList.Children.Add(rowPanel);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Activity Load Error: " + ex.Message);
            }
        }

        private async Task LoadMarketplaceProjects()
        {
            if (_isLoadingProjects) return;
            _isLoadingProjects = true;
            try
            {
                MyProjectsList.Children.Clear();
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes($"GET_MY_PROJECTS|{Session.Username}");
                    await stream.WriteAsync(data, 0, data.Length);
                    string response = await ReadFullResponseAsync(stream);
                    var projects = JsonSerializer.Deserialize<List<UploadPayload>>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (projects != null)
                    {
                        foreach (var proj in projects)
                        {
                            string gameDisplay = "Unknown";

                            if (proj.SupportedGames != null && proj.SupportedGames.Count > 0)
                            {
                                gameDisplay = string.Join(", ", proj.SupportedGames.Select(g => g.GameName));
                            }
                            else
                            {
                                string title = proj.Title.ToUpper();
                                if (title.Contains("COD2")) gameDisplay = "COD2";
                                else if (title.Contains("COD4")) gameDisplay = "COD4";
                                else if (title.Contains("QOS")) gameDisplay = "QOS";
                                else if (title.Contains("WAW")) gameDisplay = "WAW";
                                else if (title.Contains("MW2")) gameDisplay = "MW2";
                                else if (title.Contains("BO1")) gameDisplay = "BO1";
                                else if (title.Contains("MW3")) gameDisplay = "MW3"; // Supported
                                else if (title.Contains("BO2")) gameDisplay = "BO2";
                                else if (title.Contains("GHOSTS")) gameDisplay = "GHOSTS"; // Supported
                                else if (title.Contains("AW")) gameDisplay = "AW"; // Supported
                                else if (title.Contains("BO3")) gameDisplay = "BO3";
                                else if (title.Contains("IW")) gameDisplay = "IW"; // Supported
                                else if (title.Contains("MWR")) gameDisplay = "MWR"; // Supported
                                else if (title.Contains("WW2")) gameDisplay = "WW2"; // Supported
                                else if (title.Contains("BO4")) gameDisplay = "BO4";
                                else if (title.Contains("MW2019")) gameDisplay = "MW2019"; // Supported
                                else if (title.Contains("BOCW")) gameDisplay = "BOCW";
                                else if (title.Contains("VG")) gameDisplay = "VG"; // Supported
                                else if (title.Contains("MWII")) gameDisplay = "MWII"; // Supported
                                else if (title.Contains("MWIII")) gameDisplay = "MWIII"; // WIP
                                else if (title.Contains("BO6")) gameDisplay = "BO6";
                                else if (title.Contains("BO7")) gameDisplay = "BO7";
                            }

                            AddProjectCard(proj.Title, gameDisplay, "0", "0", "N/A", false);
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Project Load Error: " + ex.Message); }
            finally { _isLoadingProjects = false; }
        }

        private void AddProjectCard(string name, string game, string users, string sold, string days, bool updated)
        {
            Border card = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212")), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Width = 220, Height = 125, Margin = new Thickness(0, 0, 15, 10), Padding = new Thickness(15) };
            StackPanel infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.Bold, FontSize = 14, Foreground = Brushes.White });
            infoStack.Children.Add(new TextBlock { Text = game, Foreground = Brushes.Gray, FontSize = 10, Margin = new Thickness(0, 2, 0, 10) });

            Button editBtn = new Button { Content = "✎ UPDATE MOD MENU", Style = (Style)FindResource("EditButtonStyle"), HorizontalAlignment = HorizontalAlignment.Stretch, Height = 20 };
            editBtn.Click += (s, e) => { new UpdateModMenu(name, game).ShowDialog(); };

            infoStack.Children.Add(editBtn);
            card.Child = infoStack;
            MyProjectsList.Children.Add(card);
        }

        private async Task<string> ReadFullResponseAsync(NetworkStream stream)
        {
            var sb = new StringBuilder();
            byte[] buffer = new byte[4096];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (sb.ToString().Contains("<EOF>")) return sb.ToString().Replace("<EOF>", "");
                if (!stream.DataAvailable) break;
            }
            return sb.ToString();
        }

        private void SetStarRating(double r) { }
    }
}