using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VMC.Misc
{
    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolColorConverter : IValueConverter
    {
        private static readonly Color trueCol = Colors.LightGreen;
        private static readonly Color falseCol = Colors.DarkGreen;
        private static readonly char[] splitChar = new char[] { '/' };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new SolidColorBrush(GetColor((bool)value, parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = value as SolidColorBrush;
            return brush.Color == GetColor(true, parameter);
        }

        private Color GetColor(bool color, object parameter)
        {
            if (parameter != null)
            {
                string colString = parameter as string;

                string[] trueFalseColors = colString.Split(splitChar);
                return color ? (Color)ColorConverter.ConvertFromString(trueFalseColors[0]) : (Color)ColorConverter.ConvertFromString(trueFalseColors[1]);
            }
            else
            {
                return color ? trueCol : falseCol;
            }
        }
    }
}
