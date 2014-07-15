using System;
using System.Globalization;
using System.Windows.Data;

namespace Converters
{
    public class MaxConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return ((double)value);
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}