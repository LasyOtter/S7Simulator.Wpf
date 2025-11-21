using System.Globalization;
using System.Windows.Data;

namespace S7Simulator.Wpf
{
    public class BoolStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                bool boolValue => boolValue,
                string stringValue => ParseString(stringValue),
                _ => false
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                bool boolValue => boolValue.ToString().ToLowerInvariant(),
                bool? nullableBool => (nullableBool ?? false).ToString().ToLowerInvariant(),
                string stringValue => stringValue,
                _ => "false"
            };
        }

        private static bool ParseString(string value)
        {
            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return value switch
            {
                "1" => true,
                "0" => false,
                _ => false
            };
        }
    }
}
