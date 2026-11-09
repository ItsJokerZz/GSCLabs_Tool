using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GSCLab.Views
{
    public partial class GscMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public GscMessageBox(string message, string title, MessageBoxButton buttons)
        {
            InitializeComponent();
            MessageText.Text = message;
            TitleText.Text = title.ToUpper();
            CreateButtons(buttons);
        }

        private void CreateButtons(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, true);
                    break;
                case MessageBoxButton.YesNo:
                    AddButton("YES", MessageBoxResult.Yes, true);
                    AddButton("NO", MessageBoxResult.No, false);
                break;
            }
        }

        private void AddButton(string text, MessageBoxResult result, bool isPrimary)
        {
            Button btn = new Button
            {
                Content = text,
                Width = 90,
                Height = 30,
                Margin = new Thickness(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                Style = (Style)FindResource(isPrimary ? "PrimaryActionBtn" : "SecondaryActionBtn")
            };

            btn.Click += (s, e) => {
                this.Result = result;
                this.DialogResult = true;
                this.Close();
            };

            ButtonContainer.Children.Add(btn);
        }
        public static MessageBoxResult Show(string message, string title = "GSCLab", MessageBoxButton buttons = MessageBoxButton.OK)
        {
            var msg = new GscMessageBox(message, title, buttons);
            msg.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            msg.ShowDialog();
            return msg.Result;
        }
    }
}
