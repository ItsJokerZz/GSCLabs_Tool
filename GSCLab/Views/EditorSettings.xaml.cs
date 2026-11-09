using GSCLab.Infrastructure;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using GSCLab.Connections;

namespace GSCLab.Views
{
    public partial class EditorSettings : Window
    {
        private readonly CompilerView _editor;
        private EditorTheme _currentTheme;
        private string _configPath = "theme_settings.json";

        public EditorSettings(CompilerView editor)
        {
            InitializeComponent();
            _editor = editor;
            LoadColorLists();
        }
        private void LoadColorLists()
        {
            var colorList = typeof(Colors).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Select(p => p.Name).ToList();

            var defaults = new EditorTheme();
            var defaultValues = new[]
            {
                defaults.StringColor, defaults.KeywordColor, defaults.NumberColor,
                defaults.RtmColor, defaults.ScopeColor, defaults.PointerColor,
                defaults.CommentColor, defaults.FunctionColor, defaults.BoolColor
            };

            foreach (var val in defaultValues)
            {
                if (!colorList.Contains(val))
                {
                    colorList.Insert(0, val);
                }
            }

            ComboBox[] allCombos = { StringColorCombo, KeywordColorCombo, NumberColorCombo, RtmColorCombo, ScopeColorCombo, PointerColorCombo, CommentColorCombo, FunctionColorCombo, BoolColorCombo };

            foreach (var cb in allCombos) cb.ItemsSource = colorList;

            if (System.IO.File.Exists(_configPath))
            {
                string json = System.IO.File.ReadAllText(_configPath);
                _currentTheme = System.Text.Json.JsonSerializer.Deserialize<EditorTheme>(json) ?? new EditorTheme();
            }
            else
            {
                _currentTheme = new EditorTheme();
            }

            StringColorCombo.SelectedItem = _currentTheme.StringColor;
            KeywordColorCombo.SelectedItem = _currentTheme.KeywordColor;
            NumberColorCombo.SelectedItem = _currentTheme.NumberColor;
            RtmColorCombo.SelectedItem = _currentTheme.RtmColor;
            ScopeColorCombo.SelectedItem = _currentTheme.ScopeColor;
            PointerColorCombo.SelectedItem = _currentTheme.PointerColor;
            CommentColorCombo.SelectedItem = _currentTheme.CommentColor;
            FunctionColorCombo.SelectedItem = _currentTheme.FunctionColor;
            BoolColorCombo.SelectedItem = _currentTheme.BoolColor;
            //PS4DebugCheckBox.IsChecked = _currentTheme.UsePS4Debug;
            NexusCheckBox.IsChecked = _currentTheme.UseNexus;
            Ps4IpTextBox.Text = _currentTheme.Ps4IP;
        }
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            _currentTheme.StringColor = StringColorCombo.SelectedItem as string;
            _currentTheme.KeywordColor = KeywordColorCombo.SelectedItem as string;
            _currentTheme.NumberColor = NumberColorCombo.SelectedItem as string;
            _currentTheme.RtmColor = RtmColorCombo.SelectedItem as string;
            _currentTheme.ScopeColor = ScopeColorCombo.SelectedItem as string;
            _currentTheme.PointerColor = PointerColorCombo.SelectedItem as string;
            _currentTheme.CommentColor = CommentColorCombo.SelectedItem as string;
            _currentTheme.FunctionColor = FunctionColorCombo.SelectedItem as string;
            _currentTheme.BoolColor = BoolColorCombo.SelectedItem as string;
            _currentTheme.Ps4IP = Ps4IpTextBox.Text;
            //_currentTheme.UsePS4Debug = PS4DebugCheckBox.IsChecked ?? false;
            _currentTheme.UseNexus = NexusCheckBox.IsChecked ?? false;

            string json = System.Text.Json.JsonSerializer.Serialize(_currentTheme);
            System.IO.File.WriteAllText(_configPath, json);

            ApplyThemeToEditor();

            ToastNotification.Show("Editor Preferences Saved!");

