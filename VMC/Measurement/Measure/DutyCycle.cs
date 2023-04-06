using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using VMC.Misc;
using VMC.Controller;
using System.Linq;
using System.Diagnostics;

namespace VMC.Measurement
{
    public struct CycleData
    {
        public CycleData(Axis axis, Procedure<double> procedure, TimeSpan cycleTime)
        {
            Axis = axis;
            Procedure = procedure;
            CycleTime = cycleTime;
        }

        public Axis Axis;
        public Procedure<double> Procedure;
        public TimeSpan CycleTime;
    }

    public class DutyCycle : Measure<TimeDomain>, IMeasure
    {

        private readonly CycleData[] data;
        private readonly TimeSpan dur;
        private readonly TimeSpan sTime;

        public DutyCycle(string name, CycleData[] cycleData, TimeSpan duration, TimeSpan samplingTime) : base(name)
        {
            data = cycleData;
            dur = duration;
            sTime = samplingTime;

            List<string> header = new List<string>
            {
                "Date",
                "Time"
            };

            foreach (CycleData cyDa in data)
            {
                header.Add(cyDa.Axis.ToString() + "-DriveTemp");
                header.Add(cyDa.Axis.ToString() + "-MotorTemp");
            }
            DataHeader = string.Join(";", header);
        }


        public Task Measure(string directory, TriaController controller, IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                DateTime startTime = DateTime.Now;
                string uniqueFN = GetUniqueFilename($"{directory}\\{base.Name}.csv");

                result.Clear();
                MetaData.Clear();

                DateTime endTime = startTime + dur;

                List<Task> tasks = new List<Task>();

                CancellationTokenSource caToSou = new CancellationTokenSource();
                CancellationToken _ = caToSou.Token;
                // start duty cycle for each axis
                foreach (CycleData cyDa in data)
                {
                    Task ta = RunDC(controller.GetAxis(cyDa.Axis), cyDa.Procedure, cyDa.CycleTime, _);
                    tasks.Add(ta);
                }
                Task samplingTask = RunSampling(sTime, data, controller, progress, caTok);

                try
                {
                    samplingTask.Wait(caTok);
                }
                catch
                {
                    caToSou.Cancel();
                    return;
                }

                caToSou.Cancel();

                TimeSpan measureTime = DateTime.Now - startTime;

                // add metadata from measurement
                MetaData.Add(new MetaData("Duration", measureTime.ToString(durationFormat)));
                MetaData.Add(new MetaData("Date", DateTime.Now.ToString(dateFormat)));
                MetaData.Add(new MetaData("EndTime", DateTime.Now.ToString(timeFormat)));

                WriteCSV(uniqueFN);

            }, caTok);
        }

        private Task RunDC(TriaAxis axis, Procedure<double> procedure, TimeSpan cycleTime, CancellationToken caTok)
        {
            Task ta = Task.Run(async () =>
            {

                DateTime endTime;
                while (true)
                {
                    endTime = DateTime.Now + cycleTime;

                    Task _ = axis.Move(procedure.GetNextPosition());
                    _.Wait(caTok);
                    while (DateTime.Now < endTime) {
                        if (caTok.IsCancellationRequested) { break; }
                    };
                }
            }, caTok);
            return ta;
        }

        public Task RunSampling(TimeSpan samplingTime, CycleData[] cyData, TriaController controller, IProgress<TaskProgReport> progress, CancellationToken caTok) { 

            Task ta = Task.Run(async () =>
            {

                int totalProg = (int)(dur.Ticks / sTime.Ticks); // number calls of "progess.Report(...);" in code of this method
                DateTime startTime = DateTime.Now;

                for (int prog = 0; prog <= totalProg; ++prog)
                {
                    List<float> temperatures = new List<float>();
                    foreach (CycleData cyDa in data) // capture temperatures
                    {
                        temperatures.Add(controller.GetAxis(cyDa.Axis).DriveTemperature);
                        temperatures.Add(controller.GetAxis(cyDa.Axis).MotorTemperature);
                    }
                    result.Add(new TimeDomain(DateTime.Now, temperatures.ToArray()));

                    while (DateTime.Now.Ticks < (prog * sTime.Ticks + startTime.Ticks)) // wait till next measurement can be captured
                    {}

                    progress.Report(new TaskProgReport { CurrentProgess = prog, TotalProgess = totalProg, Message = $"Sample {prog} of {totalProg}" });
                }
            }, caTok);
            return ta;
        }
}
}
