using GSCLab.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GSCLab.Views
{
    public partial class OpenProjectView : Window
    {
        public ObservableCollection<GameGroup> GameGroups { get; set; } = new ObservableCollection<GameGroup>();
        public static Action CloseProject;

        public OpenProjectView()
        {
            InitializeComponent();
            DataContext = this;
            LoadUserProjects();
        }

        private void LoadUserProjects()
        {
            string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GSCLab");
            string[] codFolders = {  "COD4", "MW3", "GHO", "AW", "MWR", "IW", "WW2", "MW2019", "MW2CR", "VG", "MWII", "Bundle Projects" };

            if (!Directory.Exists(rootPath)) return;

            foreach (string folder in codFolders)
            {
                string gamePath = Path.Combine(rootPath, folder);

                if (Directory.Exists(gamePath))
                {
                    var projectsInGame = new List<GSCProject>();

                    string fullGameName = folder switch
                    {
                        "COD4" => "Modern Warfare",
                        "MW3" => "Modern Warfare 3",
                        "GHO" => "Call of Duty: Ghosts",
                        "AW" => "Advanced Warfare",
                        "MWR" => "Modern Warfare Remastered",
                        "IW" => "Infinite Warfare",
                        "WW2" => "Call of Duty: WWII",
                        "MW2019" => "Modern Warfare 2019",
                        "MW2CR" => "MW2 Campaign Remastered",
                        "VG" => "Vanguard",
                        "MWII" => "Modern Warfare II",
                        "Bundle Projects" => "Bundle Projects",
                        _ => folder
                    };

                    string logo = folder switch
                    {
                        "COD4" => "pack://application:,,,/Assets/cod4_logo.png",
                        "MW3" => "pack://application:,,,/Assets/mw3_logo.png",
                        "IW" => "pack://application:,,,/Assets/iw_logo.png",
                        "MWR" => "pack://application:,,,/Assets/mwr_logo.png",
                        "WW2" => "pack://application:,,,/Assets/ww2_logo.png",
                        "MW2CR" => "pack://application:,,,/Assets/mw2cr_logo.png",
                        "GHO" => "pack://application:,,,/Assets/gho_logo.png",
                        "AW" => "pack://application:,,,/Assets/aw_logo.png",
                        "Bundle Projects" => "pack://application:,,,/Assets/bundle_logo.png",
                        "MWII" => "pack://application:,,,/Assets/mwii_logo.png",
                        "VG" => "pack://application:,,,/Assets/vg_logo.png",
                        "MW2019" => "pack://application:,,,/Assets/mw2019_logo.png",
                        _ => "/Assets/default_gsc.png"
                    };

                    foreach (string projectDir in Directory.GetDirectories(gamePath))
                    {
                        projectsInGame.Add(new GSCProject
                        {
                            ProjectName = Path.GetFileName(projectDir),
                            FullPath = projectDir,
                            GameTitle = fullGameName,
                            IconPath = logo
                        });
                    }

                    if (projectsInGame.Count > 0)
                    {
                        GameGroups.Add(new GameGroup
                        {
                            GameName = fullGameName,
                            GameIcon = logo,
                            Projects = new ObservableCollection<GSCProject>(projectsInGame)
                        });
                    }
                }
            }

            ProjectsGroupsControl.ItemsSource = GameGroups;
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindVisualParent<ScrollViewer>((ListBox)sender);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public class GSCProject
        {
            public string ProjectName { get; set; }
            public string FullPath { get; set; }
            public string GameTitle { get; set; }
            public string IconPath { get; set; }
        }

        public class GameGroup
        {
            public string GameName { get; set; }
            public string GameIcon { get; set; }
            public ObservableCollection<GSCProject> Projects { get; set; }
        }

        private void ProjectsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is GSCProject selected)
            {
                CloseProject?.Invoke();
                ProjectManager.LoadProject(selected.FullPath);

                this.DialogResult = true;
                this.Close();
            }
        }

        private void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            NewProjectView newProject = new NewProjectView();
            newProject.ShowDialog();
            this.Close();
        }

        private void btnCloneProject_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Views.CloneRepoWindow Clone = new CloneRepoWindow();
            Clone.ShowDialog();
        }
    }
}