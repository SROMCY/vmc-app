using System;
using System.Collections.Generic;
using System.Windows;
using FileHelpers;

namespace VMC.Measurement
{
    [DelimitedRecord(";")]
    public class PositionDomain1D : IComparable
    {
        public double Position { get; set; }
        public double Measure { get; set; }

        public PositionDomain1D()
        {
            Position = 0;
            Measure = 0;
        }
        public PositionDomain1D(double position, double measure)
        {
            Position = position;
            Measure = measure;
        }

        public int CompareTo(object obj)
        {
            if (obj != null)
            {
                if (obj is PositionDomain1D toComp)
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
                    throw new ArgumentException("Object is not a PositionDomain1D");
                }
            }
            else
            {
                return 0;
            }
        }
    }
}
