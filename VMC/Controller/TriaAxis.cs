using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Triamec.Tam;
using Triamec.Tam.Registers;
using Triamec.Tam.Requests;
using Triamec.Tam.Rlid19;
using Triamec.TriaLink;
using VMC.Misc;

namespace VMC.Controller
{
    public enum AxisTopology { Default, Dualloop, Gantry }
    public class TriaAxis : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public AxisTopology Topology
        {
            get => topo;
            set
            {
                topo = value;
                OnPropertyChanged(nameof(Topology));
            }
        }
        public TamAxis Axis
        {
            get => ax;
            set
            {
                ax = value;
                axisIndex = ax.AxisIndex;
                reg = (Triamec.Tam.Rlid19.Axis)ax.Register;
                maxVel = reg.Parameters.PathPlanner.VelocityMaximum.Read();
                maxAcc = reg.Parameters.PathPlanner.AccelerationMaximum.Read();
                maxJrk = reg.Parameters.PathPlanner.JerkMaximum.Read();
                DynamicReductionFactor = reg.Parameters.PathPlanner.DynamicReductionFactor.Read();

                OnPropertyChanged(nameof(Axis));

                ax.Transition += UpdateState;
            }
        }

        public ITamDrive Drive => ax.Drive;

        public float MotorTemperature => reg.Signals.General.MotorTemperature.Read();

        public float DriveTemperature => reg.Signals.General.PowerStageTemperature.Read();

