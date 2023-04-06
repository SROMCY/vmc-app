using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VMC.Misc;
using VMC.Controller;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VMC.Measurement
{
    public class Ortho2D : Measure<PositionDomain2D>, IMeasure
    {
        private readonly Point _startVectA;
        private readonly Point _endVectA;
        private readonly Point _startVectB;
        private readonly Point _endVectB;
        private readonly Axis _axisX;
        private readonly Axis _axisY;
        public Ortho2D(string name, Axis axisX, Axis axisY, Point startVectA, Point endVectA, Point startVectB, Point endVectB) : base(name)
        {
            _axisX = axisX;
            _axisY = axisY;
            _startVectA = startVectA;
            _endVectA = endVectA;
            _startVectB = startVectB;
            _endVectB = endVectB;
            DataHeader = "PosX[mm];PosY[mm];MeasureX[mm];MeasureY[mm]";
        }

        public Task Measure(string directory, TriaController controller, IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                DateTime startTime = DateTime.Now;

                int prog = 0; // progress variable
                int totalProg = 4;

                // prepare measurement
                result.Clear();
                MetaData.Clear();
                ExtEncoder xEnc = MeasureDevices[0] as ExtEncoder;
                ExtEncoder yEnc = MeasureDevices[1] as ExtEncoder;

                // move to start of vector A
                try
                {
                Task ta1 = controller.Move(_axisX, _startVectA.X);
                Task ta2 = controller.Move(_axisY, _startVectA.Y);
                ta1.Wait(caTok);
                ta2.Wait(caTok);
                }
                catch (OperationCanceledException ex) { return; }
                Thread.Sleep(PreMeasureDelay);

                xEnc.SetOffset(_startVectA.X, xEnc.GetFeedback());
                yEnc.SetOffset(_startVectA.Y, yEnc.GetFeedback());
                result.Add(new PositionDomain2D(_startVectA, new Point(xEnc.GetFeedback(), yEnc.GetFeedback())));

                // report progress
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = "Start of vector A aproached" });

                // move to end of vector A
                try
                {
                Task ta1 = controller.Move(_axisX, _endVectA.X);
                Task ta2 = controller.Move(_axisY, _endVectA.Y);
                ta1.Wait(caTok);
                ta2.Wait(caTok);
                }
                catch (OperationCanceledException ex) { return; }
                Thread.Sleep(PreMeasureDelay);

                result.Add(new PositionDomain2D(_endVectA, new Point(xEnc.GetFeedback(), yEnc.GetFeedback())));

                // report progress
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = "End of vector A aproached" });

                // move to start of vector B
                try
                {
                Task ta1 = controller.Move(_axisX, _startVectB.X);
                Task ta2 = controller.Move(_axisY, _startVectB.Y);
                ta1.Wait(caTok);
                ta2.Wait(caTok);
                }
                catch (OperationCanceledException ex) { return; }
                Thread.Sleep(PreMeasureDelay);

                result.Add(new PositionDomain2D(_startVectB, new Point(xEnc.GetFeedback(), yEnc.GetFeedback())));

                // report progress
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = "Start of vector B aproached" });

                // move to end of vector B
                try { 
                Task ta1 = controller.Move(_axisX, _endVectB.X);
                Task ta2 = controller.Move(_axisY, _endVectB.Y);
                ta1.Wait(caTok);
                ta2.Wait(caTok);
                }
                catch (OperationCanceledException ex) { return; }

                Thread.Sleep(PreMeasureDelay);

                result.Add(new PositionDomain2D(_endVectB, new Point(xEnc.GetFeedback(), yEnc.GetFeedback())));

                Vector a = new Vector(result[1].Measure.X - result[0].Measure.X, result[1].Measure.Y - result[0].Measure.Y);
                Vector b = new Vector(result[3].Measure.X - result[2].Measure.X, result[3].Measure.Y - result[2].Measure.Y);
                double angle = Vector.AngleBetween(a, b);

                // report progress
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = "End of vector B aproached" });

                TimeSpan measureTime = DateTime.Now - startTime;

                // add metadata from measurement
                MetaData.Add(new MetaData("Duration", measureTime.ToString(durationFormat)));
                MetaData.Add(new MetaData("Date", DateTime.Now.ToString(dateFormat)));
                MetaData.Add(new MetaData("EndTime", DateTime.Now.ToString(timeFormat)));
                MetaData.Add(new MetaData("Angle", angle.ToString()));

                string uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}.csv");
                WriteCSV(uniqueFN);

            }, caTok);
        }
    }
}
