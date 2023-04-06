using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMC.Measurement
{
    public class BiDirStep1D : IPositionProvider<double>
    {
        public bool IsFinished { get; private set; }
        public bool EndOfTravel { get; private set; } // Flag is set to true, if position index is equal to 0 or last possible index
        public bool ReturnDirect { get; set; }

        private int numSteps;
        private double step;
        private double offset;
        private int posInd;
        private bool countUp;

        public BiDirStep1D(int numberOfSteps, double stepSize, double patternOffset = 0, bool returnDirect = false)
        {
            IsFinished = false;
            ReturnDirect = returnDirect;
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

                if (ReturnDirect) posInd++;
            }

            double pos = (posInd * step) + offset;

            if (posInd >= numSteps && countUp)
            {
                countUp = false;

                if (ReturnDirect) posInd--;
            }
            else
            {
                if (countUp)
                    posInd++;
                else
                    posInd--;
            }

            if (posInd == -1)
                IsFinished = true;

            return pos;
        }

        public double GetOffset()
        {
            return offset;
        }

        public int GetNumPos()
        {
            if (ReturnDirect)
            {
                return 2 * numSteps;
            }
            else
            {
                return 2 * (numSteps + 1);
            }
        }

        public void Reset()
        {
            posInd = 0;
            countUp = true;
            IsFinished = false;
        }
    }
}
