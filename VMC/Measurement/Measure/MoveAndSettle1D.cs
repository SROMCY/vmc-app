using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using VMC.Misc;
using VMC.Controller;

namespace VMC.Measurement
{
    public class MoveAndSettle1D : Measure<PositionDomain1DtoN>, IMeasure
    {
        private readonly Axis ax;
        private readonly Procedure<double> mp;
        private readonly double move;
        private readonly float setRad, minSetTime;

        public MoveAndSettle1D(string name, Axis axis, Procedure<double> procedure, double moveSize, float settlingRadius, float minSettlingTime) : base(name)
        {
            ax = axis;
            mp = procedure;
            move = moveSize;
            setRad = settlingRadius;
            minSetTime = minSettlingTime;

            DataHeader = "Position;SettlingTimePos[s];SettlingTimeNeg[s]";
        }

        public Task Measure(string directory, TriaController controller, IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                DateTime startTime = DateTime.Now;
                string uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}.csv");
                int totalProg = mp.GetNumberOfPositions();
                int prog = 0;
                result.Clear();
                MetaData.Clear();
                mp.Reset();
                double position;
                Task<float> ta;
                controller.SetSettlingParam(ax, setRad, minSetTime);

                while (!mp.IsFinished)
                {
                    List<float> moveTimes = new List<float>();

                    position = mp.GetNextPosition();
                    try { 
                    ta = controller.Move(ax, position - (move / 2));
                    ta.Wait(caTok); // approach measure position
                    }
                    catch (OperationCanceledException ex) { return; }
                    Thread.Sleep(PreMeasureDelay);

                    // positive approached
                    try { 
                    ta = controller.MoveAdditive(ax, move);
                    ta.Wait(caTok);
                    }
                    catch (OperationCanceledException ex) { return; }
                    moveTimes.Add(ta.Result - minSetTime);

                    Thread.Sleep(PreMeasureDelay);

                    // negative approached
                    try { 
                    ta = controller.MoveAdditive(ax, -move);
                    ta.Wait(caTok);
                    }
                    catch (OperationCanceledException ex) { return; }
                    moveTimes.Add(ta.Result - minSetTime);

                    result.Add(new PositionDomain1DtoN(position, moveTimes.ToArray()));

                    // report progress and check for canellation
                    string progMsg = $"Position {prog} of {totalProg}";
                    progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = progMsg });
                }

                TimeSpan measureTime = DateTime.Now - startTime;

                // add metadata from measurement
                MetaData.Add(new MetaData("Duration", measureTime.ToString(durationFormat)));
                MetaData.Add(new MetaData("Date", DateTime.Now.ToString(dateFormat)));
                MetaData.Add(new MetaData("EndTime", DateTime.Now.ToString(timeFormat)));
                MetaData.Add(new MetaData("Repetitions", mp.Repetitions.ToString()));
                int numPos = mp.GetNumberOfPositions() / mp.Repetitions;
                MetaData.Add(new MetaData("NumberOfPositions", numPos.ToString()));

                WriteCSV(uniqueFN);

            }, caTok);
        }
    }
}
