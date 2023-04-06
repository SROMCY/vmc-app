using System;
using System.Collections.Generic;
using System.Windows;
using FileHelpers;

namespace VMC.Measurement
{
    [DelimitedRecord(";")]
    public class TimeDomain : IComparable
    {
        [FieldConverter(ConverterKind.Date, "dd.MM.yyyy; HH:mm:ss:fff")]
        public DateTime Time { get; set; }

        public float[] Measure { get; set; }

        public TimeDomain()
        {
            Time = DateTime.Now;
            Measure = Array.Empty<float>();
        }
        public TimeDomain(DateTime time, float[] measure)
        {
            Time = time;
            Measure = measure;
        }

        public int CompareTo(object obj)
        {
            if (obj != null)
            {
                if (obj is TimeDomain toComp)
                {
                    if (Time.CompareTo(toComp.Time) != 0)
                    {
                        return Time.CompareTo(toComp.Time);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    throw new ArgumentException("Object is not TimeDomain");
                }
            }
            else
            {
                return 0;
            }
        }
    }
}
