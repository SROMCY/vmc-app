using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMC.Misc;

namespace VMC.Measurement
{
    public class CircleGrid2D : IPositionProvider<Point>
    {
        public bool IsFinished { get; private set; }

        private Vector offset;
        private int posInd;
        private Point[] positions;
        public CircleGrid2D(double radius, double stepSize, Vector patternOffset = new Vector())
        {
            IsFinished = false;
            offset = patternOffset;
            posInd = 0;

            positions = CollectPositions(radius, stepSize);
        }

        private static int GetGridSize(double diameter, double stepSize)
        {
            return (int) Math.Floor(diameter / stepSize) + 1;
        }

        private static bool IsWithinCircle(Point pos, double radius)
        {
            Vector vect = (Vector)pos;
            return vect.Length <= radius;
        }

        private static Point[] CollectPositions(double radius, double stepSize)
        {
            List<Point> positions = new List<Point>();
            int gridSize = GetGridSize(2 * radius, stepSize);
            double gridOffset = (double)gridSize / 2 * stepSize;

            for (int xx = 0; xx < gridSize; xx++)
            {
                Point pos = new Point();
                pos.X = xx * stepSize - gridOffset;

                for (int yy = 0; yy < gridSize; yy++)
                {
                    pos.Y = yy * stepSize - gridOffset;

                    if (IsWithinCircle(pos, radius))
                        positions.Add(pos);
                }
            }

            return positions.ToArray();
        }

        public Point GetMaxPos() // not proper implemented yet
        {
            return GetMinPos();
        }

        public Point GetMinPos() // not proper implemented yet
        {
            return (Point)offset;
        }

        public Point GetNextPos()
        {
            if (IsFinished)
                Reset();

            Point pos = positions[posInd];
            ++posInd;

            if (posInd >= positions.Length)
                IsFinished = true;

            return pos + offset;
        }

        public Point GetOffset()
        {
            return (Point)offset;
        }

        public int GetNumPos()
        {
            return positions.Length;
        }

        public void Reset()
        {
            posInd = 0;
            IsFinished = false;
        }
    }
}
