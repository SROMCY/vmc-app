using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using VMC.Measurement;

namespace VMC.Controller
{
    public class TriaMapping1D
    {
        private readonly TriaTblHeader header;
        private readonly TriaTblDimension[] tableDimension;
        private double[] data;
        public TriaMapping1D(List<PositionDomain1D> value, string description)
        {
            tableDimension = new TriaTblDimension[2];
            ArrangeData(value);

            header = new TriaTblHeader()
            {
                Persistent = true,
                ChecksumMode = ChecksumMode.Calculate,
                Date = DateTimeOffset.UtcNow,
                Id = GetHashCode(),
                Description = description,
                RowSize = 1,
                FirstDimension = tableDimension[0],
                SecondDimension = tableDimension[1],
            };
        }

        public void Write(string filename)
        {
            header.Write(filename);
            using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Append)))
            {
                for (int ii = 0; ii < data.Length; ii++)
                {
                    writer.Write((float)data[ii]);
                }
            }
        }

        private void ArrangeData(List<PositionDomain1D> value)
        {
            value.Sort(); // sort by x coordinate and second by y coordinate
            List<PositionDomain1D> mapData = new List<PositionDomain1D>();
            double pos = double.MinValue;

            // calculate average for each position
            foreach (PositionDomain1D measure in value)
            {
                if (measure.Position != pos)
                {
                    pos = measure.Position;
                    List<PositionDomain1D> redundantMeasurements = (value.FindAll(
                        delegate (PositionDomain1D pd1D)
                        {
                            return pd1D.Position == pos;
                        }
                        )
                    );

                    PositionDomain1D average = new PositionDomain1D
                    {
                        Position = pos,
                        Measure = redundantMeasurements.Average(c => c.Measure)
                    };
                    mapData.Add(average);
                }
            }

            // set dimension of table
            tableDimension[0].Size = mapData.Count;
            tableDimension[0].StartValue = (float)mapData.Min(PositionDomain1D => PositionDomain1D.Position);
            tableDimension[0].Distance = (float)(mapData[1].Position - mapData[0].Position);

            tableDimension[1].Size = 1;

            data = mapData.Select(c => c.Measure).ToArray();
        }
    }
}
