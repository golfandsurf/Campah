using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
    public class FormattedMultiTextConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typeTarget, object param, CultureInfo culture)
        {
            if ((bool) value[2])
            {
                return String.Format((string) param, value);
            }

            return String.Format(" {0} ", value);  //Fixes the spacing problem on listview
        }

        public object[] ConvertBack(object value, Type[] typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
}