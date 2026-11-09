using GSCLab.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GSCLab.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private Announcement _latestAnnouncement;
        public Announcement LatestAnnouncement
        {
            get => _latestAnnouncement;
            set { _latestAnnouncement = value; OnPropertyChanged(); }
        }

        private string _onlineUserCount = "0";
        private string _registeredUserCount = "0";
        private string _totalInjections = "0";

        public string OnlineUsersCount
        {
            get => _onlineUserCount;
            set { _onlineUserCount = value; OnPropertyChanged(); }
        }

        public string RegisteredUsersCount
        {
            get => _registeredUserCount;
            set { _registeredUserCount = value; OnPropertyChanged(); }
        }

        public string TotalInjections
        {
            get => _totalInjections;
            set { _totalInjections = value; OnPropertyChanged(); }
        }

        private async Task<string> GetStatsFromServerAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes("GET_STATS");
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
            }
            catch
            {
                return "0|0|0";
            }
        }

        private async Task RefreshStatsAsync()
        { 
            string rawData = await GetStatsFromServerAsync();

            if (!string.IsNullOrEmpty(rawData))
            {
                string[] parts = rawData.Split('|');
                if (parts.Length == 3)
                {
                    OnlineUsersCount = parts[0];
                    RegisteredUsersCount = parts[1];
                    TotalInjections = parts[2];
                }
            }
        }

        public ObservableCollection<GameTitle> SupportedGames { get; set; }

        public class GameTitle
        {
            public string GameName { get; set; }
            public string ImagePath { get; set; }
            public string Platforms { get; set; }
        }

        private void LoadSupportedGames()
        {
            SupportedGames = new ObservableCollection<GameTitle>
            {
                new GameTitle { GameName = "COD4", ImagePath = "pack://application:,,,/Assets/cod4.jpg", Platforms = "PS3" },
                new GameTitle { GameName = "MW3", ImagePath = "pack://application:,,,/Assets/mw3.jpg", Platforms = "PS3" },
                new GameTitle { GameName = "GHOSTS", ImagePath = "pack://application:,,,/Assets/ghosts.jpg", Platforms = "PS3 | PS4" },
                new GameTitle { GameName = "AW", ImagePath = "pack://application:,,,/Assets/aw.jpg", Platforms = "PS3 | PS4" },
                new GameTitle { GameName = "BO3", ImagePath = "pack://application:,,,/Assets/BO3.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "MWR", ImagePath = "pack://application:,,,/Assets/mwr.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "IW", ImagePath = "pack://application:,,,/Assets/iw.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "WW2", ImagePath = "pack://application:,,,/Assets/ww2.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "MW2019", ImagePath = "pack://application:,,,/Assets/mw2019.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "MW2CR", ImagePath = "pack://application:,,,/Assets/mw2cr.jpg", Platforms = "PS4" },
                //new GameTitle { GameName = "CW", ImagePath = "pack://application:,,,/Assets/cw.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "VANGUARD", ImagePath = "pack://application:,,,/Assets/vanguard.jpg", Platforms = "PS4" },
                new GameTitle { GameName = "MW2 2022", ImagePath = "pack://application:,,,/Assets/mw22.jpg", Platforms = "PS4" },
                //new GameTitle { GameName = "MW3 2023", ImagePath = "pack://application:,,,/Assets/mwiii.png", Platforms = "PS4" },
            };
        }

        private async Task FetchLatestAnnouncementAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = Encoding.UTF8.GetBytes("GET_NEWS");
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (!string.IsNullOrEmpty(response) && response.Contains("|"))
                    {
                        string[] parts = response.Split('|');
                        if (parts.Length >= 3)
                        {
                            LatestAnnouncement = new Announcement
                            {
                                Title = parts[0],
                                Content = parts[1].Replace("\\n", Environment.NewLine),
                                Changelog = parts[2].Replace("\\n", Environment.NewLine)
                            };
                        }
                    }
                }
            }
            catch {  }
        }

        private async Task RefreshAllDataAsync()
        {
            await RefreshStatsAsync();
            await FetchLatestAnnouncementAsync();
        }

        public HomeViewModel()
        {
            LatestAnnouncement = new Announcement { Title = "Loading...", Content = "Connecting to GSCLab Server..." };

            LoadSupportedGames();
            _ = RefreshAllDataAsync();
        }
    }
}
