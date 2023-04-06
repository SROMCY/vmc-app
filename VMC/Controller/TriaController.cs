using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Triamec.Tam;
using Triamec.Tam.Registers;
using Triamec.Tam.Configuration;
using Triamec.Tam.UI;
using VMC.Misc;
using VMC.Measurement;


namespace VMC.Controller
{
    public enum Axis { X_Linear, X_Rotary, Y, Z, T };
    public enum Drive { XGantry, YTheta, ZDualloop }
    public class TriaController : IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public TriaAxis AxisX
        {
            get
            {
                if (axes.TryGetValue(Axis.X_Linear, out TriaAxis ax))
                {
                    return ax;
                }
                return null;
            }
            private set
            {
                axes[Axis.X_Linear] = value;
                OnPropertyChanged(nameof(AxisX));
            }
        }
        public TriaAxis AxisY
        {
            get
            {
                if (axes.TryGetValue(Axis.Y, out TriaAxis ax))
                {
                    return ax;
                }
                return null;
            }
            private set
            {
                axes[Axis.Y] = value;
                OnPropertyChanged(nameof(AxisY));
            }
        }
        public TriaAxis AxisZ
        {
            get
            {
                if (axes.TryGetValue(Axis.Z, out TriaAxis ax))
                {
                    return ax;
                }
                return null;
            }
            set
            {
                axes[Axis.Z] = value;
                OnPropertyChanged(nameof(AxisZ));
            }
        }
        public TriaAxis AxisT
        {
            get
            {
                if (axes.TryGetValue(Axis.T, out TriaAxis ax))
                {
                    return ax;
                }
                return null;
            }
            set
            {
                axes[Axis.T] = value;
                OnPropertyChanged(nameof(AxisT));
            }
        }
        public bool IsConnected
        {
            get => isConnected;
            private set
            {
                isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
        public bool IsNotBusy => !isBusy;
        public bool DampingIsReady
        {
            get => dampingIsReady;
            private set
            {
                dampingIsReady = value;
                OnPropertyChanged(nameof(DampingIsReady));
            }
        }
        public bool DampingHasError
        {
            get => dampingHasError;
            private set
            {
                dampingHasError = value;
                OnPropertyChanged(nameof(DampingHasError));
            }
        }
        public bool PowerSupplyOK
        {
            get => psOK;
            private set
            {
                psOK = value;
                OnPropertyChanged(nameof(PowerSupplyOK));
                if (!psOK) MessageBox.Show("Warning: State input from Triamec Power-Supply missing!");
            }
        }
        public CancellationTokenSource caToSou;

        private bool isConnected; // true if connection was successfully estabished
        private bool isBusy;      // true if some action is in charge
        private bool dampingIsReady;     // true if I/O of damping system is high
        private bool dampingHasError;    // true if I/O of damping system is high
        private bool psOK;    // true if I/O of triamec power supply is high

        private TamTopology topology;
        private TamSystem system;
        private TamLink link;

        private TriaInputSubscription dampingSystemIO;
        private TriaInputSubscription powerSupplyIO;
        private readonly Dictionary<Drive, ITamDrive> drives;
        private readonly Dictionary<Axis, TriaAxis> axes;

        public TriaController()
        {
            isConnected = false;
            isBusy = false;
            dampingIsReady = false;
            dampingHasError = false;
            psOK = true;
            drives = new Dictionary<Drive, ITamDrive>();
            axes = new Dictionary<Axis, TriaAxis>()
            {
                { Axis.X_Linear, new TriaAxis(AxisTopology.Gantry) },
                { Axis.X_Rotary, new TriaAxis(AxisTopology.Gantry) },
                { Axis.Y, new TriaAxis(AxisTopology.Default) },
                { Axis.Z, new TriaAxis(AxisTopology.Dualloop) },
                { Axis.T, new TriaAxis(AxisTopology.Default) }
            };
        }

        public TriaAxis GetAxis(Axis axis)
        {
            return axes[axis];
        }

        public void Cancel()
        {
            if (caToSou != null)
            {
                if (!caToSou.IsCancellationRequested)
                    caToSou.Cancel();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (isBusy)
            {
                Cancel();
            }

            while (isBusy) { } // wait till all tasks are finished

            if (dampingSystemIO != null) dampingSystemIO.Dispose(); // unsubscribe from input listener of damping system
            if (powerSupplyIO != null) powerSupplyIO.Dispose();

            /*
            foreach (Axis ax in Enum.GetValues(typeof(Axis)))
            {
                if (axes[ax].Enabled)
                {
                    Progress<TaskProgReport> prog = new Progress<TaskProgReport>();
                    DisableAsync(ax, prog);
                    while (axes[ax].Enabled) { }
                }
            }
            */

            foreach (KeyValuePair<Drive, ITamDrive> dr in drives)
            {
                dr.Value.RemoveStateObserver(this);
            }

            drives.Clear();
            if (system != null) system.Dispose();
            if (topology != null) topology.Dispose();

            IsConnected = false;
        }

        public async void ConnectAsync(IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            caToSou = new CancellationTokenSource();
            Task ta = null;

            try
            {
                ta = Connect(progress, caToSou.Token);
                await ta;
                IsConnected = true;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message);
                IsConnected = false;
                Dispose();
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }

        private Task Connect(IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                int prog = 0; // progress variable
                const int totalProg = 10; // number calls of "progess.Report(...);" in code of this method

                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = "Initialize topology" });
                topology = new TamTopology(null);

                system = topology.AddLocalSystem(); // Add the local TAM system on this PC to the topology.

                system.Identify(); // boot the Tria-Link so that it learns about connected stations.
                link = system[0][0]; // get the 1st link on the 1st adapter
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = "Initialize link" });
                link.Identify(); // this step can take several seconds

