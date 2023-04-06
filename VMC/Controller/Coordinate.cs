using System;

namespace VMC.Controller
{
    public class Coordinate : IEquatable<Coordinate>
    {
        public double X
        {
            get { return positions[(int)Axis.X_Linear]; }
            set { positions[(int)Axis.X_Linear] = value; }
        }
        public double Y
        {
            get { return positions[(int)Axis.Y]; }
            set { positions[(int)Axis.Y] = value; }
        }
        public double Z
        {
            get { return positions[(int)Axis.Z]; }
            set { positions[(int)Axis.Z] = value; }
        }
        public double T
        {
            get { return positions[(int)Axis.T]; }
            set { positions[(int)Axis.T] = value; }
        }

        private double[] positions;
        public Coordinate()
        {
            positions = new double[Enum.GetValues(typeof(Axis)).Length];
        }
        public Coordinate(double x = 0, double y = 0, double z = 0, double t = 0)
        {
            positions = new double[Enum.GetValues(typeof(Axis)).Length];
            X = x;
            Y = y;
            Z = z;
            T = t;
        }

        public double GetPosition(Axis ax)
        {
            return positions[(int)ax];
        }

        public void SetPosition(Axis ax, double value)
        {
            positions[(int)ax] = value;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(Coordinate))
            {
                Coordinate other = obj as Coordinate;
                return Equals(other);
            }
            return false;
        }

        public static bool operator ==(Coordinate left, Coordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Coordinate left, Coordinate right)
        {
            return !(left == right);
        }

        public bool Equals(Coordinate other)
        {
            if (other.positions == positions)
                return true;
            else
                return false;
        }
    }
}
