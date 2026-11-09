using GSCLab.Infrastructure;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GSCLab.Views
{
    public partial class PublishMenuWindow : Window
    {
        private string _selectedImagePath = "";
        private string _selectedFolderPath = "";
        public PublishMenuWindow()
        {
            InitializeComponent();
            Discord.UpdatePresence("Publishing A Menu", "In Library");
            AuthorInput.Text = Session.Username;
        }
        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void SelectImgBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select Cover Image";
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                long length = new FileInfo(openFileDialog.FileName).Length;
                if (length > 5 * 1024 * 1024)
                {
                    GscMessageBox.Show("Image file is too large! Please select an image under 5MB.", "File Too Large", MessageBoxButton.OK);
                    return;
                }

                _selectedImagePath = openFileDialog.FileName;
                SelectedImageNameText.Text = Path.GetFileName(_selectedImagePath);
                SelectedImageNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
            }
        }
        private void SelectFolderBtn_Click(object sender, MouseButtonEventArgs e)
        {
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Select your GSC Project Folder";
                folderDialog.UseDescriptionForTitle = true;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedFolderPath = folderDialog.SelectedPath;
                    SelectedFolderNameText.Text = new DirectoryInfo(_selectedFolderPath).Name;
                    SelectedFolderNameText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
                }
            }
        }
        private async void SubmitMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MenuNameInput.Text) || string.IsNullOrWhiteSpace(AuthorInput.Text))
            {
                GscMessageBox.Show("Please fill out the Menu Name and Author fields.", "Validation Error", MessageBoxButton.OK);
                return;
            }

            if (string.IsNullOrEmpty(_selectedImagePath) || string.IsNullOrEmpty(_selectedFolderPath))
            {
                GscMessageBox.Show("You must select both a Cover Image and a Project Folder.", "Missing Files", MessageBoxButton.OK);
            }

            SubmitMenuBtn.IsEnabled = false;
            SubmitMenuBtn.Content = "CHECKING FILES...";

            try
            {
                string[] gscFiles = Directory.GetFiles(_selectedFolderPath, "*.gsc", SearchOption.AllDirectories);
                if (gscFiles.Length == 0)
                {
                    GscMessageBox.Show("No .gsc files were found in the selected folder!", "Compile Error", MessageBoxButton.OK);
                    return;
                }

                StringBuilder masterBuilder = new StringBuilder();
                foreach (string file in gscFiles)
                {
                    masterBuilder.AppendLine($"// File: {Path.GetFileName(file)}");
                    masterBuilder.AppendLine(File.ReadAllText(file));
                    masterBuilder.AppendLine();
                }
                string combinedCode = masterBuilder.ToString();

                string targetCompilerGame = "iw6";
                if (ChkAW.IsChecked == true) targetCompilerGame = "s1ps4";
                else if (ChkMWR.IsChecked == true) targetCompilerGame = "h1ps4";
                else if (ChkMW2CR.IsChecked == true) targetCompilerGame = "h2ps4";
                else if (ChkWW2.IsChecked == true) targetCompilerGame = "s2ps4";
                else if (ChkGhosts.IsChecked == true) targetCompilerGame = "iw6ps4";
                else if (ChkIW.IsChecked == true) targetCompilerGame = "iw7ps4";
                else if (ChkMW3.IsChecked == true) targetCompilerGame = "iw5ps";
                else if (ChkMW2019.IsChecked == true) targetCompilerGame = "iw8";
                else if (ChkMW22.IsChecked == true) targetCompilerGame = "iw9";
                else if (ChkVG.IsChecked == true) targetCompilerGame = "s4";


                byte[] compiledBytes = await RequestCompile(targetCompilerGame, "main.gsc", combinedCode);

                if (compiledBytes == null || compiledBytes.Length == 0)
                {
                    return;
                }

                SubmitMenuBtn.Content = "UPLOADING TO MARKETPLACE...";

                UploadPayload payload = new UploadPayload
                {
                    Title = MenuNameInput.Text.Trim(),
                    Author = AuthorInput.Text.Trim(),
                    Version = VersionInput.Text.Trim(),
                    PriceType = (PricingTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString().Replace(" Menu", ""),
                    Price = PriceInput.Text.Trim(),
                    CouponCode = CouponCodeInput.Text.Trim(),
                    Description = MenuDescInput.Text.Trim(),
                    Changelog = ChangelogInput.Text.Trim(),
                    YoutubeLink = YoutubeLinkInput.Text.Trim(),

                    ImageFileName = Path.GetFileName(_selectedImagePath),
                    ImageFileBase64 = Convert.ToBase64String(File.ReadAllBytes(_selectedImagePath)),

                    GscFileName = $"{MenuNameInput.Text.Replace(" ", "_")}.gscbin",
                    GscFileBase64 = Convert.ToBase64String(compiledBytes),

                    SupportedGames = GatherSupportedGames()
                };

                string result = await UploadToServer(payload);

                if (result == "SUCCESS")
                {
                    GscMessageBox.Show("Menu successfully published to GSCLab Marketplace!", "Success", MessageBoxButton.OK);
                    this.Close();
                }
                else
                {
                    GscMessageBox.Show($"Server returned an error: {result}", "Upload Failed", MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                GscMessageBox.Show($"Operation crashed: {ex.Message}", "Fatal Error", MessageBoxButton.OK);
            }
            finally
            {
                SubmitMenuBtn.IsEnabled = true;
                SubmitMenuBtn.Content = "🚀 PUBLISH TO MARKETPLACE";
            }
        }
        private async Task<byte[]> RequestCompile(string game, string scriptName, string code)
        {
            if (string.IsNullOrEmpty(Infrastructure.Session.Token))
                throw new Exception("No active session. Please log in again.");

            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(code));
                    string cmd = $"COMPILE|{Infrastructure.Session.Token}|{game}|{scriptName}|{b64}<EOF>";

                    byte[] cmdData = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(cmdData, 0, cmdData.Length);

                    byte[] header = new byte[4];
                    int read = await stream.ReadAsync(header, 0, 4);
                    if (read < 4) return null;

                    string headerText = Encoding.UTF8.GetString(header);

                    if (headerText == "ERRO")
                    {
                        byte[] errorBuffer = new byte[1024];
                        int errorRead = await stream.ReadAsync(errorBuffer, 0, errorBuffer.Length);
                        string fullError = Encoding.UTF8.GetString(errorBuffer, 0, errorRead);
                        MessageBox.Show($"Compilation Error:\n\n{fullError.Replace("|", "")}", "Compile Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }

                    int fileSize = BitConverter.ToInt32(header, 0);
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
            catch (Exception ex)
            {
                MessageBox.Show($"Network Error during compilation: {ex.Message}", "Compile Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        private async Task<string> UploadToServer(UploadPayload payload)
        {
            string jsonPayload = JsonSerializer.Serialize(payload);

            using (TcpClient client = new TcpClient("45.90.116.64", 8888))
            using (NetworkStream stream = client.GetStream())
            {
                string cmd = $"UPLOAD|{jsonPayload}<EOF>";
                byte[] data = Encoding.UTF8.GetBytes(cmd);
                await stream.WriteAsync(data, 0, data.Length);

                byte[] responseBuffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                return Encoding.UTF8.GetString(responseBuffer, 0, bytesRead).Trim();
            }
        }
        private List<MenuSupportConfig> GatherSupportedGames()
        {
            List<MenuSupportConfig> supportedGames = new List<MenuSupportConfig>();
            AddIfConfigured(supportedGames, "Call of Duty: Ghosts", "GHOSTS", ChkGhosts);
            AddIfConfigured(supportedGames, "Advanced Warfare", "AW", ChkAW);
            AddIfConfigured(supportedGames, "Modern Warfare Remastered", "MWR", ChkMWR);
            AddIfConfigured(supportedGames, "Modern Warfare 2 Campaign Remastered", "MW2CR", ChkMW2CR);
            AddIfConfigured(supportedGames, "World War II", "WW2", ChkWW2);
            AddIfConfigured(supportedGames, "Modern Warfare 3", "MW3", ChkMW3);
            AddIfConfigured(supportedGames, "Modern Warfare 2019", "MW2019", ChkMW2019);
            AddIfConfigured(supportedGames, "Vanguard", "VG", ChkVG);
            AddIfConfigured(supportedGames, "Modern Warfare 2 2022", "MW22", ChkMW22);
            AddIfConfigured(supportedGames, "Infinite Warfare", "IW", ChkIW);

            return supportedGames;
        }
        private void AddIfConfigured(List<MenuSupportConfig> list, string fullGameName, string shortName, ToggleButton parentToggle)
        {
            MenuSupportConfig config = ExtractGameConfig(fullGameName, shortName, parentToggle);

            if (config.Modes.Count > 0 || config.Consoles.Count > 0)
            {
                list.Add(config);
            }
        }
        private MenuSupportConfig ExtractGameConfig(string fullGameName, string shortName, ToggleButton parentToggle)
        {
            MenuSupportConfig config = new MenuSupportConfig
            {
                GameName = fullGameName,
                ShortName = shortName,
                Modes = new List<string>(),
                Consoles = new List<string>()
            };

            StackPanel parentPanel = (StackPanel)parentToggle.Parent;
            Border configBorder = (Border)parentPanel.Children[1];
            Grid configGrid = (Grid)configBorder.Child;

            StackPanel modesPanel = (StackPanel)configGrid.Children[0];
            WrapPanel modesWrap = (WrapPanel)modesPanel.Children[1];
            foreach (var child in modesWrap.Children)
            {
                if (child is CheckBox chk && chk.IsChecked == true)
                    config.Modes.Add(chk.Content.ToString());
            }

            StackPanel consolesPanel = (StackPanel)configGrid.Children[1];
            WrapPanel consolesWrap = (WrapPanel)consolesPanel.Children[1];
            foreach (var child in consolesWrap.Children)
            {
                if (child is CheckBox chk && chk.IsChecked == true)
                    config.Consoles.Add(chk.Content.ToString());
            }

            return config;
        }
    }
}