                caTok.ThrowIfCancellationRequested();

                foreach (Axis ax in Enum.GetValues(typeof(Axis)))
                {   
                    TamAxis tamAx = system.AsDepthFirstLeaves<TamAxis>().FirstOrDefault(a => a.Name == ax.ToString());
                    if (tamAx == null) throw new TamException($"Axis not found: {ax}");
                    else
                    {
                        axes[ax].Axis = tamAx;
                    }
                    progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = $"Axis found: {ax}"});

                    caTok.ThrowIfCancellationRequested();
                }

                drives.Add(Drive.XGantry, axes[Axis.X_Linear].Drive);
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = $"Drive found: {Drive.XGantry}" });
                drives.Add(Drive.YTheta, axes[Axis.Y].Drive);
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = $"Drive found: {Drive.YTheta}" });
                drives.Add(Drive.ZDualloop, axes[Axis.Z].Drive);
                progress.Report(new TaskProgReport { CurrentProgess = ++prog, TotalProgess = totalProg, Message = $"Drive found: {Drive.ZDualloop}" });

                // Add state observer to drive
                foreach (Drive dr in Enum.GetValues(typeof(Drive)))
                {
                    _ = drives[dr].AddStateObserver(this);
                }

                dampingSystemIO = new TriaInputSubscription(axes[Axis.T].Register.Signals.General.DigitalInputBits); // capture inputs of damping system
                dampingSystemIO.RegisterChanged += DampingSystemIO_RegisterChanged;
                powerSupplyIO = new TriaInputSubscription(axes[Axis.X_Linear].Register.Signals.General.DigitalInputBits); // capture inputs of triamec power supply
                powerSupplyIO.RegisterChanged += PowerSupplyIO_RegisterChanged;

                IsConnected = true;
                IsBusy = false;
            }, caTok);
        }

        public async void EnableAsync(Axis axis, IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            caToSou = new CancellationTokenSource();
            CancellationToken caTok = caToSou.Token;
            Task<bool> ta = null;

            try
            {
                ta = axes[axis].Enable(progress, caTok);
                await ta;
                //_ = MessageBox.Show($"Enabling failed of {axis}");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message);
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }

        public async void DisableAsync(Axis axis, IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            caToSou = new CancellationTokenSource();
            CancellationToken caTok = caToSou.Token;
            Task<bool> ta = null;

            try
            {
                ta = axes[axis].Disable(progress, caTok);
                await ta;
                //_ = MessageBox.Show($"Disabling failed of {axis}");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message);
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }

        public async void HomeAsync(Dictionary<Axis, TimeSpan> toHome, IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            caToSou = new CancellationTokenSource();
            CancellationToken caTok = caToSou.Token;
            List<Task<bool>> tasks = new List<Task<bool>>();

            // try homing of dedicated axes ( Axis.X_Linear, Axis.Y, Axis.T, Axis.Z )
            foreach (KeyValuePair<Axis, TimeSpan> homeAxis in toHome)
            {
                tasks.Add(axes[homeAxis.Key].Home(homeAxis.Value, progress, caTok));
            }

            foreach (Task<bool> ta in tasks)
            {
                try
                {
                    if (!await ta)
                        _ = MessageBox.Show($"Homing failed of {toHome.Keys.ToList()[tasks.IndexOf(ta)]}");
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message);
                }
                finally
                {
                    ta.Dispose();
                }
            }
            IsBusy = false;
        }

        public bool Load(string directory)
        {
            return LoadSurveyor.Load(directory, topology, true, null);
        }

        public void Persist()
        {
            foreach (Drive dr in Enum.GetValues(typeof(Drive)))
            {
                Triamec.Tam.Rlid19.Register reg = (Triamec.Tam.Rlid19.Register)drives[dr].Register;
                reg.General.Commands.ParameterManagement.Write(Triamec.Tam.Rlid19.ParameterManagementCommand.SaveParametersPermanently);
            }
        }

        public SaveDialog SaveConfig()
        {
            TamSerializer serializer = new TamSerializer(true, topology);
            return new SaveDialog(serializer);
        }

        #region DampingSystem
        public async void ResetDamping(int millisecondsTimeout, IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            caToSou = new CancellationTokenSource();
            CancellationToken caTok = caToSou.Token;
            Task ta = null;
            TimeSpan cycleTime = TimeSpan.FromMilliseconds(millisecondsTimeout / 30);
            int totalCycles = millisecondsTimeout / cycleTime.Milliseconds;

            try
            {
                ta = Task.Run(() =>
                {
                    axes[Axis.T].Register.Commands.General.DigitalOut2.Write(true);  // set IDE damping system reset input

                    for (int ii = 0; ii <= totalCycles; ii++)                                 // wait at least 4s befor clearing reset input (according Opticon 1000 Digital I/O document)
                    {
                        Thread.Sleep(cycleTime);
                        progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = totalCycles, Message = $"Reseting Damping System: {(ii * cycleTime.Milliseconds) / 1000}s" });
                        if (caTok.IsCancellationRequested)
                        {
                            IsBusy = false;
                            axes[Axis.T].Register.Commands.General.DigitalOut2.Write(false);
                            caTok.ThrowIfCancellationRequested();
                        }
                    }
                    axes[Axis.T].Register.Commands.General.DigitalOut2.Write(false);
                }, caTok);

                await ta;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }

        public async void SetDamping(bool active, int millisecondsTimeout, IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            caToSou = new CancellationTokenSource();
            CancellationToken caTok = caToSou.Token;
            Task ta = null;
            TimeSpan cycleTime = TimeSpan.FromMilliseconds(millisecondsTimeout / 30);
            int totalCycles = millisecondsTimeout / cycleTime.Milliseconds;

            try
            {
                ta = Task.Run(() =>
                {
                    axes[Axis.T].Register.Commands.General.DigitalOut1.Write(true); // set IDE damping system enable input

                    for (int ii = 0; ii <= totalCycles; ii++)                       // wait at least 2s befor exit (according Opticon 1000 Digital I/O document)
                    {
                        if (active == dampingIsReady)
                        {
                            ii = totalCycles;
                        }

                        progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = totalCycles, Message = $"Set Damping System: {(ii * cycleTime.Milliseconds) / 1000}s" });
                        Thread.Sleep(cycleTime);
                    }
                }, caTok);

                await ta;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                axes[Axis.T].Register.Commands.General.DigitalOut1.Write(false); // reset IDE damping system enable input
                ta.Dispose();
                IsBusy = false;
            }
        }

        private void DampingSystemIO_RegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            int inputBit1 = BitVector32.CreateMask();
            int inputBit2 = BitVector32.CreateMask(inputBit1);
            DampingIsReady = e.GetBit(inputBit1);
            DampingHasError = e.GetBit(inputBit2);
        }

        #endregion
        #region PowerSupply
        private void PowerSupplyIO_RegisterChanged(object sender, RegisterChangedEventArgs e) => PowerSupplyOK = e.GetBit(BitVector32.CreateMask()); // update status of damping system

        #endregion
        #region CoordinateSystem
        public Coordinate GetCoordinate()
        {
            if (IsConnected)
            {
                return new Coordinate()
                {
                    X = axes[Axis.X_Linear].Position,
                    Y = axes[Axis.Y].Position,
                    Z = axes[Axis.Z].Position,
                    T = axes[Axis.T].Position,
                };
            }
            else
                return new Coordinate();
        }

        public Coordinate GetReferenceCoordinate()
        {
            Coordinate refCoord = new Coordinate();
            foreach (Axis ax in Enum.GetValues(typeof(Axis)))
            {
                double refPos = axes[ax].ReferencePosition;
                refCoord.SetPosition(ax, refPos);
            }
            return refCoord;
        }

        public void SetReferencePosition(double offset, Axis ax)
        {
            axes[ax].ReferencePosition = offset;
        }
        #endregion
        #region Measurement

        public async void MeasureAsync(IMeasure measure, string directory, IProgress<TaskProgReport> progress)
        {
            IsBusy = true;
            Task ta = null;
            caToSou = new CancellationTokenSource();
            CancellationToken caTok = caToSou.Token;

            try
            {
                ta = measure.Measure(directory, this, progress, caTok);
                await ta;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }

        public void SetSettlingParam(Axis axis, float settlingWindow, float minSettlingTime)
        {
            axes[axis].SetSettlingParam(settlingWindow, minSettlingTime);
        }

        public void SendHWTrigger(ExtTrigger trigger)
        {
            axes[Axis.X_Rotary].Register.Commands.General.DigitalOut2.Write(true); // set indication output for start of this sequence
            trigger.WaitPreDelay(); // wait on moving average of measurement signal from external device
            axes[Axis.X_Rotary].Register.Commands.General.DigitalOut1.Write(true); // set trigger output
            trigger.GetFeedback(); // set pulse
            axes[Axis.X_Rotary].Register.Commands.General.DigitalOut1.Write(false); // reset output
            axes[Axis.X_Rotary].Register.Commands.General.DigitalOut2.Write(false); // reset output
            trigger.WaitPostDelay(); // wait before next move
        }

        public double GetEncoderFeedback(Encoder encoder)
        {
            ITamNode node = topology.FindTamNode(encoder.RegisterPath);
            ITamReadonlyRegister<double> reg = (ITamReadonlyRegister<double>)node;
            return reg.Read();
        }

        public double[] GetEncoderFeedback(Encoder[] encoders)
        {
            List<double> feedback = new List<double>();
            foreach (Encoder enc in encoders)
            {
                feedback.Add(GetEncoderFeedback(enc));
            }

            return feedback.ToArray();
        }
        #endregion
        #region Motion
        public Task<float> Move(Axis axis, double position)
        {
            return axes[axis].Move(position);
        }

        public List<Task<float>> Move(Dictionary<Axis, double> moves)
        {
            List<Task<float>> tasks = new List<Task<float>>();
            foreach (KeyValuePair<Axis, double> move in moves)
            {
                tasks.Add(Move(move.Key, move.Value));
            }
            return tasks;
        }

        public async void MoveAsync(Axis axis, double position)
        {
            Task<float> ta = null;
            try
            {
                ta = Move(axis, position);
                _ = await ta;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message);
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }

        public void MoveAsync(Dictionary<Axis, double> moves)
        {
            foreach (KeyValuePair<Axis, double> move in moves)
            {
                MoveAsync(move.Key, move.Value);
            }
        }

        public void MoveAsync(Coordinate coordinate)
        {
            MoveAsync(Axis.X_Linear, coordinate.X);
            MoveAsync(Axis.Y, coordinate.Y);
            MoveAsync(Axis.Z, coordinate.Z);
            MoveAsync(Axis.T, coordinate.T);
        }

        public Task<float> MoveAdditive(Axis axis, double step)
        {
            return axes[axis].MoveAdditive(step);
        }

        public List<Task<float>> MoveAdditive(Dictionary<Axis, double> moves)
        {
            List<Task<float>> tasks = new List<Task<float>>();
            foreach (KeyValuePair<Axis, double> move in moves)
            {
                tasks.Add(MoveAdditive(move.Key, move.Value));
            }
            return tasks;
        }

        public async void MoveAdditiveAsync(Axis axis, double distance)
        {
            Task<float> ta = null;
            try
            {
                ta = MoveAdditive(axis, distance);
                _ = await ta;
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message);
            }
            finally
            {
                ta.Dispose();
                IsBusy = false;
            }
        }
        #endregion
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
