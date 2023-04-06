using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using VMC.Misc;
using VMC.Controller;


namespace VMC.Measurement
{
    public class AccRep2D : Measure<PositionDomain2D>, IMeasure
    {
        public bool IsMapping { get; set; }

        private readonly Procedure<Point> mp;
        private readonly Axis firstAxis;
        private readonly Axis secondAxis;
        private readonly Vector blStep;
        private double blSign;
        private bool blDone;
        public AccRep2D(string name, Axis measureAxis1, Axis measureAxis2, Procedure<Point> procedure, Vector backlashStep = new Vector()) : base(name)
        {
            firstAxis = measureAxis1;
            secondAxis = measureAxis2;
            DataHeader = "PosX[mm];PosY[mm];ErrorX[mm];ErrorY[mm]";
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
                blDone = true; // avoid blStep at start
                blSign = 1;

                Point position = mp.GetOffset();
                Dictionary<Axis, double> moves = new Dictionary<Axis, double>
                {
                    { firstAxis, position.X },
                    { secondAxis, position.Y }
                };
                try { 
                Task.WaitAll(controller.Move(moves).ToArray(), caTok);
                }
                catch (OperationCanceledException ex) { return; }
                Thread.Sleep(PreMeasureDelay);

                for (int ii = 0; ii < MeasureDevices.Count; ++ii)
                {
                    switch (MeasureDevices[ii])
                    {
                        case Encoder enc:
                            double encPos = controller.GetEncoderFeedback(enc);
                            if (enc.SetZero)
                            {
                                enc.SetOffset(0, encPos);
                            }
                            else
                            {
                                enc.SetOffset(moves.Values.ElementAt(ii), encPos);
                            }
                            break;
                        case ExtEncoder enc:
                            encPos = enc.GetFeedback();
                            enc.SetOffset(moves.Values.ElementAt(ii), encPos);
                            break;
                        case ExtTrigger trig:
                            MessageBox.Show("Align external measurement device");
                            break;
                        case FeedbackSimulator fbSim:
                            break;
                        default:
                            throw new InvalidEnumArgumentException(MeasureDevices[ii].GetType().ToString());
                    }
                }


                while (!mp.IsFinished)
                {
                    position = mp.GetNextPosition();
                    moves = new Dictionary<Axis, double>
                    {
                        { firstAxis, position.X },
                        { secondAxis, position.Y }
                    };
                    try
                    {
                        Task.WaitAll(controller.Move(moves).ToArray(), caTok);
                    }
                    catch (OperationCanceledException ex) { return; }

                    
                    Thread.Sleep(PreMeasureDelay);

                    // report progress
                    string progMsg = $"Position {prog} of {totalProg}";
                    progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = progMsg});

                    // measure value
                    double[] measure = new double[MeasureDevices.Count];
                    for (int ii = 0; ii < MeasureDevices.Count; ++ii)
                    {
                        switch (MeasureDevices[ii])
                        {
                            case ExtTrigger trig:
                                controller.SendHWTrigger(trig);
                                trig.ShowMessage(mp.EndOfTravel);
                                break;
                            case Encoder enc:
                                measure[ii] = controller.GetEncoderFeedback(enc);
                                measure[ii] = enc.SetZero ? enc.GetFeedback(measure[ii]) : moves.Values.ElementAt(ii) - enc.GetFeedback(measure[ii]);
                                break;
                            case ExtEncoder enc:
                                measure[ii] = moves.Values.ElementAt(ii) - enc.GetFeedback();
                                break;
                            case FeedbackSimulator fbSim:
                                Thread.Sleep(100);
                                measure[ii] = fbSim.GetFeedback();
                                break;
                            default:
                                throw new InvalidEnumArgumentException(MeasureDevices[ii].GetType().ToString());
                        }
                    }
                    if(measure.Length == 2)
                    {
                        result.Add(new PositionDomain2D(position, new Point(measure[0], measure[1])));
                    }

                    if (mp.EndOfTravel && !blDone)
                    {
                        moves = new Dictionary<Axis, double>
                        {
                            { firstAxis, blStep.X * blSign },
                            { secondAxis, blStep.Y * blSign }
                        };
                        try { 
                        Task.WaitAll(controller.MoveAdditive(moves).ToArray(), caTok);
                        }
                        catch (OperationCanceledException ex) { return; }
                        blSign *= -1;
                        moves = new Dictionary<Axis, double>
                        {
                            { firstAxis, blStep.X * blSign },
                            { secondAxis, blStep.Y * blSign }
                        };
                        try { 
                        Task.WaitAll(controller.MoveAdditive(moves).ToArray(), caTok);
                        }
                        catch (OperationCanceledException ex) { return; }
                        blDone = true;
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
                    foreach (PositionDomain2D pd2D in result) // invert measure for mapping
                    {
                        pd2D.Measure = new Point(pd2D.Measure.X * -1, pd2D.Measure.Y * -1);
                    }

                    TriaMapping2D mapX = new TriaMapping2D(result, $"{base.Name}_X");
                    TriaMapping2D mapY = new TriaMapping2D(result, $"{base.Name}_Y", false);

                    uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}-X.TAMtbl");
                    mapX.Write(uniqueFN);
                    uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}-Y.TAMtbl");
                    mapY.Write(uniqueFN);
                }
                else
                {
                    uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}.csv");
                    base.WriteCSV(uniqueFN);
                }
            }, caTok);
        }
    }
}