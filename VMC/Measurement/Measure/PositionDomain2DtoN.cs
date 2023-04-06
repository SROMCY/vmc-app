using System;
using System.Collections.Generic;
using System.Windows;
using FileHelpers;

namespace VMC.Measurement
{
    [DelimitedRecord(";")]
    public class PositionDomain2DtoN : IComparable
    {
        [FieldConverter(typeof(PointConverter))]
        public Point Position { get; private set; }
        public float[] Measure { get; private set; }

        public PositionDomain2DtoN()
        {
            Position = new Point();
            Measure = Array.Empty<float>();
        }
        public PositionDomain2DtoN(Point position, float[] measure)
        {
            Position = position;
            Measure = measure;
        }

        public int CompareTo(object obj)
        {
            if (obj != null)
            {
                PositionDomain2DtoN toComp = obj as PositionDomain2DtoN;
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