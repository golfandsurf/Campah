using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class LifetimeSpentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "You have spent " + (int)value + " gil total with Campah";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}