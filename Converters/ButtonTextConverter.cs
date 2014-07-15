using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class ButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return (Modes) value == Modes.Buying ? "Stop" : "Start";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}