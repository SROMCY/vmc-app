using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VMC.Measurement
{
    public class BiDirStep2D : IPositionProvider<Point>
    {
        public bool IsFinished { get; private set; }

        public bool ReturnDirect { get; set; }

        private int numSteps;
        private Vector step;
        private Vector offset;
        private int posInd;
        private bool countUp;

        public BiDirStep2D(int numberOfSteps, Vector stepVector, Vector patternOffset = new Vector(), bool returnDirect = false)
        {
            IsFinished = false;
            ReturnDirect = returnDirect;
            numSteps = numberOfSteps;
            step = stepVector;
            offset = patternOffset;

            Reset();
        }

        public Point GetMaxPos()
        {
            return (Point)(numSteps * step) + offset;
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

                if (ReturnDirect) posInd++;
            }

            Point pos = (Point)(posInd * step) + offset;

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

        public Point GetOffset()
        {
            return (Point)offset;
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
