using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Triamec.Tam.UI;
using VMC.Misc;
using VMC.Measurement;
using VMC.Controller;
using Microsoft.Win32;
using System.Net.Sockets;
using System.Net;
using System.Windows.Markup;
using System.Threading.Tasks;
using System.Threading;
using ACS.SPiiPlusNET;
using Axis = VMC.Controller.Axis;
using Triamec.TriaLink;

namespace VMC
{
    /// <summary>
    /// Logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region PropertiesForBinding
        public event PropertyChangedEventHandler PropertyChanged;
        public double RelativeStepXY
        {
            get { return relStepXY; }
            set
            {
                if (value > 0)
                {
                    relStepXY = value;
                    OnPropertyChanged(nameof(RelativeStepXY));
                }
            }
        }

        public double RelativeStepZ
        {
            get { return relStepZ; }
            set
            {
                if (value > 0)
                {
                    relStepZ = value;
                    OnPropertyChanged(nameof(RelativeStepZ));
                }
            }
        }

        public double RelativeStepT
        {
            get { return relStepT; }
            set
            {
                if (value > 0)
                {
                    relStepT = value;
                    OnPropertyChanged(nameof(RelativeStepT));
                }
            }
        }


        public TriaController Controller
        {
            get { return controller; }
            set
            {
                controller = value;
                OnPropertyChanged(nameof(Controller));
            }
        }

        public Api ACSController
        {
            get { return acs_controller; }
            set
            {
                acs_controller = value;
                OnPropertyChanged(nameof(ACSController));

            }
        }

        private double relStepXY;
        private double relStepZ;
        private double relStepT;

        #endregion
        private enum ConState { NotConnected, Connecting, Connected }

        public TriaController controller;
        public Api acs_controller;
        public DispatcherTimer timer;
        public CancellationTokenSource caToSou;
        private string resultDir;


        public MainWindow()
        {
            InitializeComponent();

            // initialize variables

            string ip = "10.0.0.100";
            int port = 701;
            acs_controller = new Api();
            //acs_controller.OpenCommEthernet(ip, port);
            controller = new TriaController();
            DataContext = controller; // for data binding

            relStepXY = 50;
            relStepZ = 1;
            relStepT = 90;

            SetBinding(); // set binding for relative step functionality

            PrepareMeasurement(); // setup of available measurements

            resultDir = App.GetFolder("Result"); // get initial folder path

        }