        public Triamec.Tam.Rlid19.Axis Register => reg;
        public double Position => reg.Signals.PositionController.MasterPosition.Read();
        public double ReferencePosition // unique parameter foreach machine and axis
        {
            get => reg.Commands.Homing.ReferencePosition.Read();
            set => reg.Commands.Homing.ReferencePosition.Write(value);
        }
        public float Velocity
        {
            get => vel;
            private set
            {
                if (value > 0)
                {
                    vel = value;
                    OnPropertyChanged(nameof(Velocity));
                }
            }
        }
        public float Acceleration
        {
            get => acc;
            private set
            {
                if (value > 0)
                {
                    acc = value;
                    OnPropertyChanged(nameof(Acceleration));
                }

            }
        }
        public float Jerk
        {
            get => jrk;
            private set
            {
                if (value > 0)
                {
                    jrk = value;
                    OnPropertyChanged(nameof(Jerk));
                }
            }
        }
        public float DynamicReductionFactor
        {
            get => drf;
            set
            {
                if (value <= 0 || value > 1) return;

                drf = value;
                Velocity = GetVel(maxVel, drf);
                Acceleration = GetAcc(maxAcc, drf);
                Jerk = GetJerk(maxJrk, drf);
                reg.Parameters.PathPlanner.DynamicReductionFactor.Write(drf);
                reg.Parameters.PathPlanner.Commit();
                OnPropertyChanged(nameof(DynamicReductionFactor));
            }
        }
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }
        public string StateAndErrorInfo
        {
            get => staErrInfo;
            set
            {
                staErrInfo = value;
                OnPropertyChanged(nameof(StateAndErrorInfo));
            }
        }

        private AxisTopology topo;
        private Triamec.Tam.Rlid19.Axis reg;
        private TamAxis ax;
        private int axisIndex;
        private AxisState state;
        private AxisErrorIdentification error;
        private string staErrInfo;

        private float vel;
        private float maxVel;
        private float acc;
        private float maxAcc;
        private float jrk;
        private float maxJrk;
        private float drf;
        private bool enabled;
        private static readonly TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
        private static readonly Dictionary<Type, int> appParamNumVariables = new Dictionary<Type, int> // declares number of used variables for one axis by type
        {
            { typeof(double), 0 },
            { typeof(float), 2 },
            { typeof(int), 0 },
            { typeof(bool), 0 }
        };

        public TriaAxis(AxisTopology topology)
        {
            topo = topology;
            vel = 0;
            maxVel = 0;
            acc = 0;
            maxAcc = 0;
            jrk = 0;
            maxJrk = 0;
            drf = 0;
            state = AxisState.Disabled;
            error = AxisErrorIdentification.None;
            enabled = false;
            staErrInfo = GetStateAndErrorInfo(state, error);
        }

        public Task<bool> Enable(IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                const int maxTries = 10; // max tries for first and second loop
                int ii; // loop variable, also for reporting progress

                TamRequest requ = null;
                ITamDrive drive = ax.Drive;
                DeviceState drState;

                if (error != AxisErrorIdentification.None)
                {
                    requ = ax.Control(AxisControlCommands.ResetError);
                    requ.WaitForTermination();
                }

                for (ii = 0; ii <= maxTries; ii++) // try to get a defined state
                {
                    drState = drive.ReadDeviceState();
                    switch (drState)
                    {
                        case DeviceState.FaultPending:
                            Thread.Sleep(timeout);
                            break;
                        case DeviceState.FaultReactionActive:
                            requ = drive.ResetFault();
                            requ.WaitForSuccess(timeout);
                            break;
                        case DeviceState.NotReadyToSwitchOn:
                            Thread.Sleep(timeout);
                            break;
                        case DeviceState.Operational:
                            ii = maxTries;
                            break;
                        case DeviceState.ReadyToSwitchOn:
                            requ = drive.SwitchOn();
                            requ.WaitForSuccess(timeout);
                            break;
                        case DeviceState.StartUp:
                            requ = drive.SwitchOn();
                            requ.WaitForSuccess(timeout);
                            break;
                        default:
                            throw new InvalidEnumArgumentException(drState.ToString());
                    }

                    progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = maxTries * 2, Message = $"{ax.DisplayName} {drState}" });
                    caTok.ThrowIfCancellationRequested();
                }

                drState = drive.ReadDeviceState();
                if (drState != DeviceState.Operational) return false; // abord if drive have not reached right state
                AxisState axState;

                for (ii = maxTries; ii <= maxTries * 2; ii++) // try to get a defined state
                {
                    axState = ax.ReadAxisState();
                    switch (axState)
                    {
                        case AxisState.ContinuousMotion:
                            requ = ax.Stop();
                            requ.WaitForSuccess(timeout);
                            break;
                        case AxisState.Disabled:
                            requ = ax.Control(AxisControlCommands.Enable);
                            requ.WaitForSuccess(timeout);
                            break;
                        case AxisState.Disabling:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.DiscreteMotion:
                            requ = ax.Stop();
                            requ.WaitForSuccess(timeout);
                            break;
                        case AxisState.Enabling:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.ErrorStopping:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.Standstill:
                            ii = maxTries * 2;
                            break;
                        case AxisState.Startup:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.Stopping:
                            Thread.Sleep(timeout);
                            break;
                        default:
                            throw new InvalidEnumArgumentException(axState.ToString());
                    }

                    progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = maxTries * 2, Message = $"{ax.DisplayName} {axState}" });
                    caTok.ThrowIfCancellationRequested();
                }

                axState = ax.ReadAxisState();
                return axState == AxisState.Standstill;
            }, caTok);
        }

        public Task<bool> Disable(IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                const int maxTries = 10; // max tries for first and second loop
                int ii; // loop variable, also for reporting progress

                TamRequest requ = null;
                AxisState axState;


                for (ii = 0; ii <= maxTries; ii++) // try 100 times to get a defined state
                {
                    axState = ax.ReadAxisState();
                    switch (axState)
                    {
                        case AxisState.ContinuousMotion:
                            requ = ax.Stop();
                            requ.WaitForSuccess(timeout);
                            break;
                        case AxisState.Disabled:
                            ii = maxTries;
                            break;
                        case AxisState.Disabling:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.DiscreteMotion:
                            requ = ax.Stop();
                            requ.WaitForSuccess(timeout);
                            break;
                        case AxisState.Enabling:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.ErrorStopping:
                            Thread.Sleep(timeout);
                            break;
                        case AxisState.Standstill:
                            requ = ax.Control(AxisControlCommands.Disable);
                            requ.WaitForSuccess(timeout);
                            break;
                        case AxisState.Startup:
                            ii = maxTries;
                            break;
                        case AxisState.Stopping:
                            Thread.Sleep(timeout);
                            break;
                        default:
                            throw new InvalidEnumArgumentException(axState.ToString());
                    }

                    progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = maxTries, Message = $"{ax.DisplayName} {axState}" });
                    caTok.ThrowIfCancellationRequested();
                }

                ITamDrive drive = ax.Drive;
                DeviceState drState;

                for (ii = maxTries; ii <= maxTries * 2; ii++) // try 100 times to get a defined state
                {
                    drState = drive.ReadDeviceState();

                    switch (drState)
                    {
                        case DeviceState.FaultPending:
                            requ = drive.ResetFault();
                            requ.WaitForSuccess(timeout);
                            break;
                        case DeviceState.FaultReactionActive:
                            Thread.Sleep(timeout);
                            break;
                        case DeviceState.NotReadyToSwitchOn:
                            Thread.Sleep(timeout);
                            break;
                        case DeviceState.Operational:
                            //requ = drive.SwitchOff();
                            //requ.WaitForSuccess(timeout);
                            ii = maxTries * 2;
                            break;
                        case DeviceState.ReadyToSwitchOn:
                            ii = maxTries * 2;
                            break;
                        case DeviceState.StartUp:
                            ii = maxTries * 2;
                            break;
                        default:
                            throw new InvalidEnumArgumentException(drState.ToString());
                    }
                    
                    progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = maxTries * 2, Message = $"{ax.DisplayName} {drState}" });
                    caTok.ThrowIfCancellationRequested();
                }

                axState = ax.ReadAxisState();
                return axState == AxisState.Startup;
            }, caTok);
        }

        public Task<bool> Home(TimeSpan maxHomingTime, IProgress<TaskProgReport> progress, CancellationToken caTok)
        {
            return Task.Run(() =>
            {
                bool success = false;
                if (state != AxisState.Standstill) // initial condition fullfiled?
                {
                    return success;
                }

                reg.Commands.PositionController.ReferenceDone.Write(false);

                switch (topo)
                {
                    case AxisTopology.Gantry:
                        ITamNode node = ax.FindTamNode(new Uri("tam://mc/XGantry/Application/TamaControl/IsochronousMainCommand", UriKind.Absolute));
                        ITamRegister<int> isoMainCommand = (ITamRegister<int>)node;
                        isoMainCommand.Write(0);
                        Thread.Sleep(timeout);
                        isoMainCommand.Write(2);
                        break;
                    default:
                        reg.Commands.Homing.Command.Write(HomingCommand.Start);
                        break;
                }
                

                const int maxTries = 20;
                double timeFactor = 1 / drf;
                TimeSpan homingTimeStep = TimeSpan.FromMilliseconds((maxHomingTime.TotalMilliseconds * timeFactor) / maxTries);
                for (int ii = 0; ii <= maxTries; ii++)
                {
                    if (reg.Commands.PositionController.ReferenceDone.Read())
                    {
                        ii = maxTries;
                        success = true;
                    }
                    else Thread.Sleep(homingTimeStep);

                    progress.Report(new TaskProgReport { CurrentProgess = ii, TotalProgess = maxTries, Message = $"{ax.DisplayName} {state}" });
                    if (caTok.IsCancellationRequested)
                    {
                        ax.Stop();
                        caTok.ThrowIfCancellationRequested();
                    }
                }

                switch (topo)
                {
                    case AxisTopology.Gantry:
                        ITamNode node = ax.FindTamNode(new Uri("tam://mc/XGantry/Application/TamaControl/IsochronousMainCommand", UriKind.Absolute));
                        ITamRegister<int> isoMainCommand = (ITamRegister<int>)node;
                        int mainCommand = isoMainCommand.Read();

                        return (success && mainCommand == 4); // check if yaw mapping is active
                    default:
                        return success;
                }
                

            }, caTok);
        }

        public void SetSettlingParam(float settlingWindow, float minSettlingTime)
        {
            Register topReg = (Register)ax.Drive.Register;
            topReg.Application.Parameters.Floats[GetAppParamRegisterIndex(0, typeof(float))].Write(settlingWindow);
            topReg.Application.Parameters.Floats[GetAppParamRegisterIndex(1, typeof(float))].Write(minSettlingTime);
        }

        private int GetAppParamRegisterIndex(int index, Type varType)
        {
            return axisIndex * appParamNumVariables[varType] + index;
        }

        public Task<float> Move(double pos)
        {
            return Task.Run(() =>
            {
                return WaitOnMoveTermination(ax.MoveAbsolute(pos, PathPlannerDirection.Shortest));
            });
        }

        public Task<float> MoveAdditive(double distance)
        {
            return Task.Run(() =>
            {
                TamRequest move = ax.MoveAdditive(distance);
                return WaitOnMoveTermination(move);
            });
        }

        // returns motion time of move in seconds
        private float WaitOnMoveTermination(TamRequest request)
        {
            request.WaitForTermination();
            if (request.Termination != TamRequestResolution.Completed)
            {
                throw new Exception($"Tamrequest unsuccessfull! {ax.DisplayName} {StateAndErrorInfo}");
            }

            Register topReg = (Register)ax.Drive.Register;

            while (!topReg.Application.Variables.Booleans[axisIndex].Read()) { } // wait on flag which indicates "is in position"
            return topReg.Application.Variables.Floats[axisIndex].Read();
        }

        private void UpdateState(object sender, StateTransition transistion)
        {
            error = transistion.Errors.GetAxisError(axisIndex);
            state = transistion.States.GetAxisState(axisIndex);

            Enabled = !(state == AxisState.Disabled || state == AxisState.Startup || state == AxisState.ErrorStopping);
            StateAndErrorInfo = GetStateAndErrorInfo(state, error);
        }

        private static string GetStateAndErrorInfo(AxisState axState, AxisErrorIdentification axError)
        {
            return $"State: {axState} / Errors: {axError}";
        }

        private static float GetVel(float maxVelocity, float dynamicReductionFactor)
        {
            return maxVelocity * dynamicReductionFactor;
        }

        private static float GetAcc(float maxAcceleration, float dynamicReductionFactor)
        {
            return maxAcceleration * (float)Math.Pow(dynamicReductionFactor, 2);
        }

        private static float GetJerk(float maxJerk, float dynamicReductionFactor)
        {
            return maxJerk * (float)Math.Pow(dynamicReductionFactor, 3);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
