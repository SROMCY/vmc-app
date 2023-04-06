using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMC.Measurement
{
    public class FeedbackSimulator : IMeasureDevice
    {
        private readonly Random rnd;

        public FeedbackSimulator()
        {
            rnd = new Random();
        }

        public double GetFeedback(double range = 2)
        {
            double minValue = -range / 2;
            return (rnd.NextDouble() * range) + minValue;
        }
    }
}