            this.Close();
        }
        private void ApplyThemeToEditor()
        {
            if (_editor == null) return;

            _editor.UpdateHighlightColor("String", GetColorFromCombo(StringColorCombo));
            _editor.UpdateHighlightColor("Keywords", GetColorFromCombo(KeywordColorCombo));
            _editor.UpdateHighlightColor("Numbers", GetColorFromCombo(NumberColorCombo));
            _editor.UpdateHighlightColor("RTMCalls", GetColorFromCombo(RtmColorCombo));
            _editor.UpdateHighlightColor("Scope", GetColorFromCombo(ScopeColorCombo));
            _editor.UpdateHighlightColor("MethodPointer", GetColorFromCombo(PointerColorCombo));
            _editor.UpdateHighlightColor("Comment", GetColorFromCombo(CommentColorCombo));
            _editor.UpdateHighlightColor("Functions", GetColorFromCombo(FunctionColorCombo));
            _editor.UpdateHighlightColor("Bools", GetColorFromCombo(BoolColorCombo));
        }
        private Color GetColorFromCombo(System.Windows.Controls.ComboBox combo)
        {
            if (combo.SelectedItem is string colorName)
            {
                return (Color)ColorConverter.ConvertFromString(colorName);
            }
            return Colors.White;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentTheme = new EditorTheme();

                StringColorCombo.SelectedItem = _currentTheme.StringColor;
                KeywordColorCombo.SelectedItem = _currentTheme.KeywordColor;
                NumberColorCombo.SelectedItem = _currentTheme.NumberColor;
                RtmColorCombo.SelectedItem = _currentTheme.RtmColor;
                ScopeColorCombo.SelectedItem = _currentTheme.ScopeColor;
                PointerColorCombo.SelectedItem = _currentTheme.PointerColor;
                CommentColorCombo.SelectedItem = _currentTheme.CommentColor;
                FunctionColorCombo.SelectedItem = _currentTheme.FunctionColor;
                BoolColorCombo.SelectedItem = _currentTheme.BoolColor;

                string json = System.Text.Json.JsonSerializer.Serialize(_currentTheme);
                System.IO.File.WriteAllText(_configPath, json);

                _editor.UpdateHighlightColor("String", (Color)ColorConverter.ConvertFromString(_currentTheme.StringColor));
                _editor.UpdateHighlightColor("Keywords", (Color)ColorConverter.ConvertFromString(_currentTheme.KeywordColor));
                _editor.UpdateHighlightColor("Numbers", (Color)ColorConverter.ConvertFromString(_currentTheme.NumberColor));
                _editor.UpdateHighlightColor("RTMCalls", (Color)ColorConverter.ConvertFromString(_currentTheme.RtmColor));
                _editor.UpdateHighlightColor("Scope", (Color)ColorConverter.ConvertFromString(_currentTheme.ScopeColor));
                _editor.UpdateHighlightColor("MethodPointer", (Color)ColorConverter.ConvertFromString(_currentTheme.PointerColor));
                _editor.UpdateHighlightColor("Comment", (Color)ColorConverter.ConvertFromString(_currentTheme.CommentColor));
                _editor.UpdateHighlightColor("Functions", (Color)ColorConverter.ConvertFromString(_currentTheme.FunctionColor));
                _editor.UpdateHighlightColor("Bools", (Color)ColorConverter.ConvertFromString(_currentTheme.BoolColor));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to reset theme: {ex.Message}", "GSCLab Error");
            }
        }
        private void NexusCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //if(PS4DebugCheckBox.IsChecked == true)
                //PS4DebugCheckBox.IsChecked = false;

            NexusCheckBox.IsChecked = true;
        }
        private void PS4DebugCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (NexusCheckBox.IsChecked == true)
                NexusCheckBox.IsChecked = false;

            //PS4DebugCheckBox.IsChecked = true;
        }

        private async void SendPayloadPS4_Click(object sender, RoutedEventArgs e)
        {
            bool use_nexus = NexusCheckBox.IsChecked == true;

            await PS4API.SendPayload(Ps4IpTextBox.Text);
        }
    }
}
