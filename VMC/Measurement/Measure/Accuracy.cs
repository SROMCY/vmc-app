using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Triamec.Tam;
using Triamec.TriaLink;
using VMC.Misc;

namespace VMC.Measurement
{
    public class Accuracy : Measure, IMeasure
    {
        private TamAxis axis;
        private Procedure<double> mp;
        public Accuracy(string name, TamAxis ax, Procedure<double> procedure) : base(name)
        {
            axis = ax;
            mp = procedure;
        }

        public Task StartMeasure(Uri filename, IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            

            return Task.Run(() =>
            {
                int prog = 0; // progress variable
                int totalProg = mp.GetNumberOfPositions(); // number calls of "progess.Report(...);" in code of this method
                while (!mp.IsFinished)
                {
                    axis.MoveAbsolute(mp.GetNextPosition());
                    string progMsg = $"Position {prog} of {totalProg}";
                    progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = progMsg});
                    caTok.ThrowIfCancellationRequested();
                    while (axis.ReadAxisState() == AxisState.Standstill);

                    // measure value
                }
            }, caTok);
        }
    }
}
