using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GSCLab.Infrastructure
{
    public class IndentConverter : IValueConverter
    {
        public double IndentMultiplier { get; set; } = 15.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as TreeViewItem;
            if (item == null) return new Thickness(0);

            int level = 0;
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (parent != null && !(parent is TreeView))
            {
                if (parent is TreeViewItem) level++;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return new Thickness(level * IndentMultiplier, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
