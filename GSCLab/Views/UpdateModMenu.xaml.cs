using GSCLab.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Text.Json;

namespace GSCLab.Views
{
    public partial class UpdateModMenu : Window
    {
        private string _selectedFolderPath;
        private string _targetGame;

        private static readonly Dictionary<string, string> GameCompilerMap = new()
        {
            { "AW", "s1ps4" }, { "BO2", "bo2_ps3" }, { "GHOSTS", "iw6ps4" },
            { "IW", "iw7ps4" }, { "MW2CR", "h2ps4" }, { "MW3", "iw5ps" },
            { "MWR", "h1ps4" }, { "WW2", "s2ps4" }, { "WWII", "s2ps4" },
            { "Mw22", "iw9" }
        };

        public UpdateModMenu(string menuName, string gameName)
        {
            InitializeComponent();
            TxtMenuName.Text = menuName;
            TxtGameName.Text = gameName;
            _targetGame = gameName;
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _selectedFolderPath = dialog.SelectedPath;
                    TxtFolderPath.Text = _selectedFolderPath;
                }
            }
        }

        private async Task SendUpdateToServer(UploadPayload payload)
        {
            using (TcpClient client = new TcpClient("45.90.116.64", 8888))
            using (NetworkStream stream = client.GetStream())
            {
                string json = JsonSerializer.Serialize(payload);
                byte[] data = Encoding.UTF8.GetBytes($"UPDATE_PROJECT|{json}<EOF>");
                await stream.WriteAsync(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!response.Contains("SUCCESS"))
                {
                    throw new Exception("Server rejected update: " + response);
                }
            }
        }

        private async void PushUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolderPath))
            {
                GscMessageBox.Show("Please select a project folder first.", "Error", MessageBoxButton.OK);
                return;
            }

            try
            {
                Log("Compiling...");
                if (!GameCompilerMap.TryGetValue(_targetGame, out string compilerKey)) compilerKey = "iw6";

                string[] gscFiles = Directory.GetFiles(_selectedFolderPath, "*.gsc", SearchOption.AllDirectories);
                var masterBuilder = new System.Text.StringBuilder();
                foreach (string file in gscFiles) masterBuilder.AppendLine(File.ReadAllText(file));

                byte[] compiledBytes = await RequestCompile(compilerKey, "main.gsc", masterBuilder.ToString());

                if (compiledBytes != null)
                {
                    Log("Upload initiated...");

                    var payload = new UploadPayload
                    {
                        Title = TxtMenuName.Text,
                        Version = TxtVersion.Text,
                        Changelog = TxtChangelog.Text,
                        GscFileName = "main.gscbin",
                        GscFileBase64 = Convert.ToBase64String(compiledBytes)
                    };

                    await SendUpdateToServer(payload);

                    Log("Update successful!");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Log($"Update failed: {ex.Message}");
            }
        }

        public async Task<byte[]> RequestCompile(string game, string scriptName, string code)
        {
            if (string.IsNullOrEmpty(Infrastructure.Session.Token))
            {
                throw new Exception("No active session. Please log in again.");
            }

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
                        throw new Exception(fullError.Replace("|", ""));
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
                throw new Exception(ex.Message);
            }
        }

        private void Log(string message)
        {
            TxtChangelog.Text += $"\n{DateTime.Now:HH:mm:ss} - {message}";
        }
    }
}