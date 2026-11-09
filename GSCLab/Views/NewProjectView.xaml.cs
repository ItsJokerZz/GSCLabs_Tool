using GSCLab.Infrastructure;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace GSCLab.Views
{
    public partial class NewProjectView : Window
    {
        public NewProjectView()
        {
            InitializeComponent();
            PopulateGames();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string NameHelper(string game)
        {
            switch (game)
            {
                case "mw1": return "COD4";
                case "mw3": return "MW3";
                case "ghosts": return "GHO";
                case "aw": return "AW";
                case "mwr": return "MWR";
                case "iw": return "IW";
                case "ww2": return "WW2";
                case "mw2019": return "MW2019";
                case "mw2cr": return "MW2CR";
                case "vanguard": return "VG";
                case "mw22": return "MWII";
                case "mw323": return "MWIII";
                default: return "Unknown Game";
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string projectName = ProjectNameBox.Text.Trim();
            if (string.IsNullOrEmpty(projectName))
            {
                MessageBox.Show("Please enter a project name.");
                return;
            }

            var selectedGame = (ComboBoxItem)GameSelector.SelectedItem;
            string shortGameFolder = selectedGame.Tag.ToString()!;

            var selectedMode = ModeSelector.SelectedItem as ComboBoxItem;
            string modeCode = selectedMode?.Tag?.ToString() ?? "";

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string projectFolderPath = System.IO.Path.Combine(documentsPath, "GSCLab", NameHelper(shortGameFolder), projectName);

            try
            {
                if (!Directory.Exists(projectFolderPath))
                {
                    Directory.CreateDirectory(projectFolderPath);

                    string mainGscPath = System.IO.Path.Combine(projectFolderPath, "main.gsc");
                    string author = string.IsNullOrWhiteSpace(AuthorBox.Text) ? "Unknown" : AuthorBox.Text;

                    string gscTemplate =
                    $"// Project: {projectName}\n" +
                    $"// Author: {author}\n" +
                    $"// Game: {selectedGame.Content} : Mode {selectedMode.Content}\n\n" +
                    "init()\n{\n    level thread onPlayerConnect();\n}\n\n" +
                    "onPlayerConnect()\n{\n    for(;;)\n    {\n        level waittill(\"connected\", player);\n        // Add logic here\n    }\n}";

                    File.WriteAllText(mainGscPath, gscTemplate);

                    var configData = new GSCProjectConfig
                    {
                        ProjectTitle = projectName,
                        Author = author,
                        StartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        GameCode = shortGameFolder,
                        ModeCode = modeCode,
                    };

                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(configData, options);
                    File.WriteAllText(System.IO.Path.Combine(projectFolderPath, "config.gl"), jsonString);

                    Infrastructure.ProjectManager.LoadProject(projectFolderPath);

                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("A project with this name already exists in that game folder.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating project: {ex.Message}");
            }
        }

        private void PopulateGames()
        {
            GameSelector.Items.Add(new ComboBoxItem { Content = "Modern Warfare", Tag = "mw1" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Modern Warfare 3", Tag = "mw3" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Ghosts", Tag = "ghosts" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Advanced Warfare", Tag = "aw" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Modern Warfare Remastered", Tag = "mwr" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Infinite Warfare", Tag = "iw" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "World War II", Tag = "ww2" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Modern Warfare 2019", Tag = "mw2019" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "MW2 Campaign Remastered", Tag = "mw2cr" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Vanguard", Tag = "vanguard" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Modern Warfare II (2022)", Tag = "mw22" });
            GameSelector.Items.Add(new ComboBoxItem { Content = "Modern Warfare III (2023)", Tag = "mw323" });

            GameSelector.SelectedIndex = 0;
        }

        private void GameSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModeSelector == null) return;

            ModeSelector.Items.Clear();

            if (GameSelector.SelectedItem is ComboBoxItem selectedGame)
            {
                string gameTag = selectedGame.Tag?.ToString();

                switch (gameTag)
                {
                    case "mw3":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Spec Ops", Tag = "sp" });
                        break;
                    case "ww2":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Zombies", Tag = "zm" });
                        break;
                    case "mw2cr":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        break;
                    case "aw":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Exo Zombies", Tag = "zm" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Exo Survival", Tag = "zm" });
                        break;
                    case "ghosts":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Extinction", Tag = "zm" });
                        break;
                    case "iw":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Zombies", Tag = "zm" });
                        break;
                    case "vanguard":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Zombies", Tag = "zm" });
                        break;
                    case "mw22":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        break;
                    case "mw2019":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Spec Ops", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        break;
                    case "mw323":
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        break;
                    case "mwr":
                    case "cod4":
                    default:
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Multiplayer", Tag = "mp" });
                        ModeSelector.Items.Add(new ComboBoxItem { Content = "Singleplayer", Tag = "sp" });
                        break;
                }

                if (ModeSelector.Items.Count > 0)
                    ModeSelector.SelectedIndex = 0;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}