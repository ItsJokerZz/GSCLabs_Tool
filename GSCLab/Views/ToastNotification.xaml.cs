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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GSCLab.Views
{
    public partial class ToastNotification : Window
    {
        public ToastNotification(string message, bool isError)
        {
            InitializeComponent();

            NotificationMessage.Text = message;
            string colorHex = isError ? "#D32F2F" : "#007ACC";
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));

            NotificationToast.BorderBrush = brush;
            NotificationIcon.Foreground = brush;
            NotificationIcon.Text = isError ? "✕" : "ℹ";

            Window mainWindow = Application.Current.MainWindow;

            if (mainWindow != null && mainWindow.WindowState != WindowState.Minimized)
            {
                double margin = 20;

                this.Left = mainWindow.Left + mainWindow.ActualWidth - this.Width - margin;

                this.Top = mainWindow.Top + mainWindow.ActualHeight - this.Height - margin;
            }
            else
            {
                var desktopWorkingArea = SystemParameters.WorkArea;
                this.Left = desktopWorkingArea.Right - this.Width - 10;
                this.Top = desktopWorkingArea.Bottom - this.Height - 10;
            }

            this.Loaded += ToastNotification_Loaded;
        }

        private async void ToastNotification_Loaded(object sender, RoutedEventArgs e)
        {
            var slideIn = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300));

            ToastTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
            NotificationToast.BeginAnimation(OpacityProperty, fadeIn);

            await Task.Delay(3000);

            var slideOut = new DoubleAnimation(20, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));

            ToastTransform.BeginAnimation(TranslateTransform.YProperty, slideOut);
            NotificationToast.BeginAnimation(OpacityProperty, fadeOut);

            await Task.Delay(300);
            this.Close();
        }

        public static void Show(string message, bool isError = false)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is ToastNotification existingToast)
                {
                    existingToast.Close();
                }
            }

            var toast = new ToastNotification(message, isError);
            toast.Show();
        }
    }
}
