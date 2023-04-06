using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using VMC.Misc;
using VMC.Controller;

namespace VMC.Measurement
{
    public class MoveAndSettle2D : Measure<PositionDomain2DtoN>, IMeasure
    {
        private readonly Dictionary<Axis, double> axMotion;
        private readonly Axis axOne;
        private readonly Axis axTwo;
        private readonly Procedure<Point> mp;
        private readonly double move;
        private readonly float setRad, minSetTime;

        public MoveAndSettle2D(string name, Axis firstAxis, Axis secondAxis, Procedure<Point> procedure, double moveSize, float settlingRadius, float minSettlingTime) : base(name)
        {
            axOne = firstAxis;
            axTwo = secondAxis;

            axMotion = new Dictionary<Axis, double>()
            {
                { axOne, 0 },
                { axTwo, 0 }
            };
            mp = procedure;
            move = moveSize;
            setRad = settlingRadius;
            minSetTime = minSettlingTime;

            DataHeader = "PositionX;PositionY;SettlingTimePosX[s];SettlingTimePosY[s];SettlingTimeNegX[s];SettlingTimeNegY[s]";
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
                Point position;
                List<Task<float>> tasks;
                controller.SetSettlingParam(axOne, setRad, minSetTime);
                controller.SetSettlingParam(axTwo, setRad, minSetTime);

                while (!mp.IsFinished)
                {
                    List<float> moveTimes = new List<float>();

                    // approach measure position
                    position = mp.GetNextPosition();
                    axMotion[axOne] = position.X - (move / 2);
                    axMotion[axTwo] = position.Y - (move / 2);
                    try { 
                    tasks = controller.Move(axMotion);
                    Task.WaitAll(tasks.ToArray(), caTok);
                    }
                    catch (OperationCanceledException ex) { return; }
                    Thread.Sleep(PreMeasureDelay);

                    // positive approached
                    axMotion[axOne] = move;
                    axMotion[axTwo] = move;
                    try { 
                    tasks = controller.MoveAdditive(axMotion);
                    foreach (Task<float> ta in tasks)
                    {
                        ta.Wait(caTok);
                        moveTimes.Add(ta.Result - minSetTime);
                    }
                    }
                    catch (OperationCanceledException ex) { return; }
                    Thread.Sleep(PreMeasureDelay);

                    // negative approached
                    axMotion[axOne] = -move;
                    axMotion[axTwo] = -move;
                    try { 
                    tasks = controller.MoveAdditive(axMotion);
                    foreach (Task<float> ta in tasks)
                    {
                        ta.Wait(caTok);
                        moveTimes.Add(ta.Result - minSetTime);
                    }
                    }
                    catch (OperationCanceledException ex) { return; }
                    result.Add(new PositionDomain2DtoN(position, moveTimes.ToArray()));

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
