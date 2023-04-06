using System;
using System.Globalization;
using System.Windows.Data;

namespace VMC.Misc
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolStringConverter : IValueConverter
    {
        private static readonly string trueStr = "True";
        private static readonly string falseStr = "False";
        private static readonly char[] splitChar = new char[] { '/' };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetString((bool)value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            return text == GetString(true, parameter);
        }

        private string GetString(bool color, object parameter)
        {
            if (parameter != null)
            {
                string strString = parameter as string;

                string[] trueFalseString = strString.Split(splitChar);
                return color ? trueFalseString[0] : trueFalseString[1];
            }
            else
            {
                return color ? trueStr : falseStr;
            }
        }
    }
}
