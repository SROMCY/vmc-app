using System;

namespace VMC.Measurement
{
    public class Encoder : IMeasureDevice
    {
        public Uri RegisterPath { get; private set; }
        public bool SetZero { get; set; } // indicates wether the offset belogs to an axis position or should be set to zero
        private double offset; // offset to start position

        public Encoder(string registerPath, bool setZero = false)
        {
            RegisterPath = new Uri(registerPath, UriKind.Absolute);
            SetZero = setZero;
            offset = 0;
        }

        public void SetOffset(double desiredPosition, double measurePosition)
        {
            offset = desiredPosition - measurePosition;
        }

        public double GetFeedback(double encPosition)
        {
            return encPosition + offset;
        }
    }
}
