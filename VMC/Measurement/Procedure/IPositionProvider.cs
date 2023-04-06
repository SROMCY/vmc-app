using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMC.Measurement
{
    public interface IPositionProvider<T>
    {
        bool IsFinished { get; } // Flag is set to true, if all positions in procedure are returned
        T GetMaxPos(); // returns position with highest position index
        T GetMinPos(); // return position with position index equal to 0
        T GetNextPos(); // returns next position in procedure
        T GetOffset(); // returns offset position
        int GetNumPos(); // returns amount of positions, returned by GetNextPos(), till IsFinished flag will be set to true
        void Reset(); // reset position index within IPositionProviders
    }
}
