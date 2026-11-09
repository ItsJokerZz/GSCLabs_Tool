using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GSCLab.Views
{
    public partial class ProjectConfigControl : UserControl, INotifyPropertyChanged
    {
        private bool _isInitialLoading = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private string _lastValidPlatform = "unknown";

        private static readonly Dictionary<string, HashSet<string>> GameToPlatforms = new()
        {
            { "mw1",     new() { "ccapi" } },//Call of Duty: 4 Modern Warfare
            { "mw3",     new() { "ccapi", "hen" } },//Call of Duty: Modern Warfare 3
            { "ghosts",  new() { "ccapi", "hen", "ps4" } },//Call of Duty: Ghosts
            { "aw",      new() { "ccapi", "hen", "ps4" } },//Call of Duty: Advanced Warfare
            { "bo3",     new() { "" } },//Call of Duty: Black Ops III
            { "mwr",     new() { "ps4" } },//Call of Duty: Modern Warfare Remastered
            { "mw2cr",   new() { "ps4" } },//Call of Duty: Modern Warfare 2 Campaign Remastered
            { "ww2",     new() { "ps4" } },//Call of Duty: World War II
            { "iw",      new() { "ps4" } },//Call of Duty: Infinite Warfare
            { "vanguard", new() { "ps4" } },//Call of Duty: Vanguard
            { "mw22", new() { "ps4" } },//Call of Duty: MW2 2022
            { "mw2019", new() {"ps4"} },//Call of Duty: Modern Warfare 2019
            { "mwiii", new() { "ps4" } }
        };

        public ProjectConfigControl()
        {
            InitializeComponent();
            Infrastructure.ProjectManager.ConfigUI = this;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string SelectedGameCode => GetSelectedUid(GamePanel);
        public string SelectedModeCode => GetSelectedUid(ModePanel);
        public string SelectedPlatform => GetSelectedUid(PlatformPanel);

        private string GetSelectedUid(WrapPanel panel)
        {
            if (panel == null) return "unknown";
            var selectedRadio = panel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
            return selectedRadio?.Uid ?? "unknown";
        }

        private void SetSelectedUid(WrapPanel panel, string uidToSelect)
        {
            if (panel == null || string.IsNullOrEmpty(uidToSelect)) return;
            var target = panel.Children.OfType<RadioButton>().FirstOrDefault(r => r.Uid == uidToSelect);
            if (target != null) target.IsChecked = true;
        }
        private void FilterPlatforms(string gameCode)
        {
            if (PlatformPanel == null || string.IsNullOrEmpty(gameCode)) return;

            GameToPlatforms.TryGetValue(gameCode.ToLower(), out var allowedPlatforms);
            allowedPlatforms ??= new HashSet<string>();

            foreach (var radio in PlatformPanel.Children.OfType<RadioButton>())
            {
                radio.Visibility = allowedPlatforms.Contains(radio.Uid) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void FilterModes(string gameCode)
        {
            if (ModePanel == null) return;

            var modes = ModePanel.Children.OfType<RadioButton>().ToDictionary(r => r.Uid);

            foreach (var radio in modes.Values)
            {
                radio.Visibility = Visibility.Collapsed;
                if (radio.Uid == "sp") { radio.Content = "SINGLEPLAYER"; radio.Tag = "pack://application:,,,/Assets/sp_logo.png"; }
                if (radio.Uid == "mp") { radio.Content = "MULTIPLAYER"; radio.Tag = "pack://application:,,,/Assets/mp_logo.png"; }
                if (radio.Uid == "zm") { radio.Content = "ZOMBIES"; radio.Tag = "pack://application:,,,/Assets/zm_logo.png"; }
                if (radio.Uid == "ex") { radio.Content = "EXTINCTION"; radio.Tag = "pack://application:,,,/Assets/extinction_logo.png"; }
            }

            switch (gameCode?.ToLower())
            {
                case "aw":
                    if (modes.TryGetValue("sp", out var spAw)) spAw.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpAw)) mpAw.Visibility = Visibility.Visible;

                    if (modes.TryGetValue("zm", out var zmAw))
                    {
                        zmAw.Visibility = Visibility.Visible;
                        zmAw.Content = "EXO ZOMBIES";
                        zmAw.Tag = "pack://application:,,,/Assets/ez_logo.png";
                    }
                    if (modes.TryGetValue("ex", out var esAw))
                    {
                        esAw.Visibility = Visibility.Visible; esAw.Content = "EXO SURVIVAL"; esAw.Tag = "pack://application:,,,/Assets/sp_logo.png";
                    }
                    break;

                case "mw2cr":
                    if (modes.TryGetValue("sp", out var spMw2)) { spMw2.Visibility = Visibility.Visible; spMw2.IsChecked = true; }
                    break;

                case "ww2":
                    if (modes.TryGetValue("sp", out var spWw2)) spWw2.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpWw2)) mpWw2.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("zm", out var zmWw2)) zmWw2.Visibility = Visibility.Visible;
                    break;

                case "ghosts":
                    if (modes.TryGetValue("sp", out var spG)) spG.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpG)) mpG.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("zm", out var zmG)) { zmG.Visibility = Visibility.Visible; zmG.Content = "EXTINCTION"; zmG.Tag = "pack://application:,,,/Assets/extinction_logo.png"; }
                    //if (modes.TryGetValue("ex", out var exG)) exG.Visibility = Visibility.Visible;
                    break;

                case "bo3":
                    if (modes.TryGetValue("sp", out var spBo3)) { spBo3.Visibility = Visibility.Visible; spBo3.Content = "CAMPAIGN"; }
                    if (modes.TryGetValue("mp", out var mpBo3)) mpBo3.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("zm", out var zmBo3)) zmBo3.Visibility = Visibility.Visible;
                    break;

                case "mw1":
                case "mwr":
                    if (modes.TryGetValue("sp", out var spMw)) spMw.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpMw)) mpMw.Visibility = Visibility.Visible;
                    break;
                case "mw3":
                    if (modes.TryGetValue("sp", out var spMw3)) spMw3.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpMw3)) mpMw3.Visibility = Visibility.Visible;
                    break;
                case "iw":
                    if (modes.TryGetValue("sp", out var spIW)) spIW.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpIW)) mpIW.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("zm", out var zmIW)) zmIW.Visibility = Visibility.Visible;
                    break;
                case "vanguard":
                    if (modes.TryGetValue("sp", out var spVAN)) spVAN.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("mp", out var mpVAN)) mpVAN.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("zm", out var zmVAN)) zmVAN.Visibility = Visibility.Visible;
                    break;
                case "mw22":
                    if (modes.TryGetValue("mp", out var mpMW22)) mpMW22.Visibility = Visibility.Visible;
                    break;
                case "mw2019":
                    if (modes.TryGetValue("mp", out var mpMW2019)) mpMW2019.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("sp", out var spMW2019)) spMW2019.Visibility = Visibility.Visible;
                    break;
                case "mwiii":
                    if (modes.TryGetValue("mp", out var mpMWiii)) mpMWiii.Visibility = Visibility.Visible;
                    if (modes.TryGetValue("sp", out var spMWiii)) spMWiii.Visibility = Visibility.Visible;
                    break;
            }
        }

        private async void OnGameSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitialLoading) return;

            string game = GetSelectedUid(GamePanel);

            FilterPlatforms(game);
            FilterModes(game);

            string platform = GetSelectedUid(PlatformPanel);
            var selectedPlatformBtn = PlatformPanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.Uid == platform);
            if (selectedPlatformBtn == null || selectedPlatformBtn.Visibility == Visibility.Collapsed)
            {
                var firstVisiblePlatform = PlatformPanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.Visibility == Visibility.Visible);
                if (firstVisiblePlatform != null)
                {
                    firstVisiblePlatform.IsChecked = true;
                    platform = firstVisiblePlatform.Uid;
                }
            }

            string mode = GetSelectedUid(ModePanel);
            var selectedModeBtn = ModePanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.Uid == mode);
            if (selectedModeBtn == null || selectedModeBtn.Visibility == Visibility.Collapsed)
            {
                var firstVisibleMode = ModePanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.Visibility == Visibility.Visible);
                if (firstVisibleMode != null)
                {
                    firstVisibleMode.IsChecked = true;
                    mode = firstVisibleMode.Uid;
                }
            }

            Infrastructure.ProjectManager.ActiveGame = game;
            Infrastructure.ProjectManager.ActiveMode = mode;
            Infrastructure.ProjectManager.ActivePlatform = platform;

            UpdateModeLabel(mode);

            OnPropertyChanged(nameof(SelectedGameCode));
            OnPropertyChanged(nameof(SelectedModeCode));

            if (game == "unknown" || platform == "unknown") return;

            await LoadDatabase($"{game}_{platform}", true);
        }

        public async void SaveConfiguration(string projectFolderPath)
        {
            string game = GetSelectedUid(GamePanel);
            string platform = GetSelectedUid(PlatformPanel);

            var configData = new GSCProjectConfig
            {
                ProjectTitle = TitleTextBox.Text,
                Author = AuthorTextBox.Text,
                StartDate = StartDateTextBox.Text,
                GameCode = game,
                ModeCode = GetSelectedUid(ModePanel),
                PlatformCode = platform
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(configData, options);
            File.WriteAllText(Path.Combine(projectFolderPath, "config.gl"), jsonString);

            await LoadDatabase($"{game}_{platform}", true);
        }

        public async void LoadConfiguration(string projectFolderPath)
        {
            string loadPath = Path.Combine(projectFolderPath, "config.gl");
            if (!File.Exists(loadPath)) return;

            try
            {
                _isInitialLoading = true;

                string jsonString = File.ReadAllText(loadPath);
                var configData = JsonSerializer.Deserialize<GSCProjectConfig>(jsonString);

                if (configData != null)
                {
                    TitleTextBox.Text = configData.ProjectTitle;
                    AuthorTextBox.Text = configData.Author;
                    StartDateTextBox.Text = configData.StartDate;

                    SetSelectedUid(GamePanel, configData.GameCode);
                    FilterModes(configData.GameCode);
                    FilterPlatforms(configData.GameCode);
                    SetSelectedUid(ModePanel, configData.ModeCode);
                    SetSelectedUid(PlatformPanel, configData.PlatformCode);

                    await Application.Current.Dispatcher.InvokeAsync(() => 
                    {
                        string finalGame = GetSelectedUid(GamePanel);
                        string finalMode = GetSelectedUid(ModePanel);
                        string finalPlatform = GetSelectedUid(PlatformPanel);

                        Infrastructure.ProjectManager.ActiveGame = finalGame;
                        Infrastructure.ProjectManager.ActiveMode = finalMode;
                        Infrastructure.ProjectManager.ActivePlatform = finalPlatform;

                        UpdateModeLabel(finalMode);
                    });

                    await LoadDatabase($"{configData.GameCode}_{configData.PlatformCode}", false);

                    _lastValidPlatform = configData.PlatformCode;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
            finally { _isInitialLoading = false; }
        }

        private void UpdateModeLabel(string modeCode)
        {
            ModeTextBox.Text = modeCode switch
            {
                "zm" => "Zombies",
                "mp" => "Multiplayer",
                "sp" => "Campaign",
                "ex" => "Extinction",
                "es" => "Exo Survival",
                "ez" => "Exo Zombies",
                _ => ModeTextBox.Text
            };
        }

        private async Task LoadDatabase(string gameToLoad, bool shouldNotify)
        {
            string folderName = ReturnFolderName(gameToLoad);
            if (folderName == "unknown") return;

            await Infrastructure.FunctionLibrary.LoadGameData(folderName);

            if (shouldNotify)
            {
                Infrastructure.ProjectManager.NotifyProjectChanged();
            }
        }

        private string ReturnFolderName(string gameCombination)
        {
            return gameCombination switch
            {
                "aw_ps4" => "aw_ps4",
                "ghosts_ps4" => "ghosts_ps4",
                "mwr_ps4" => "mwr_ps4",
                "ww2_ps4" => "ww2_ps4",
                "mw2cr_ps4" => "mw2cr_ps4",
                "mw3_ccapi" => "mw3_ps3",
                "mw3_hen" => "mw3_ps3",
                "ghosts_hen" => "iw6_ps3",
                "ghosts_ccapi" => "iw6_ps3",
                "aw_hen" => "s1_ps3",
                "aw_ccapi" => "s1_ps3",
                "iw_ps4" => "iw_ps4",
                "mw22_ps4" => "mw2_ps4",
                "mw2019_ps4" => "mw2019_ps4",
                "vanguard_ps4" => "vanguard_ps4",
                _ => "unknown"
            };
        }
    }

    public class GSCProjectConfig
    {
        public string ProjectTitle { get; set; }
        public string Author { get; set; }
        public string StartDate { get; set; }
        public string GameCode { get; set; }
        public string ModeCode { get; set; }
        public string PlatformCode { get; set; }
    }
}