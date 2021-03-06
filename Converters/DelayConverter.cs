﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace CampahApp.Converters
{
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
}