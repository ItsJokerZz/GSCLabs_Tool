using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GSCLab.Views
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string Changelog { get; set; }
        public string DownloadUrl { get; set; }
    }

    public class UpdateItem
    {
        public string VersionTag { get; set; }
        public string StatusColor { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public string DownloadUrl { get; set; }
    }

    public partial class UpdateView : Window
    {
        private string _downloadUrl;

        public UpdateView(List<UpdateItem> updates)
        {
            InitializeComponent();

            UpdatesList.ItemsSource = updates;

            var latest = updates.FirstOrDefault();
            if (latest != null)
            {
                VersionTxt.Text = latest.VersionTag;
                _downloadUrl = latest.DownloadUrl;
            }

            this.MouseDown += (s, e) => {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) this.DragMove();
            };
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_downloadUrl)) return;

            try
            {
                string[] parts = _downloadUrl.Split(':');
                string vpsIp = parts[0];
                int vpsPort = int.Parse(parts[1]);

                string tempPath = Path.Combine(Path.GetTempPath(), "GSCLab_Update.zip");
                UpdateBtn.IsEnabled = false;

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(vpsIp, vpsPort);
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] cmd = Encoding.UTF8.GetBytes("DOWNLOAD_UPDATE");
                        await stream.WriteAsync(cmd, 0, cmd.Length);

                        byte[] sizeBuffer = new byte[4];
                        await stream.ReadAsync(sizeBuffer, 0, 4);
                        int fileSize = BitConverter.ToInt32(sizeBuffer, 0);

                        using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192];
                            int totalRead = 0;

                            while (totalRead < fileSize)
                            {
                                int read = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, fileSize - totalRead));
                                if (read == 0) break;

                                await fs.WriteAsync(buffer, 0, read);
                                totalRead += read;

                                int percent = (int)((totalRead / (float)fileSize) * 100);
                                this.Dispatcher.Invoke(() => UpdateBtn.Content = $"Downloading {percent}%");
                            }
                        }
                    }
                }
                CreateAndRunUpdater(tempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed: " + ex.Message);
                UpdateBtn.IsEnabled = true;
                UpdateBtn.Content = "UPDATE NOW";
            }
        }

        private void CreateAndRunUpdater(string zipFile)
        {
            string batchPath = Path.Combine(Path.GetTempPath(), "update_gsclab.bat");
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string script = $@"@echo off
:loop
tasklist /FI ""PID eq {Process.GetCurrentProcess().Id}"" | findstr /I ""{Process.GetCurrentProcess().ProcessName}"" > nul
if %errorlevel% equ 0 (timeout /t 1 > nul & goto loop)

powershell -Command ""Expand-Archive -Path '{zipFile}' -DestinationPath '{currentDir}temp_update' -Force""
if exist ""{currentDir}temp_update\net8.0-windows"" (
    robocopy ""{currentDir}temp_update\net8.0-windows"" ""{currentDir}"" /E /MT:8 /IS /MOVE > nul
) else (
    robocopy ""{currentDir}temp_update"" ""{currentDir}"" /E /MT:8 /IS /MOVE > nul
)
rmdir /S /Q ""{currentDir}temp_update""
del ""{zipFile}""
start """" ""{currentExe}""
del ""%~f0""";

            File.WriteAllText(batchPath, script, Encoding.Default);

            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            Application.Current.Shutdown();
        }
    }
}