using GSCLab.Infrastructure;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GSCLab.Views
{
    public class MenuSupportConfig
    {
        public string GameName { get; set; }
        public string ShortName { get; set; }
        public List<string> Modes { get; set; } = new List<string>();
        public List<string> Consoles { get; set; } = new List<string>();
    }

    public class UploadPayload
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public List<MenuSupportConfig> SupportedGames { get; set; }
        public string PriceType { get; set; }
        public string Price { get; set; }
        public string CouponCode { get; set; }
        public string CouponPercent { get; set; }
        public string Description { get; set; }
        public string Changelog { get; set; }
        public string YoutubeLink { get; set; }
        public string GscFileName { get; set; }
        public string GscFileBase64 { get; set; }
        public string ImageFileName { get; set; }
        public string ImageFileBase64 { get; set; }
    }

    public class LibraryCategory
    {
        public string Title { get; set; }
        public string TargetName { get; set; }
        public string LogoPath { get; set; }
        public bool IsSelected { get; set; }
        public bool HasSeparator { get; set; }
        public Visibility SeparatorVisibility => HasSeparator ? Visibility.Visible : Visibility.Collapsed;
    }

    public partial class LibraryView : UserControl
    {
        #region Server Calls
        private readonly string SERVER_IP = "45.90.116.64";
        private readonly int SERVER_PORT = 8888;
        #endregion

        public ObservableCollection<LibraryCategory> NavCategories { get; set; }
        private List<UploadPayload> _currentMenus = new List<UploadPayload>();

        public LibraryView()
        {
            InitializeComponent();

            this.DataContext = this;

            Discord.UpdatePresence("Idle", "In Library");

            NavCategories = new ObservableCollection<LibraryCategory>
            {
                new LibraryCategory { Title = "Call of Duty: Modern Warfare 3", TargetName = "MW3", LogoPath = "pack://application:,,,/Assets/mw3_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Ghosts", TargetName = "GHOSTS", LogoPath = "pack://application:,,,/Assets/gho_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Advanced Warfare", TargetName = "AW", LogoPath = "pack://application:,,,/Assets/aw_logo.png" },
                new LibraryCategory { Title = "Call Of Duty: World War II", TargetName = "WW2", LogoPath = "pack://application:,,,/Assets/ww2_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Modern Warfare Remastered", TargetName = "MWR", LogoPath = "pack://application:,,,/Assets/mwr_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Modern Warfare 2 Campaign Remastered", TargetName = "MW2CR", LogoPath = "pack://application:,,,/Assets/mw2cr_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Infinite Warfare", TargetName = "IW", LogoPath = "pack://application:,,,/Assets/iw_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Modern Warfare 2019", TargetName = "MW2019", LogoPath = "pack://application:,,,/Assets/mw2019_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Vanguard", TargetName = "VG", LogoPath = "pack://application:,,,/Assets/vg_logo.png" },
                new LibraryCategory { Title = "Call of Duty: Modern Warfare II ( 2022 )", TargetName = "MWII", LogoPath = "pack://application:,,,/Assets/mwii_logo.png" },
                new LibraryCategory { Title = "Bundles & Collections", TargetName = "Bundles", LogoPath = "pack://application:,,,/Assets/bundle_logo.png", HasSeparator = true }
            };

            UserRoleText.Text = Session.Role;

            if (Session.Role == "Owner" || Session.Role == "GSC Developer" || Session.Role == "Admin")
                ShowUploadMenuBtn.Visibility = Visibility.Visible;
            else
                ShowUploadMenuBtn.Visibility = Visibility.Hidden;

            LoadHomePage();
        }

        private void ShowUploadMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            PublishMenuWindow publishWin = new PublishMenuWindow();
            publishWin.ShowDialog();
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.DataContext is LibraryCategory category)
            {
                ViewTitle.Text = category.Title + " Menus";
                Discord.UpdatePresence("Viewing " + ReturnNames(category.TargetName), "In Library");
                LoadMarketplace(category.TargetName);
            }
        }

        private void HomeNavBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cat in NavCategories)
            {
                cat.IsSelected = false;
            }

            LoadHomePage();
        }

        private async void LoadHomePage()
        {
            ViewTitle.Text = "Marketplace Home";
            Discord.UpdatePresence("Browsing Home", "In Library");

            StorefrontHomeFeed.Visibility = Visibility.Visible;
            ScriptsContainer.Visibility = Visibility.Collapsed;

            SearchInput.Text = "Search menus...";

            RecentContainer.Children.Clear();
            FreeContainer.Children.Clear();
            InjectedContainer.Children.Clear();
            PaidContainer.Children.Clear();

            RecentContainer.Children.Add(new TextBlock { Text = "Loading marketplace catalog...", Foreground = Brushes.Gray, Margin = new Thickness(10) });

            try
            {
                string jsonResponse = await Task.Run(() => {
                    using (TcpClient client = new TcpClient(SERVER_IP, SERVER_PORT))
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] cmd = Encoding.UTF8.GetBytes("GET_MARKET|ALL<EOF>");
                        stream.Write(cmd, 0, cmd.Length);
                        StringBuilder sb = new StringBuilder();
                        byte[] buffer = new byte[8192];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        return sb.ToString().Replace("<EOF>", "");
                    }
                });

                RecentContainer.Children.Clear();

                if (jsonResponse == "[]" || string.IsNullOrEmpty(jsonResponse))
                {
                    _currentMenus.Clear();
                    RecentContainer.Children.Add(new TextBlock { Text = "No menus available right now.", Foreground = Brushes.Gray, Margin = new Thickness(10) });
                    return;
                }

                var menus = JsonSerializer.Deserialize<List<UploadPayload>>(jsonResponse);
                _currentMenus = menus ?? new List<UploadPayload>();

                PopulateHomeSections();
            }
            catch
            {
                RecentContainer.Children.Clear();
                RecentContainer.Children.Add(new TextBlock { Text = "Connection Error: Could not reach the GSCLab Marketplace.", Foreground = Brushes.Red, Margin = new Thickness(10) });
            }
        }

        private void PopulateHomeSections()
        {
            // 1. Most Recent Uploads (Limited to 5)
            var recentMenus = _currentMenus.Take(5);
            foreach (var menu in recentMenus)
            {
                RecentContainer.Children.Add(CreateStoreCard(menu));
            }

            // 2. Free Menus (Limited to 5)
            var freeMenus = _currentMenus.Where(m => string.IsNullOrEmpty(m.PriceType) || m.PriceType.Contains("Free") || m.Price == "0" || m.Price == "0.00").Take(5);
            foreach (var menu in freeMenus)
            {
                FreeContainer.Children.Add(CreateStoreCard(menu));
            }

            // 3. Top Injected Menus (Limited to 5)
            var topInjected = _currentMenus.OrderByDescending(m => 0).Take(5);
            foreach (var menu in topInjected)
            {
                InjectedContainer.Children.Add(CreateStoreCard(menu));
            }

            // 4. Paid & Premium Menus (Limited to 5)
            var paidMenus = _currentMenus.Where(m => !string.IsNullOrEmpty(m.PriceType) && m.PriceType.Contains("Paid")).Take(5);
            foreach (var menu in paidMenus)
            {
                PaidContainer.Children.Add(CreateStoreCard(menu));
            }
        }

        private Border CreateStoreCard(UploadPayload menu, int injectionCount = 0)
        {
            bool isFree = string.IsNullOrEmpty(menu.PriceType) || menu.PriceType.Contains("Free");
            string displayPrice = string.IsNullOrWhiteSpace(menu.Price) ? "0.00" : menu.Price;
            string priceText = isFree ? "Free" : $"${displayPrice}";
            string priceColor = isFree ? "#888" : "#28A745";

            string cardXaml = $@"<Border xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" Background=""#0C0C0C"" BorderBrush=""#1A1A1A"" BorderThickness=""1"" CornerRadius=""8"" Width=""320"" Height=""90"" Margin=""0,0,15,0"" Cursor=""Hand"" ClipToBounds=""True""><Grid><Grid.ColumnDefinitions><ColumnDefinition Width=""120""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions><Border Grid.Column=""0"" Background=""#080808"" BorderBrush=""#1C1C1C"" BorderThickness=""0,0,1,0""><Image x:Name=""CoverImage"" Stretch=""Uniform"" RenderOptions.BitmapScalingMode=""HighQuality"" HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border><Grid Grid.Column=""1"" Margin=""12,8""><Grid.RowDefinitions><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""*""/></Grid.RowDefinitions><DockPanel Grid.Row=""0""><TextBlock Text=""{priceText}"" Foreground=""{priceColor}"" FontWeight=""Bold"" FontSize=""11"" DockPanel.Dock=""Right"" HorizontalAlignment=""Right""/><TextBlock Text=""{System.Security.SecurityElement.Escape(menu.Title)}"" Foreground=""White"" FontWeight=""Bold"" FontSize=""12"" TextTrimming=""CharacterEllipsis"" Margin=""0,0,6,0""/></DockPanel><TextBlock Grid.Row=""1"" Text=""By {System.Security.SecurityElement.Escape(menu.Author)}"" Foreground=""#666"" FontSize=""9"" Margin=""0,2,0,4""/><StackPanel Grid.Row=""2"" Orientation=""Horizontal"" VerticalAlignment=""Bottom""><Border Background=""#141414"" BorderBrush=""#222"" BorderThickness=""1"" CornerRadius=""3"" Padding=""5,1""><StackPanel Orientation=""Horizontal""><TextBlock Text=""⚡ "" FontSize=""8"" VerticalAlignment=""Center""/><TextBlock Text=""{injectionCount} Injections"" Foreground=""#007ACC"" FontSize=""9"" FontWeight=""SemiBold"" VerticalAlignment=""Center""/></StackPanel></Border></StackPanel></Grid></Grid></Border>";

            Border newCard = (Border)XamlReader.Parse(cardXaml);
            Image imgControl = (Image)newCard.FindName("CoverImage");

            if (imgControl != null && !string.IsNullOrEmpty(menu.ImageFileBase64))
            {
                byte[] imgBytes = Convert.FromBase64String(menu.ImageFileBase64);
                using (var ms = new MemoryStream(imgBytes))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    imgControl.Source = bitmap;
                }
            }

            newCard.MouseLeftButtonUp += (s, e) => {
                var detailsWin = new MenuDetailsWindow(menu);
                detailsWin.ShowDialog();
            };

            return newCard;
        }

        private string ReturnNames(string input)
        {
            switch (input)
            {
                case "GHOSTS": return "Ghosts Mod Menus";
                case "AW": return "Advanced Warfare Mod Menus";
                case "MW2CR": return "Modern Warfare 2 Campaign Remastered Mod Menus";
                case "WW2": return "World War 2 Mod Menus";
                case "MWR": return "Modern Warfare Remastered Mod Menus";
                case "IW": return "Infinite Warfare Mod Menus";
                case "MW3": return "Modern Warfare 3 Mod Menus";
                case "MW2019": return "Modern Warfare 2019 Mod Menus";
                case "MWII": return "Modern Warfare II (2022) Mod Menus";
                case "VANGUARD": return "Vanguard Mod Menus";
                case "Bundles": return "Bundles";
                default: return "Idling";
            }
        }

        private async void LoadMarketplace(string category)
        {
            StorefrontHomeFeed.Visibility = Visibility.Collapsed;
            ScriptsContainer.Visibility = Visibility.Visible;

            ScriptsContainer.Children.Clear();
            ScriptsContainer.Children.Add(new TextBlock { Text = $"Loading {category} menus...", Foreground = Brushes.Gray, Margin = new Thickness(20) });

            SearchInput.Text = "Search menus...";

            try
            {
                string jsonResponse = await Task.Run(() => {
                    using (TcpClient client = new TcpClient(SERVER_IP, SERVER_PORT))
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] cmd = Encoding.UTF8.GetBytes($"GET_MARKET|{category}<EOF>");
                        stream.Write(cmd, 0, cmd.Length);
                        StringBuilder sb = new StringBuilder();
                        byte[] buffer = new byte[8192];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        return sb.ToString().Replace("<EOF>", "");
                    }
                });

                ScriptsContainer.Children.Clear();

                if (jsonResponse == "[]" || string.IsNullOrEmpty(jsonResponse))
                {
                    _currentMenus.Clear();
                    ScriptsContainer.Children.Add(new TextBlock { Text = "No menus found in this category.", Foreground = Brushes.Gray, Margin = new Thickness(20) });
                    return;
                }

                var menus = JsonSerializer.Deserialize<List<UploadPayload>>(jsonResponse);
                _currentMenus = menus ?? new List<UploadPayload>();

                FilterAndDisplayMenus();
            }
            catch
            {
                ScriptsContainer.Children.Clear();
                ScriptsContainer.Children.Add(new TextBlock { Text = "Connection Error: Could not reach the GSCLab Marketplace.", Foreground = Brushes.Red, Margin = new Thickness(20) });
            }
        }

        private void FilterAndDisplayMenus()
        {
            string query = SearchInput.Text.ToLower().Trim();
            if (query == "search menus...") query = "";

            if (!string.IsNullOrEmpty(query))
            {
                StorefrontHomeFeed.Visibility = Visibility.Collapsed;
                ScriptsContainer.Visibility = Visibility.Visible;
            }
            else if (ViewTitle.Text == "Marketplace Home")
            {
                StorefrontHomeFeed.Visibility = Visibility.Visible;
                ScriptsContainer.Visibility = Visibility.Collapsed;
                return;
            }

            ScriptsContainer.Children.Clear();

            bool foundAny = false;

            foreach (var menu in _currentMenus)
            {
                if (string.IsNullOrEmpty(query) ||
                    (menu.Title != null && menu.Title.ToLower().Contains(query)) ||
                    (menu.Author != null && menu.Author.ToLower().Contains(query)))
                {
                    ScriptsContainer.Children.Add(CreateStoreCard(menu));
                    foundAny = true;
                }
            }

            if (!foundAny)
            {
                ScriptsContainer.Children.Add(new TextBlock { Text = "No menus match your search.", Foreground = Brushes.Gray, Margin = new Thickness(20) });
            }
        }

        private void SearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentMenus != null && _currentMenus.Count > 0)
            {
                FilterAndDisplayMenus();
            }
        }

        private void SearchInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchInput.Text == "Search menus...")
            {
                SearchInput.Text = "";
            }
        }

        private void SearchInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchInput.Text))
            {
                SearchInput.Text = "Search menus...";
            }
        }
    }
}