using System;
using System.Windows;
using FileHelpers;

namespace VMC.Measurement
{
    public class PointConverter : ConverterBase
    {
        public override object StringToField(string from)
        {
            Point pos = new Point();
            char[] delim = { ';' };
            string[] splited = from.Split(delim);
            pos.X = Convert.ToDouble(double.Parse(splited[0]));
            pos.Y = Convert.ToDouble(double.Parse(splited[1]));

            return pos;
        }

        public override string FieldToString(object fieldValue)
        {
            Point pos = (Point)fieldValue;
            return string.Concat(pos.X.ToString(), ";", pos.Y.ToString());
        }
    }
}
