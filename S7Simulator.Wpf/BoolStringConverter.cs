using System;
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
            // 修复 CS8116：不能在 switch 表达式中直接使用 bool? 类型模式
            if (value is bool boolValue)
            {
                return boolValue.ToString().ToLowerInvariant();
            }
            // 只能用普通类型判断，不能用模式匹配 bool?
            if (value is null)
            {
                return "false";
            }
            if (value is string stringValue)
            {
                return stringValue;
            }
            return "false";
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
