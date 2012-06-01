using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Converters
{
    public class ProjectedCostConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "The following selections could cost up to " + (int)value + " gil.";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class TotalSpentConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "You have spent " + (int)value + " gil this session";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class LifetimeSpentConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "You have spent " + (int)value + " gil total with Campah";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }



    public class StackConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return "Stack";
            return "Single";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class ItemContainerWidthConverter : IValueConverter
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

    public class ItemIconConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {            
            string[] v = (value as string).Split(',');
            if (v.Length > 0)
                return "images/ahicons/"+v[1] + ".png";
            return "1.png";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }

    public class FormattedMultiTextConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typeTarget, object param, CultureInfo culture)
        {
            if ((bool)value[2])
                return String.Format((string)param, value);
            return String.Format(" {0} ", value);  //Fixes the spacing problem on listview
        }

        public object[] ConvertBack(object value, Type[] typeTarget, object param, CultureInfo culture)
        {

            return null;
        }
    }

    public class MultiVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type typeTarget, object param, CultureInfo culture)
        {
            foreach(object val in value)
                if ((bool)val == true)
                {
                    return System.Windows.Visibility.Visible;
                }
            return System.Windows.Visibility.Hidden;
        }
        public object[] ConvertBack(object value, Type[] typeTarget, object param, CultureInfo culture)
        {

            return null;
        }        
    }

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
    public class HintConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            if (string.IsNullOrEmpty((string)value))
                return System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    public class DelayConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return "Global Delay: " + (int)((double)value) + "ms";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    public class ButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            if ((CampahApp.Modes)value == CampahApp.Modes.Buying)
                return "Stop";
            else
                return "Start";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    public class AddEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return CampahApp.Modes.Stopped == (CampahApp.Modes)value;
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    public class AHParseEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return (CampahApp.Modes.Stopped == (CampahApp.Modes)value || CampahApp.Modes.Updating == (CampahApp.Modes)value);
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    public class AHParseButtonConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            if (CampahApp.Modes.Updating == (CampahApp.Modes) value)
                return "Stop Updating";
            return "Update AH Database XML";
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
    
/*    public class ItemRequestEditConverter : IValueConverter
    {

        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            if ((bool)value == true && CampahApp.CampahStatus.Instance.Mode == CampahApp.Modes.Stopped)
                return System.Windows.Visibility.Visible;
            return System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            return null;
        }
    }
 */
}
