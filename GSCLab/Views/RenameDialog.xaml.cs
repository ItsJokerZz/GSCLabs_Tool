using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GSCLab.Views
{
    public partial class RenameDialog : Window
    {
        public string NewName { get; private set; }

        public RenameDialog(string defaultName, string titleText = "RENAME ITEM", string buttonText = "RENAME")
        {
            InitializeComponent();

            TitleBlock.Text = titleText;
            ActionButton.Content = buttonText;

            NameInput.Text = defaultName;
            NameInput.Focus();
            NameInput.SelectAll();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameInput.Text))
            {
                NewName = NameInput.Text;
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void NameInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) Rename_Click(null, null);
            if (e.Key == System.Windows.Input.Key.Escape) Cancel_Click(null, null);
        }
    }
}
