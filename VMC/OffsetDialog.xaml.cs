using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VMC.Misc;
using VMC.Controller;

namespace VMC
{
    /// <summary>
    /// Logic for OffsetDialog.xaml
    /// </summary>
    public partial class OffsetDialog : Window, INotifyPropertyChanged
    {
        public double X
        {
            get { return _offset.X; }
            set
            {
                _offset.X = value;
                OnPropertyChanged(nameof(X));
            }
        }

        public double Y
        {
            get { return _offset.Y; }
            set
            {
                _offset.Y = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public double Z
        {
            get { return _offset.Z; }
            set
            {
                _offset.Z = value;
                OnPropertyChanged(nameof(Z));
            }
        }

        public double T
        {
            get { return _offset.T; }
            set
            {
                _offset.T = value;
                OnPropertyChanged(nameof(T));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Coordinate _offset;
        public OffsetDialog()
        {
            InitializeComponent();
            _offset = new Coordinate();
            SetBindings();
        }

        public OffsetDialog(Coordinate offset)
        {
            InitializeComponent();
            _offset = offset;
            SetBindings();
        }

        private void SetBindings()
        {
            Binding bdgX = new Binding(nameof(X))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtOffsetX.SetBinding(TextBox.TextProperty, bdgX);
            OnPropertyChanged(nameof(X));

            Binding bdgY = new Binding(nameof(Y))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtOffsetY.SetBinding(TextBox.TextProperty, bdgY);
            OnPropertyChanged(nameof(Y));

            Binding bdgZ = new Binding(nameof(Z))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtOffsetZ.SetBinding(TextBox.TextProperty, bdgZ);
            OnPropertyChanged(nameof(Z));

            Binding bdgT = new Binding(nameof(T))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = new DoubleStringConverter()
            };
            TxtOffsetT.SetBinding(TextBox.TextProperty, bdgT);
            OnPropertyChanged(nameof(T));
        }

        private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
