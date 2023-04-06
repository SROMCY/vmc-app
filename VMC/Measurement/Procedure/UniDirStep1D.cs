
namespace VMC.Measurement
{
    public class UniDirStep1D : IPositionProvider<double>
    {
        public bool IsFinished { get; private set; }

        private readonly int numSteps;
        private readonly double step;
        private readonly double offset;
        private int posInd;

        public UniDirStep1D(int numberOfSteps, double stepSize, double patternOffset = 0)
        {
            IsFinished = false;
            numSteps = numberOfSteps;
            step = stepSize;
            offset = patternOffset;

            Reset();
        }

        public double GetMaxPos()
        {
            return (numSteps * step) + offset;
        }

        public double GetMinPos()
        {
            return offset;
        }

        public double GetNextPos()
        {
            if (IsFinished)
            {
                Reset();
            }

            double pos = (posInd * step) + offset;

            posInd++;

            if (posInd > numSteps)
                IsFinished = true;

            return pos;
        }

        public double GetOffset()
        {
            return offset;
        }

        public int GetNumPos()
        {
            return numSteps + 1;
        }

        public void Reset()
        {
            posInd = 0;
            IsFinished = false;
        }
    }
}
