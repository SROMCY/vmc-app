using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using VMC.Measurement;

namespace VMC.Controller
{
    public class TriaMapping2D
    {
        private readonly TriaTblHeader header;
        private readonly TriaTblDimension[] tableDimension;
        private double[,] data;
        public TriaMapping2D(List<PositionDomain2D> value, string description, bool useFirstMeaurementDimension = true)
        {
            if (value.Count >= 4)
            {
                tableDimension = new TriaTblDimension[3];
                ArrangeData(value, useFirstMeaurementDimension);

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
                    ThirdDimension = tableDimension[2],
                };
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public void Write(string filename)
        {
            header.Write(filename);
            using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Append)))
            {
                for (int ii = 0; ii < data.GetLength(0); ii++)
                {
                    for (int jj = 0; jj < data.GetLength(1); jj++)
                    {
                        writer.Write((float)data[ii,jj]);
                    }
                }
            }
        }

        private void ArrangeData(List<PositionDomain2D> value, bool useFirstMeaurementDimension)
        {
            value.Sort(); // sort by x coordinate and second by y coordinate

            List<List<PositionDomain2D>> sortedXY = new List<List<PositionDomain2D>>();
            double posX = double.MinValue;

            // sort by x coordinate to list
            foreach (PositionDomain2D measure in value)
            {
                if (measure.Position.X != posX)
                {
                    posX = measure.Position.X;
                    sortedXY.Add(value.FindAll(
                        delegate (PositionDomain2D pd2D)
                        {
                            return pd2D.Position.X == posX;
                        }
                        )
                    );
                }
            }

            int dimX = sortedXY.Count;
            int dimY = 0; // find biggest dimension
            foreach (List<PositionDomain2D> list in sortedXY)
            {
                if (list.Count > dimY)
                {
                    dimY = list.Count;
                }
            }

            // set dimension of table
            tableDimension[0].Size = dimX;
            tableDimension[0].StartValue = (float)value.Min(PositionDomain2D => PositionDomain2D.Position.X);
            tableDimension[0].Distance = (float)(sortedXY[1][0].Position.X - sortedXY[0][0].Position.X);

            tableDimension[1].Size = dimY;
            tableDimension[1].StartValue = (float)value.Min(PositionDomain2D => PositionDomain2D.Position.Y);
            tableDimension[1].Distance = (float)(sortedXY[0][1].Position.Y - sortedXY[0][0].Position.Y);

            tableDimension[2].Size = 1;

            data = new double[dimX, dimY];

            // fill data into 2D array
            for (int ii = 0; ii < sortedXY.Count; ii++)
            {
                int jj = (dimY - sortedXY[ii].Count) / 2;

                for (int kk = 0; kk < sortedXY[ii].Count; kk++)
                {
                    data[ii, jj] = useFirstMeaurementDimension
                        ? sortedXY[ii][kk].Measure.X
                        : sortedXY[ii][kk].Measure.Y;
                    jj++;
                }
            }
        }
    }
}
