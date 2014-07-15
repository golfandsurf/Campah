using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class AhParseButtonConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return Modes.Updating == (Modes) value ? "Stop Updating" : "Update AH Database XML";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}
