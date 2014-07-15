using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class MultiVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typeTarget, object param, CultureInfo culture)
        {
            return value.Any(val => (bool)val) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] typeTarget, object param, CultureInfo culture)
        {

            return null;
        }        
    }
}