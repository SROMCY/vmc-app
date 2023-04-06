using System;
using System.Collections.Generic;
using System.Windows;
using FileHelpers;

namespace VMC.Measurement
{
    [DelimitedRecord(";")]
    public class PositionDomain1DtoN : IComparable
    {
        public double Position { get; set; }

        public float[] Measure { get; set; }

        public PositionDomain1DtoN()
        {
            Position = 0;
            Measure = Array.Empty<float>();
        }
        public PositionDomain1DtoN(double position, float[] measure)
        {
            Position = position;
            Measure = measure;
        }

        public int CompareTo(object obj)
        {
            if (obj != null)
            {
                if (obj is PositionDomain1DtoN toComp)
                {
                    if (Position.CompareTo(toComp.Position) != 0)
                    {
                        return Position.CompareTo(toComp.Position);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    throw new ArgumentException("Object is not PositionDomain1DtoN");
                }
            }
            else
            {
                return 0;
            }
        }
    }
}
