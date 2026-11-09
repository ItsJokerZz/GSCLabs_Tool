using GSCLab.Infrastructure;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using System;

namespace GSCLab.Views
{
    public partial class AccountSettingsView : Window
    {
        private string vpsIp = "45.90.116.64";
        private int vpsPort = 8888;
        private bool _isHwidRevealed = false;

        public AccountSettingsView()
        {
            InitializeComponent();
            this.Loaded += AccountSettingsView_Loaded;
            Discord.UpdatePresence("Editing " + Session.Username, "In Account Setting's");
        }

        private async void AccountSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCurrentAccountDetailsAsync();
        }

        private async Task LoadCurrentAccountDetailsAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient(vpsIp, vpsPort))
                using (NetworkStream stream = client.GetStream())
                {
                    // Request user stats to get current avatar and status text
                    byte[] data = Encoding.UTF8.GetBytes($"GET_CLIENT_STATS|{Session.Username}");
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[8192];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] parts = response.Split('|');

                    // STATS_SUCCESS|stars|totalInjections|projectCount|level|xp|gameData|avatarUrl|statusText|...
                    if (parts.Length >= 9 && parts[0] == "STATS_SUCCESS")
                    {
                        string avatarFileName = parts[7];
                        string currentStatus = parts[8];

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Populate Custom Status text box
                            if (!string.IsNullOrEmpty(currentStatus) && currentStatus != "None")
                            {
                                InputStatusText.Text = currentStatus;
                            }
                        });

                        // Load Avatar image if it exists
                        if (!string.IsNullOrEmpty(avatarFileName) && avatarFileName != "default.png")
                        {
                            await DownloadAndShowAvatarAsync(avatarFileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Load Settings Error: " + ex.Message);
            }
        }

        private async Task DownloadAndShowAvatarAsync(string fileName)
        {
            try
            {
                string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", "Avatars");
                if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
                string fullPath = Path.Combine(cacheDir, fileName);

                if (File.Exists(fullPath))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var bmp = new BitmapImage(new Uri(fullPath));
                        ImgProfileModal.Source = bmp;
                        TxtProfileEmoji.Visibility = Visibility.Collapsed;
                    });
                    return;
                }

                using (TcpClient client = new TcpClient(vpsIp, vpsPort))
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

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var bmp = new BitmapImage(new Uri(fullPath));
                            ImgProfileModal.Source = bmp;
                            TxtProfileEmoji.Visibility = Visibility.Collapsed;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Avatar Load Error: " + ex.Message);
            }
        }

        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Discord.UpdatePresence("Looking Around", "In Account Setting's");
        }

        private async void BtnUpdateIcon_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                BtnUpdateIcon.Content = "Uploading...";
                BtnUpdateIcon.IsEnabled = false;

                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(openFileDialog.FileName);
                    bmp.DecodePixelWidth = 256;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
                    encoder.Frames.Add(BitmapFrame.Create(bmp));

                    byte[] imageBytes;
                    using (var ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        imageBytes = ms.ToArray();
                    }

                    string base64Image = Convert.ToBase64String(imageBytes);

                    using (TcpClient client = new TcpClient(vpsIp, vpsPort))
                    using (NetworkStream stream = client.GetStream())
                    {
                        string headerPrefix = $"UPDATE_AVATAR|{Session.Token}|";
                        byte[] prefixBytes = Encoding.UTF8.GetBytes(headerPrefix);
                        byte[] imageStringBytes = Encoding.UTF8.GetBytes(base64Image);
                        byte[] eofBytes = Encoding.UTF8.GetBytes("<EOF>");

                        using (var packetStream = new MemoryStream())
                        {
                            packetStream.Write(prefixBytes, 0, prefixBytes.Length);
                            packetStream.Write(imageStringBytes, 0, imageStringBytes.Length);
                            packetStream.Write(eofBytes, 0, eofBytes.Length);

                            byte[] finalPayload = packetStream.ToArray();
                            await stream.WriteAsync(finalPayload, 0, finalPayload.Length);
                        }

                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string response = await reader.ReadLineAsync() ?? "";

                            if (response.StartsWith("AVATAR_SUCCESS"))
                            {
                                ImgProfileModal.Source = bmp;
                                TxtProfileEmoji.Visibility = Visibility.Collapsed;
                                ToastNotification.Show("Profile picture updated successfully!");
                            }
                            else
                            {
                                MessageBox.Show($"Server returned an error: {response}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ToastNotification.Show($"Connection Error: {ex.Message}");
                }
                finally
                {
                    BtnUpdateIcon.Content = "Change Avatar";
                    BtnUpdateIcon.IsEnabled = true;
                }
            }
        }

        private async void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            string newStatus = InputStatusText.Text.Trim();
            if (string.IsNullOrEmpty(newStatus)) return;

            try
            {
                using (TcpClient client = new TcpClient(vpsIp, vpsPort))
                using (NetworkStream stream = client.GetStream())
                {
                    string cmd = $"UPDATE_STATUS|{Session.Username}|{newStatus}";
                    byte[] data = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response == "STATUS_UPDATED")
                    {
                        ToastNotification.Show("Status updated successfully!");
                    }
                    else
                    {
                        ToastNotification.Show("Failed to update status on the server.");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Connection Error: {ex.Message}");
            }
        }

        private async void BtnRevealHwid_Click(object sender, RoutedEventArgs e)
        {
            if (!_isHwidRevealed)
            {
                BtnRevealHwid.Content = "LOADING...";
                BtnRevealHwid.IsEnabled = false;

                try
                {
                    using (TcpClient client = new TcpClient(vpsIp, vpsPort))
                    using (NetworkStream stream = client.GetStream())
                    {
                        string cmd = $"GET_HWID|{Session.Token}";
                        byte[] data = Encoding.UTF8.GetBytes(cmd);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (response.StartsWith("HWID_SUCCESS"))
                        {
                            string rawHash = response.Split('|')[1];

                            string displayHwid = rawHash;
                            if (rawHash.Length >= 16)
                            {
                                string shortHash = rawHash.Substring(0, 16).ToUpper();
                                displayHwid = $"{shortHash.Substring(0, 4)}-{shortHash.Substring(4, 4)}-{shortHash.Substring(8, 4)}-{shortHash.Substring(12, 4)}";
                            }

                            TxtModalHwidDisplay.Text = displayHwid;
                            BtnRevealHwid.Content = "HIDE";
                            _isHwidRevealed = true;
                        }
                        else
                        {
                            MessageBox.Show("Could not fetch HWID from server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Connection Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (!_isHwidRevealed) BtnRevealHwid.Content = "REVEAL";
                    BtnRevealHwid.IsEnabled = true;
                }
            }
            else
            {
                TxtModalHwidDisplay.Text = "••••-••••-••••-••••";
                BtnRevealHwid.Content = "REVEAL";
                _isHwidRevealed = false;
            }
        }

        private async void UpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            string newPass = PassNewPassword.Password.Trim();

            if (string.IsNullOrEmpty(newPass) || newPass.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Invalid Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnUpdatePassword.Content = "Saving...";
            BtnUpdatePassword.IsEnabled = false;

            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    string cmd = $"UPDATE_PASSWORD|{Session.Username}|{newPass}";
                    byte[] data = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response == "PASSWORD_SUCCESS")
                    {
                        MessageBox.Show("Your password has been successfully changed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        PassNewPassword.Password = "";
                    }
                    else
                    {
                        MessageBox.Show($"Server error: {response}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection Error: {ex.Message}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnUpdatePassword.Content = "Save Security Changes";
                BtnUpdatePassword.IsEnabled = true;
            }
        }

        private async void BtnUpdateGithub_Click(object sender, RoutedEventArgs e)
        {
            string githubUser = InputGithubUsername.Text.Trim();
            string githubPat = InputGithubToken.Password.Trim();

            if (string.IsNullOrEmpty(githubUser) || string.IsNullOrEmpty(githubPat))
            {
                ToastNotification.Show("Please enter username and token.");
                return;
            }

            BtnUpdateGithub.Content = "Saving...";
            BtnUpdateGithub.IsEnabled = false;

            try
            {
                using (TcpClient client = new TcpClient(vpsIp, vpsPort))
                using (NetworkStream stream = client.GetStream())
                {
                    string cmd = $"UPDATE_GITHUB|{Session.Username}|{githubUser}|{githubPat}";
                    byte[] data = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[8192];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response == "GITHUB_SUCCESS")
                    {
                        ToastNotification.Show("Info linked to your account!");
                        InputGithubToken.Password = "";
                    }
                    else
                    {
                        ToastNotification.Show($"Server returned an error: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Connection Error: {ex.Message}");
            }
            finally
            {
                BtnUpdateGithub.Content = "Save GitHub Credentials";
                BtnUpdateGithub.IsEnabled = true;
            }
        }

        private async void BtnUpdateDiscord_Click(object sender, RoutedEventArgs e)
        {
            string discordId = InputDiscordId.Text.Trim();

            if (string.IsNullOrEmpty(discordId))
            {
                ToastNotification.Show("Please enter your Discord ID.");
                return;
            }

            BtnUpdateDiscord.Content = "Saving...";
            BtnUpdateDiscord.IsEnabled = false;

            try
            {
                using (TcpClient client = new TcpClient(vpsIp, vpsPort))
                using (NetworkStream stream = client.GetStream())
                {
                    string cmd = $"UPDATE_DISCORD|{Session.Username}|{discordId}";
                    byte[] data = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response == "DISCORD_SUCCESS")
                    {
                        ToastNotification.Show("Discord ID linked successfully!");
                        InputDiscordId.Text = "";
                    }
                    else
                    {
                        ToastNotification.Show($"Server returned an error: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Connection Error: {ex.Message}");
            }
            finally
            {
                BtnUpdateDiscord.Content = "Save Discord ID";
                BtnUpdateDiscord.IsEnabled = true;
            }
        }
    }
}