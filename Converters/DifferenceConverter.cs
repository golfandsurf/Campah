using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class DifferenceConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typeTarget, object param, CultureInfo culture)
        {
            return Math.Abs((double)value[0] - (double) value[1]);
        }
        public object[] ConvertBack(object value, Type[] typeTarget, object param, CultureInfo culture)
        {
            return null;
        }        
    }
}