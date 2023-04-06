using System.Windows;

namespace VMC.Measurement
{
    public class Grid2D : IPositionProvider<Point>
    {
        public bool IsFinished { get; private set; }

        private readonly int numStepsX;
        private readonly int numStepsY;
        private readonly Vector step;
        private readonly Vector offset;
        private int posIndX;
        private int posIndY;

        public Grid2D(int numberOfStepsX, int numberOfStepsY, Vector stepSize, Vector patternOffset = new Vector())
        {
            numStepsX = numberOfStepsX;
            numStepsY = numberOfStepsY;
            step = stepSize;
            offset = patternOffset;

            Reset();
        }

        public Point GetMaxPos()
        {
            return new Point()
            {
                X = (numStepsX * step.X) + offset.X,
                Y = (numStepsY * step.Y) + offset.Y
            };
        }

        public Point GetMinPos()
        {
            return (Point)offset;
        }

        public Point GetNextPos()
        {
            if (IsFinished)
            {
                Reset();
            }


            Point pos = new Point(posIndX * step.X, posIndY * step.Y);
            pos += offset;

            posIndX++;

            if (posIndX > numStepsX)
            {
                posIndX = 0;
                posIndY++;

                if (posIndY > numStepsY)
                {
                    IsFinished = true;
                }
            }

            return pos;
        }

        public Point GetOffset()
        {
            return (Point)offset;
        }

        public int GetNumPos()
        {
            return (numStepsX + 1) * (numStepsY + 1);
        }

        public void Reset()
        {
            posIndX = 0;
            posIndY = 0;
            IsFinished = false;
        }
    }
}
