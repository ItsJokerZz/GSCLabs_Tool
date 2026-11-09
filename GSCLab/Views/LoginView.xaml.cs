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
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.LoginViewModel();

            var vm = new ViewModels.LoginViewModel();
            this.DataContext = vm;

            var saved = Infrastructure.AppSettings.Load();
            if (saved.remember)
            {
                PassBox.Password = saved.pass;

                vm.Username = saved.user;
                vm.RememberMe = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        public string GetPassowrd()
        {
            return PassBox.Password;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterView reg = new RegisterView();
            reg.Show();
            this.Close();
        }
    }
}