        private void SetBinding()
        {
            Binding bdgXY = new Binding(nameof(RelativeStepXY))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtRelativeXY.SetBinding(TextBox.TextProperty, bdgXY);
            OnPropertyChanged(nameof(RelativeStepXY));

            Binding bdgZ = new Binding(nameof(RelativeStepZ))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtRelativeZ.SetBinding(TextBox.TextProperty, bdgZ);
            OnPropertyChanged(nameof(RelativeStepZ));

            Binding bdgT = new Binding(nameof(RelativeStepT))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtRelativeT.SetBinding(TextBox.TextProperty, bdgT);
            OnPropertyChanged(nameof(RelativeStepT));
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (controller.IsConnected)
            {
                controller.Cancel();
                while (controller.IsBusy); // wait till controllerTask is finished
                controller.Dispose();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Coordinate actCoord = controller.GetCoordinate();
            TbPosX.Text = actCoord.X.ToString("F3");
            TbPosY.Text = actCoord.Y.ToString("F3");
            TbPosZ.Text = actCoord.Z.ToString("F3");
            TbPosT.Text = actCoord.T.ToString("F3");

            ExtEncoder a0 = new ExtEncoder(client: acs_controller, axis_num: 0);
            ExtEncoder a1 = new ExtEncoder(client: acs_controller, axis_num: 1);
            ExtEncoder a2 = new ExtEncoder(client: acs_controller, axis_num: 4);
            TbPosA0.Text = a0.GetFeedback().ToString("F4");
            TbPosA1.Text = a1.GetFeedback().ToString("F4");
            TbPosA2.Text = a2.GetFeedback().ToString("F4");


            Triamec.Tam.TamAxis a = controller.AxisX.Axis;
            if (a != null)
            {
                AxisErrorIdentification error = a.ReadAxisError();
                if (error != AxisErrorIdentification.None)
                {
                    MessageBox.Show($"An error occured: {error} on axis {a.DisplayName}");
                    Triamec.Tam.Requests.TamRequest requ = a.Control(AxisControlCommands.ResetError);
                    requ.WaitForTermination();
                    
                }
                AxisState s = a.ReadAxisState();
                if (s == AxisState.Disabled)
                {
                    controller.AxisX.Enabled = false;
                }
                else
                {
                    controller.AxisX.Enabled = true;
                }
            }
            a = controller.AxisY.Axis;
            if (a != null)
            {
                AxisErrorIdentification error = a.ReadAxisError();
                if (error != AxisErrorIdentification.None)
                {
                    MessageBox.Show($"An error occured: {error} on axis {a.DisplayName}");
                    Triamec.Tam.Requests.TamRequest requ = a.Control(AxisControlCommands.ResetError);
                    requ.WaitForTermination();

                }
                if (a.ReadAxisState() == AxisState.Disabled)
                {
                    controller.AxisY.Enabled = false;
                }
                else
                {
                    controller.AxisY.Enabled = true;
                }
                
            }
            a = controller.AxisZ.Axis;
            if (a != null)
            {
                AxisErrorIdentification error = a.ReadAxisError();
                if (error != AxisErrorIdentification.None)
                {
                    MessageBox.Show($"An error occured: {error} on axis {a.DisplayName}");
                    Triamec.Tam.Requests.TamRequest requ = a.Control(AxisControlCommands.ResetError);
                    requ.WaitForTermination();

                }
                AxisState s = a.ReadAxisState();
                if (s == AxisState.Disabled)
                {
                    controller.AxisZ.Enabled = false;
                }
                else
                {
                    controller.AxisZ.Enabled = true;
                }
            }
            a = controller.AxisT.Axis;
            if (a != null)
            {
                AxisErrorIdentification error = a.ReadAxisError();
                if (error != AxisErrorIdentification.None)
                {
                    MessageBox.Show($"An error occured: {error} on axis {a.DisplayName}");
                    Triamec.Tam.Requests.TamRequest requ = a.Control(AxisControlCommands.ResetError);
                    requ.WaitForTermination();

                }
                AxisState s = a.ReadAxisState();
                if (s == AxisState.Disabled)
                {
                    controller.AxisT.Enabled = false;
                }
                else
                {
                    controller.AxisT.Enabled = true;
                }
            }
        }

        #region Program

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveDialog dialog = controller.SaveConfig();
            _ = dialog.ShowDialog();
        }


        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Title = "Load Configuration",
                InitialDirectory = System.IO.Directory.GetCurrentDirectory() + "\\TamConfig",
                Multiselect = false,
                Filter = "TamConfiguration (*.TAMcfg)|*.TAMcfg"
            };

            if ((bool)ofd.ShowDialog(this))
            {
                if (controller.Load(ofd.FileName))
                {
                    _ = MessageBox.Show("Configuration sucessfull loaded!");
                }
                else
                {
                    _ = MessageBox.Show("Error: Configuration not loaded", "Loading failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnPersist_Click(object sender, RoutedEventArgs e)
        {
            controller.Persist();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select folder to store measurement files",
                SelectedPath = resultDir
            };

            fbd.ShowDialog();

            resultDir = fbd.SelectedPath;
            fbd.Dispose();
        }

        private void PrepareMeasurement()
        {
            CBoMeasure.Items.Clear();

            _ = CBoMeasure.Items.Add(
                new AccRep2D("XY Flatness", Axis.X_Linear, Axis.Y, new Procedure<Point>(new CircleGrid2D(140, 10, new Vector(-43, -0.5)), 1))
                {
                    MeasureDevices = new List<IMeasureDevice> { new ExtTrigger() },
                    DataHeader = "PosX[mm];PosY[mm]"
                }
            );
/*
            _ = CBoMeasure.Items.Add(
                new AccRep1D("X Straightness", Axis.X_Linear, new Procedure<double>(new BiDirStep1D(40, 5, -130), 5), 5)
                {
                    MeasureDevices = new List<IMeasureDevice> { new Encoder("tam://mc/XGantry/Axes[1]/Signals/PositionController/Encoders[1]/Position", true) }
                }
            );
            _ = CBoMeasure.Items.Add(
                new AccRep1D("Y Straightness", Axis.Y, new Procedure<double>(new BiDirStep1D(40, 5, -100), 5), 5)
                {
                    MeasureDevices = new List<IMeasureDevice> { new Encoder("tam://mc/XGantry/Axes[0]/Signals/PositionController/Encoders[1]/Position", true) }
                }
            );
*/
            _ = CBoMeasure.Items.Add(
                new Ortho2D("XY Orthogonality", Axis.X_Linear, Axis.Y, new Point(-143, 0), new Point(57, 0), new Point(-43, -100), new Point(-43, 100))
                {
                    MeasureDevices = new List<IMeasureDevice>
                    {
                        // new Encoder("tam://mc/XGantry/Axes[0]/Signals/PositionController/Encoders[1]/Position"),
                        // new Encoder("tam://mc/XGantry/Axes[1]/Signals/PositionController/Encoders[1]/Position"),
                        new ExtEncoder(client: acs_controller, axis_num: 0),
                        new ExtEncoder(client: acs_controller, axis_num: 1)
                    }
                }
            );
/*             _ = CBoMeasure.Items.Add(
                new AccRep1D("T Radial Runout", Axis.T, new Procedure<double>(new BiDirStep1D(35, 10, -175), 5), 4)
                {
                    MeasureDevices = new List<IMeasureDevice> { new ExtTrigger() }
                }
            );
            _ = CBoMeasure.Items.Add(
                new AccRep1D("X Pitch-Yaw", Axis.X_Linear, new Procedure<double>(new BiDirStep1D(43, 10, -260), 5), 5)
                {
                    MeasureDevices = new List<IMeasureDevice> { new ExtTrigger() }
                }
            );
            _ = CBoMeasure.Items.Add(
                new AccRep1D("Y Pitch-Yaw", Axis.Y, new Procedure<double>(new BiDirStep1D(39, 10, -180), 5), 5)
                {
                    MeasureDevices = new List<IMeasureDevice> { new ExtTrigger() }
                }
            );
            _ = CBoMeasure.Items.Add(
                new AccRep1D("Z Pitch-Yaw", Axis.Z, new Procedure<double>(new BiDirStep1D(29, 0.4, 0.1), 5))
                {
                    MeasureDevices = new List<IMeasureDevice> { new ExtTrigger() { EndOfTravelMessage = "Please measure next pass on Autocollimator"} }
                }
            ); 
*/
            _ = CBoMeasure.Items.Add(
                new AccRep2D("XY Accuracy-Repeatability", Axis.X_Linear, Axis.Y, new Procedure<Point>(new BiDirStep2D(19, new Vector(10, 10), new Vector(-142, -96)), 5), new Vector(10, 10))
                {
                    MeasureDevices = new List<IMeasureDevice>
                    {
                        // new Encoder("tam://mc/XGantry/Axes[0]/Signals/PositionController/Encoders[1]/Position"),
                        // new Encoder("tam://mc/XGantry/Axes[1]/Signals/PositionController/Encoders[1]/Position"),
                        new ExtEncoder(client: acs_controller, axis_num: 0),
                        new ExtEncoder(client: acs_controller, axis_num: 1)
                    }
                }
            );
            _ = CBoMeasure.Items.Add(
                new AccRep1D("Z Accuracy-Repeatability", Axis.Z, new Procedure<double>(new BiDirStep1D(29, 0.4, 0.1), 5))
                {
                    MeasureDevices = new List<IMeasureDevice> { 
                        // new Encoder("tam://mc/YTheta/Axes[0]/Signals/PositionController/Encoders[1]/Position") 
                        new ExtEncoder(client: acs_controller, axis_num: 4)
                    },
                    PreMeasureDelay = TimeSpan.FromMilliseconds(100)
                }
            );
            _ = CBoMeasure.Items.Add(
                new AccRep1D("T Accuracy-Repeatability", Axis.T, new Procedure<double>(new BiDirStep1D(70, 5, -175), 1))
                {
                    MeasureDevices = new List<IMeasureDevice> { new Encoder("tam://mc/YTheta/Axes[0]/Signals/PositionController/Encoders[1]/Position") },
                    DataHeader = "Pos[deg]; Error[deg]"
                }
            );
            //_ = CBoMeasure.Items.Add(
            //    new AccRep2D("XY Mapping", Axis.X_Linear, Axis.Y, new Procedure<Point>(new Grid2D(40, 40, new Vector(5, 5), new Vector(-146, -100)), 1))
            //    {
            //        IsMapping = true,
            //        MeasureDevices = new List<IMeasureDevice>
            //        {
            //            // new Encoder("tam://mc/XGantry/Axes[0]/Signals/PositionController/Encoders[1]/Position"),
            //            // new Encoder("tam://mc/XGantry/Axes[1]/Signals/PositionController/Encoders[1]/Position"),
            //            new ExtEncoder(client: acs_controller, axis_num: 0),
            //            new ExtEncoder(client: acs_controller, axis_num: 1)
            //        }
            //    }
            //);
            //_ = CBoMeasure.Items.Add(
            //    new AccRep1D("Z Mapping", Axis.Z, new Procedure<double>(new BiDirStep1D(60, 0.2), 1))
            //    {
            //        IsMapping = true,
            //        MeasureDevices = new List<IMeasureDevice> { 
            //            // new Encoder("tam://mc/YTheta/Axes[0]/Signals/PositionController/Encoders[1]/Position") 
            //            new ExtEncoder(client: acs_controller, axis_num: 4)
            //        },
            //        PreMeasureDelay = TimeSpan.FromMilliseconds(500)
            //    }
            //);
            _ = CBoMeasure.Items.Add(
                new AccRep1D("T Mapping", Axis.T, new Procedure<double>(new BiDirStep1D(71, 5, -177.5), 2))
                {
                    IsMapping = true,
                    MeasureDevices = new List<IMeasureDevice> { new Encoder("tam://mc/YTheta/Axes[0]/Signals/PositionController/Encoders[1]/Position") }
                }
            );
            /*
                        List<CycleData> cyDat = new List<CycleData>
                        {
                            new CycleData(Axis.X_Linear, new Procedure<double>(new BiDirStep1D(18, 25, -270, true), int.MaxValue), TimeSpan.FromMilliseconds(250)),
                            new CycleData(Axis.Y, new Procedure<double>(new BiDirStep1D(16, 25, -185, true), int.MaxValue), TimeSpan.FromMilliseconds(250)),
                            new CycleData(Axis.Z, new Procedure<double>(new BiDirStep1D(100, 0.1, 1, true), int.MaxValue), TimeSpan.FromMilliseconds(500)),
                            new CycleData(Axis.T, new Procedure<double>(new BiDirStep1D(1, 180, 0, true), int.MaxValue), TimeSpan.FromSeconds(6))
                        };

                        _ = CBoMeasure.Items.Add(
                            new DutyCycle("DutyCycle", cyDat.ToArray(), new TimeSpan(24, 0, 0), TimeSpan.FromSeconds(30))
                        );
            */
            List<CycleData> cyDat = new List<CycleData>
            {
                new CycleData(Axis.X_Linear, new Procedure<double>(new BiDirStep1D(1, 450, -235, true), int.MaxValue), TimeSpan.FromSeconds(2)),
                new CycleData(Axis.Y, new Procedure<double>(new BiDirStep1D(1, 410, -190, true), int.MaxValue), TimeSpan.FromSeconds(2.1)),
                new CycleData(Axis.Z, new Procedure<double>(new BiDirStep1D(1, 12, 0, true), int.MaxValue), TimeSpan.FromSeconds(10)),
                new CycleData(Axis.T, new Procedure<double>(new BiDirStep1D(1, 180, 0, true), int.MaxValue), TimeSpan.FromSeconds(6))
            };

            _ = CBoMeasure.Items.Add(
                new DutyCycle("Burnin", cyDat.ToArray(), new TimeSpan(24, 0, 0), TimeSpan.FromSeconds(30))
            );

            Procedure<Point> procedureMS = new Procedure<Point>(new Grid2D(4, 4, new Vector(80, 80), new Vector(-190, -150)), 1);
            const float settlingTime = 0.1f; // settling time will be substracted from move time, this is just to get sure, that axis keep within window after M&S measurement

            //_ = CBoMeasure.Items.Add(new MoveAndSettle2D("XY 75mm 100nm MS", Axis.X_Linear, Axis.Y, procedureMS, 75, 0.1e-3f, settlingTime));
            _ = CBoMeasure.Items.Add(new MoveAndSettle2D("XY 25mm 100nm MS", Axis.X_Linear, Axis.Y, procedureMS, 25, 0.1e-3f, settlingTime));
            _ = CBoMeasure.Items.Add(new MoveAndSettle2D("XY 25mm 5nm MS", Axis.X_Linear, Axis.Y, procedureMS, 25, 5e-6f, settlingTime));
            //_ = CBoMeasure.Items.Add(new MoveAndSettle2D("XY 25mm 0.6nm MS", Axis.X_Linear, Axis.Y, procedureMS, 25, 0.6e-6f, settlingTime));
            //_ = CBoMeasure.Items.Add(new MoveAndSettle2D("XY 1mm 30nm MS", Axis.X_Linear, Axis.Y, procedureMS, 1, 30e-6f, settlingTime));
            _ = CBoMeasure.Items.Add(new MoveAndSettle2D("XY 0.1mm 30nm MS", Axis.X_Linear, Axis.Y, procedureMS, 0.1, 30e-6f, settlingTime));

            _ = CBoMeasure.Items.Add(new MoveAndSettle1D("Z 0.1 MS", Axis.Z, new Procedure<double>(new UniDirStep1D(9, 1, 1), 1), 0.1, 30e-6f, settlingTime) { PreMeasureDelay = TimeSpan.FromMilliseconds(1000) });
            _ = CBoMeasure.Items.Add(new MoveAndSettle1D("T 180 MS", Axis.T, new Procedure<double>(new UniDirStep1D(5, 60, -180), 1), 180, 40e-6f, settlingTime));
            //_ = CBoMeasure.Items.Add(new MoveAndSettle1D("T 90 MS", Axis.T, new Procedure<double>(new UniDirStep1D(5, 60, -180), 1), 90, 40e-6f, settlingTime));

            CBoMeasure.SelectedIndex = 0;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (CBoMeasure.SelectedItem != null)
            {
                IMeasure measure = CBoMeasure.SelectedItem as IMeasure;
                Progress<TaskProgReport> prog = new Progress<TaskProgReport>(UpdateProgress);
                caToSou = new CancellationTokenSource();
                CancellationToken caTok = caToSou.Token;
                controller.MeasureAsync(measure, resultDir, prog); // start measurement
            }
            else
            {
                MessageBox.Show("Select Measurement");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            caToSou.Cancel();
            caToSou.Dispose();
            controller.Cancel();
        }
        #endregion

        #region Machine
        private void BtnEnable_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            StackPanel stack = btn.Content as StackPanel;
            TextBlock block = stack.Children[1] as TextBlock;
            Axis ax = GetAxisFromString(block.Text);
            TriaAxis axis = controller.GetAxis(ax);

            Progress<TaskProgReport> prog = new Progress<TaskProgReport>(UpdateProgress);

            if (axis.Enabled)
            {   
                controller.DisableAsync(ax, prog);
            }
            else
            {
                controller.EnableAsync(ax, prog);
            }
        }

        private static Axis GetAxisFromString(string str)
        {
            foreach(Axis ax in Enum.GetValues(typeof(Axis)))
            {
                if (str.Contains(ax.ToString()))
                    return ax;
            }
            return Axis.X_Linear;
        }

        private void BtnEnableDamping_Click(object sender, RoutedEventArgs e)
        {
            Progress<TaskProgReport> prog = new Progress<TaskProgReport>(UpdateProgress);
            controller.SetDamping(!controller.DampingIsReady, 3000, prog);
        }

        private void BtnResetDamping_Click(object sender, RoutedEventArgs e)
        {
            Progress<TaskProgReport> prog = new Progress<TaskProgReport>(UpdateProgress);
            controller.ResetDamping(5000, prog);
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            Progress<TaskProgReport> prog = new Progress<TaskProgReport>(UpdateProgress);
            Dictionary<Axis, TimeSpan> home = new Dictionary<Axis, TimeSpan>()
            {
                {Axis.X_Linear, TimeSpan.FromSeconds(15) },
                {Axis.Y, TimeSpan.FromSeconds(13) },
                {Axis.Z, TimeSpan.FromSeconds(11) },
                {Axis.T, TimeSpan.FromSeconds(8) }
            };
            controller.HomeAsync(home, prog);
        }

        private void BtnMove_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            switch (btn.Name)
            {
                case "BtnMovePark":
                    controller.MoveAsync(new Coordinate(0, 0, 10, 0));
                    break;
                case "BtnMoveUp":
                    controller.MoveAdditiveAsync(Axis.Y, relStepXY);
                    break;
                case "BtnMoveUpRight":
                    controller.MoveAdditiveAsync(Axis.X_Linear, relStepXY);
                    controller.MoveAdditiveAsync(Axis.Y, relStepXY);
                    break;
                case "BtnMoveRight":
                    controller.MoveAdditiveAsync(Axis.X_Linear, relStepXY);
                    break;
                case "BtnMoveDownRight":
                    controller.MoveAdditiveAsync(Axis.X_Linear, relStepXY);
                    controller.MoveAdditiveAsync(Axis.Y, -relStepXY);
                    break;
                case "BtnMoveDown":
                    controller.MoveAdditiveAsync(Axis.Y, -relStepXY);
                    break;
                case "BtnMoveDownLeft":
                    controller.MoveAdditiveAsync(Axis.X_Linear, -relStepXY);
                    controller.MoveAdditiveAsync(Axis.Y, -relStepXY);
                    break;
                case "BtnMoveLeft":
                    controller.MoveAdditiveAsync(Axis.X_Linear, -relStepXY);
                    break;
                case "BtnMoveUpLeft":
                    controller.MoveAdditiveAsync(Axis.X_Linear, -relStepXY);
                    controller.MoveAdditiveAsync(Axis.Y, relStepXY);
                    break;
                case "BtnZMoveUp":
                    controller.MoveAdditiveAsync(Axis.Z, relStepZ);
                    break;
                case "BtnZMoveDown":
                    controller.MoveAdditiveAsync(Axis.Z, -relStepZ);
                    break;
                case "BtnTMovePos":
                    controller.MoveAdditiveAsync(Axis.T, relStepT);
                    break;
                case "BtnTMoveNeg":
                    controller.MoveAdditiveAsync(Axis.T, -relStepT);
                    break;
                default:
                    throw new NotImplementedException("Unknown Button");
            }
        }

        #endregion

        #region Parameter
        private void BtnHomeOffsets_Click(object sender, RoutedEventArgs e)
        {
            OffsetDialog offDia = new OffsetDialog(controller.GetReferenceCoordinate());
            offDia.Owner = this;

            if ((bool)offDia.ShowDialog())
            {
                controller.SetReferencePosition(offDia.X, Axis.X_Linear);
                controller.SetReferencePosition(offDia.X, Axis.X_Rotary);
                controller.SetReferencePosition(offDia.Y, Axis.Y);
                controller.SetReferencePosition(offDia.Z, Axis.Z);
                controller.SetReferencePosition(offDia.T, Axis.T);
            }
        }

        #endregion

        #region StatusBar
        public void SetPosition(Coordinate Position)
        {
            TbPosX.Text = Position.X.ToString("N3");
            TbPosY.Text = Position.Y.ToString("N3");
            TbPosZ.Text = Position.Z.ToString("N3");
            TbPosT.Text = Position.T.ToString("N3");

            ExtEncoder a0 = new ExtEncoder(client: acs_controller, axis_num: 0);
            ExtEncoder a1 = new ExtEncoder(client: acs_controller, axis_num: 1);
            ExtEncoder a2 = new ExtEncoder(client: acs_controller, axis_num: 4);
            TbPosA0.Text = a0.GetFeedback().ToString("F4");
            TbPosA1.Text = a1.GetFeedback().ToString("F4");
            TbPosA2.Text = a2.GetFeedback().ToString("F4");
        }

        public void UpdateProgress(TaskProgReport prog)
        {
            int percent = 100 * prog.CurrentProgess / prog.TotalProgess;

            if (percent < 100)
            {
                PgbProgress.Visibility = Visibility.Visible;
                PgbProgress.Value = percent;
                TbProgMsg.Visibility = Visibility.Visible;
                TbProgMsg.Text = prog.Message;
            }
            else
            {
                PgbProgress.Visibility = Visibility.Hidden;
                TbProgMsg.Visibility = Visibility.Hidden;
            }
        }

        public void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (controller.IsConnected)
            {
                timer.Stop();
                controller.Cancel();
                while (controller.IsBusy) ; // wait till controllerTask is finished
                controller.Dispose();
            }
            else
            {
                Progress<TaskProgReport> prog = new Progress<TaskProgReport>(UpdateProgress);
                controller = new TriaController();
                DataContext = controller; // for data binding
                controller.ConnectAsync(prog); // connect to controller in dedicated task

                // used to update ui with position info
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(50);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        #endregion


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
