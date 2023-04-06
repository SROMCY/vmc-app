using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMC.Controller
{
    public enum ChecksumMode { Ignore, Check, Calculate }

    public struct TriaTblDimension
    {
        public TriaTblDimension(int size, float startValue, float distance)
        {
            Size = size;
            StartValue = startValue;
            Distance = distance;
        }

        public int Size { get; set; }
        public float StartValue { get; set; }
        public float Distance { get; set; }

    }

    public class TriaTblHeader
    {
        private readonly byte[] data;

        private enum Register
        {
            Persistent = 0, ChecksumMode = 12, Date = 64,
            Id = 76, Description = 80, RowSize = 144,
            Dim1Size = 160, Dim1StartValue = 168, Dim1Distance = 172,
            Dim2Size = 176, Dim2StartValue = 184, Dim2Distance = 188,
            Dim3Size = 192, Dim3StartValue = 200, Dim3Distance = 204
        }
        public bool Persistent
        {
            get => BitConverter.ToBoolean(data, (int)Register.Persistent);
            set => BitConverter.GetBytes(value).CopyTo(data, (int)Register.Persistent);
        }

        public ChecksumMode ChecksumMode
        {
            get => (ChecksumMode)BitConverter.ToInt32(data, (int)Register.ChecksumMode);
            set => BitConverter.GetBytes((int)value).CopyTo(data, (int)Register.ChecksumMode);
        }

        public DateTimeOffset Date
        {
            get => DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(data, (int)Register.Date));
            set => BitConverter.GetBytes(value.ToUnixTimeSeconds()).CopyTo(data, (int)Register.Date);
        }

        public int Id
        {
            get => BitConverter.ToInt32(data, (int)Register.Id);
            set => BitConverter.GetBytes(value).CopyTo(data, (int)Register.Id);
        }

        public string Description
        {
            get => BitConverter.ToString(data, (int)Register.Description, 60);
            set
            {
                for (int ii = 0; ii < value.Length && ii < 60; ii++)
                {
                    BitConverter.GetBytes(value[ii]).CopyTo(data, (int)Register.Description + ii);
                }
            }
        }

        public int RowSize
        {
            get => BitConverter.ToInt32(data, (int)Register.RowSize);
            set => BitConverter.GetBytes(value).CopyTo(data, (int)Register.RowSize);
        }

        public TriaTblDimension FirstDimension
        {
            get
            {
                int size = BitConverter.ToInt32(data, (int)Register.Dim1Size);
                float startValue = BitConverter.ToSingle(data, (int)Register.Dim1StartValue);
                float distance = BitConverter.ToSingle(data, (int)Register.Dim1Distance);
                return new TriaTblDimension(size, startValue, distance);
            }
            set
            {
                BitConverter.GetBytes(value.Size).CopyTo(data, (int)Register.Dim1Size);
                BitConverter.GetBytes(value.StartValue).CopyTo(data, (int)Register.Dim1StartValue);
                BitConverter.GetBytes(value.Distance).CopyTo(data, (int)Register.Dim1Distance);
            }
        }

        public TriaTblDimension SecondDimension
        {
            get
            {
                int size = BitConverter.ToInt32(data, (int)Register.Dim2Size);
                float startValue = BitConverter.ToSingle(data, (int)Register.Dim2StartValue);
                float distance = BitConverter.ToSingle(data, (int)Register.Dim2Distance);
                return new TriaTblDimension(size, startValue, distance);
            }
            set
            {
                BitConverter.GetBytes(value.Size).CopyTo(data, (int)Register.Dim2Size);
                BitConverter.GetBytes(value.StartValue).CopyTo(data, (int)Register.Dim2StartValue);
                BitConverter.GetBytes(value.Distance).CopyTo(data, (int)Register.Dim2Distance);
            }
        }

        public TriaTblDimension ThirdDimension
        {
            get
            {
                int size = BitConverter.ToInt32(data, (int)Register.Dim3Size);
                float startValue = BitConverter.ToSingle(data, (int)Register.Dim3StartValue);
                float distance = BitConverter.ToSingle(data, (int)Register.Dim3Distance);
                return new TriaTblDimension(size, startValue, distance);
            }
            set
            {
                BitConverter.GetBytes(value.Size).CopyTo(data, (int)Register.Dim3Size);
                BitConverter.GetBytes(value.StartValue).CopyTo(data, (int)Register.Dim3StartValue);
                BitConverter.GetBytes(value.Distance).CopyTo(data, (int)Register.Dim3Distance);
            }
        }

        public TriaTblHeader()
        {
            data = new byte[256];
        }


        public void Write(string filename)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.OpenOrCreate)))
            {
                foreach(byte field in data)
                {
                    writer.Write(field);
                }
            }
        }
    }
}
