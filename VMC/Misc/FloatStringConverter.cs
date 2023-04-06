using System;
using System.Globalization;
using System.Windows.Data;

namespace VMC.Misc
{
    [ValueConversion(typeof(float), typeof(string))]
    public class FloatStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float number = (float)value;
            return number.ToString("N1", culture.NumberFormat);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (string)value;
            if (float.TryParse(text, NumberStyles.Number, culture.NumberFormat, out float number))
                return number;
            else
                return null;
        }
    }
}
