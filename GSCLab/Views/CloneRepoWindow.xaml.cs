using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GSCLab.Views
{
    public partial class CloneRepoWindow : Window
    {
        public string SelectedRepoUrl { get; private set; }
        public string SelectedLocalPath { get; private set; }

        public CloneRepoWindow()
        {
            InitializeComponent();

            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultPath = Path.Combine(docs, "GSCLab", "Projects");

            if (!Directory.Exists(defaultPath))
                Directory.CreateDirectory(defaultPath);

            LocalPathBox.Text = defaultPath;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RepoUrlBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (RepoUrlBox.Text == "https://github.com/Username/Repo.git")
            {
                RepoUrlBox.Text = "";
                RepoUrlBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = LocalPathBox.Text;
                dialog.Description = "Select a folder to clone the repository into";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LocalPathBox.Text = dialog.SelectedPath;
                }
            }
        }

        private async void CloneButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RepoUrlBox.Text) || !RepoUrlBox.Text.EndsWith(".git"))
            {
                ToastNotification.Show("Please enter a valid Git repository URL");
                return;
            }

            string repoName = Path.GetFileNameWithoutExtension(RepoUrlBox.Text);
            string finalPath = Path.Combine(LocalPathBox.Text, repoName);

            SelectedRepoUrl = RepoUrlBox.Text;
            SelectedLocalPath = finalPath;

            RepoUrlBox.IsEnabled = false;
            LocalPathBox.IsEnabled = false;
            CloneButton.IsEnabled = false;
            CloneButton.Content = "Cloning...";

            try
            {
                var git = await Infrastructure.GitManager.InitializeAsync();

                if (git == null)
                {
                    ToastNotification.Show("Please link your GitHub account");
                    return;
                }

                bool success = false;
                string error = "";

                await System.Threading.Tasks.Task.Run(() =>
                {
                    success = git.CloneRepository(SelectedRepoUrl, SelectedLocalPath, out error);
                });

                if (success)
                {
                    ToastNotification.Show("Successfully Cloned");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ToastNotification.Show("Failed To Clone Repo");
                }
            }
            finally
            {
                if (DialogResult != true)
                {
                    CloneButton.Content = "Clone";
                    CloneButton.IsEnabled = true;
                    RepoUrlBox.IsEnabled = true;
                    LocalPathBox.IsEnabled = true;
                }
            }
        }
    }
}
