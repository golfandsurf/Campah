using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class ItemIconConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            var s = value as string;
            if (s != null)
            {
                var v = s.Split(',');
                if (v.Length > 0)
                {
                    return "images/ahicons/" + v[1] + ".png";
                }
            }
            return "1.png";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}