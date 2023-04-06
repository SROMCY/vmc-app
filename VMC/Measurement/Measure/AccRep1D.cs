using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using VMC.Misc;
using VMC.Controller;


namespace VMC.Measurement
{
    public class AccRep1D : Measure<PositionDomain1D>, IMeasure
    {
        public bool IsMapping { get; set; }

        private readonly Procedure<double> mp;
        private readonly Axis axis;
        private readonly double blStep;
        private double blSign;
        private bool blDone;
        public AccRep1D(string name, Axis Axis, Procedure<double> procedure, double backlashStep = 0) : base(name)
        {
            axis = Axis;
            DataHeader = "Pos[mm];Error[mm]";
            IsMapping = false;
            mp = procedure;
            blStep = backlashStep;
            blSign = 1;
            blDone = false;
        }

        public Task Measure(string directory, TriaController controller, IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                DateTime startTime = DateTime.Now;

                int prog = 0; // progress variable
                int totalProg = mp.GetNumberOfPositions(); // number calls of "progess.Report(...);" in code of this method

                // prepare measurement
                result.Clear();
                MetaData.Clear();
                mp.Reset();
                IMeasureDevice dev = MeasureDevices[0];
                blDone = true; // avoid blStep at start
                blSign = 1;

                double position = mp.GetOffset();
                try { 
                Task ta = controller.Move(axis, position);
                ta.Wait(caTok);
                }
                catch (OperationCanceledException ex) { return; }
                Thread.Sleep(PreMeasureDelay);

                switch (dev)
                {
                    case Encoder enc:
                        double encPos = controller.GetEncoderFeedback(enc);
                        if (enc.SetZero)
                        {
                            enc.SetOffset(0, encPos);
                        }
                        else
                        {
                            enc.SetOffset(position, encPos);
                        }

                        break;
                    case ExtEncoder enc:
                        encPos = enc.GetFeedback();
                        enc.SetOffset(position, encPos);
                        break;
                    case ExtTrigger trig:
                        MessageBox.Show("Align external measurement device");
                        break;
                    case FeedbackSimulator fbSim:
                        break;
                    default:
                        throw new InvalidEnumArgumentException(dev.GetType().ToString());
                }

                while (!mp.IsFinished)
                {
                    position = mp.GetNextPosition();
                    try { 
                    Task ta = controller.Move(axis, position);
                    ta.Wait(caTok);
                    }
                    catch (OperationCanceledException ex) { return; }
                    Thread.Sleep(PreMeasureDelay);

                    // report progress
                    string progMsg = $"Position {prog} of {totalProg}";
                    progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = progMsg});


                    // measure value
                    switch (dev)
                    {
                        case ExtTrigger trig:
                            controller.SendHWTrigger(trig);
                            trig.ShowMessage(mp.EndOfTravel);
                            result.Add(new PositionDomain1D(position, 0));
                            break;
                        case Encoder enc:
                            double feedbackPos = controller.GetEncoderFeedback(enc);
                            feedbackPos = enc.SetZero ? enc.GetFeedback(feedbackPos) : position - enc.GetFeedback(feedbackPos);
                            result.Add(new PositionDomain1D(position, feedbackPos));
                            break;
                        case ExtEncoder enc:
                            feedbackPos = enc.GetFeedback();
                            result.Add(new PositionDomain1D(position, position - feedbackPos));
                            break;
                        case FeedbackSimulator fbSim:
                            Thread.Sleep(100);
                            result.Add(new PositionDomain1D(position, fbSim.GetFeedback(2)));
                            break;
                        default:
                            throw new InvalidEnumArgumentException(MeasureDevices[0].GetType().ToString());
                    }

                    if (mp.EndOfTravel && !blDone)
                    {
                        try { 
                        Task ta = controller.MoveAdditive(axis, blStep * blSign);
                        ta.Wait(caTok);
                        blSign *= -1;
                        ta = controller.MoveAdditive(axis, blStep * blSign);
                        ta.Wait(caTok);
                        blDone = true;
                        }
                        catch (OperationCanceledException ex) { return; }
                    }
                    else blDone = false;
                }

                TimeSpan measureTime = DateTime.Now - startTime;

                // add metadata from measurement
                MetaData.Add(new MetaData("Duration", measureTime.ToString(durationFormat)));
                MetaData.Add(new MetaData("Date", DateTime.Now.ToString(dateFormat)));
                MetaData.Add(new MetaData("EndTime", DateTime.Now.ToString(timeFormat)));
                MetaData.Add(new MetaData("Repetitions", mp.Repetitions.ToString()));
                int numPos = mp.GetNumberOfPositions() / mp.Repetitions;
                MetaData.Add(new MetaData("NumberOfPositions", numPos.ToString()));

                string uniqueFN;

                if (IsMapping)
                {
                    foreach (PositionDomain1D pd1D in result) // invert measure for mapping
                    {
                        pd1D.Measure *= -1;
                    }

                    TriaMapping1D map = new TriaMapping1D(result, $"{base.Name}");

                    uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}.TAMtbl");
                    map.Write(uniqueFN);
                }
                else
                {
                    uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}.csv");
                    WriteCSV(uniqueFN);
                }
            }, caTok);
        }
    }
}
