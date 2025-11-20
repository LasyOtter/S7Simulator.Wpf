using System;
using System.Globalization;
using System.Windows.Data;

namespace S7Simulator.Wpf
{
    public class BoolStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "True" : "False";
            return "False";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.Equals("True", StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }
}
