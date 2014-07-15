using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class HintConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return string.IsNullOrEmpty((string) value) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}