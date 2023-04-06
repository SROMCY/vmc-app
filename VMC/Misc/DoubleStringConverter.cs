using System;
using System.Globalization;
using System.Windows.Data;

namespace VMC.Misc
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double number = (double)value;
            return number.ToString("N3", culture.NumberFormat);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (string)value;
            if (double.TryParse(text, NumberStyles.Number, culture.NumberFormat, out double number))
                return number;
            else
                return null;
        }
    }
}
