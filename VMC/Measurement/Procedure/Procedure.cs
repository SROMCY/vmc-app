using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMC.Measurement
{
    public class Procedure<T>
    {
        public bool IsFinished { get; private set; }
        public bool EndOfTravel { get; private set; }
        public int Repetitions { get; private set; }

        private readonly IPositionProvider<T> provider;
        private int repIndex;
        public Procedure(IPositionProvider<T> posProvider, int repetitions)
        {
            provider = posProvider;
            Repetitions = repetitions;
            Reset();
        }

        public int GetNumberOfPositions()
        {
            return Repetitions * provider.GetNumPos();
        }

        public T GetNextPosition()
        {
            if (IsFinished)
                Reset();

            T position = provider.GetNextPos();

            EndOfTravel = position.Equals(provider.GetMinPos()) || position.Equals(provider.GetMaxPos());

            if (provider.IsFinished)
                repIndex++;

            if (repIndex >= Repetitions)
                IsFinished = true;

            return position;
        }

        public T GetOffset()
        {
            return provider.GetOffset();
        }

        public void Reset()
        {
            provider.Reset();
            repIndex = 0;
            IsFinished = false;
            EndOfTravel = true;
        }
    }
}
