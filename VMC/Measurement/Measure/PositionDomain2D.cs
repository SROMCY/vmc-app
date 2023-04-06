using System;
using System.Collections.Generic;
using System.Windows;
using FileHelpers;

namespace VMC.Measurement
{
    [DelimitedRecord(";")]
    public class PositionDomain2D : IComparable
    {
        [FieldConverter(typeof(PointConverter))]
        public Point Position { get; set; }
        [FieldConverter(typeof(PointConverter))]
        public Point Measure { get; set; }

        public PositionDomain2D()
        {
            Position = new Point();
            Measure = new Point();
        }
        public PositionDomain2D(Point position, Point measure)
        {
            Position = position;
            Measure = measure;
        }

        public int CompareTo(object obj)
        {
            if (obj != null)
            {
                PositionDomain2D toComp = obj as PositionDomain2D;
                if (toComp != null)
                {
                    if (Position.X.CompareTo(toComp.Position.X) != 0)
                    {
                        return Position.X.CompareTo(toComp.Position.X);
                    }
                    else if (Position.Y.CompareTo(toComp.Position.Y) != 0)
                    {
                        return Position.Y.CompareTo(toComp.Position.Y);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    throw new ArgumentException("Object is not a PositionDomain2D");
                }
            }
            else
            {
                return 0;
            }
        }
    }
}