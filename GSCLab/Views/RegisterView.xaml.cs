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
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.RegisterViewModel();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginView Login = new LoginView();
            Login.Show();
            this.Close();
        }

        private void CloseButton_Click(Object sender, RoutedEventArgs e) => this.Close();
    }
}
