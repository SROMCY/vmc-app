using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMC.Measurement
{
    public interface IMeasureDevice
    {
        double GetFeedback(double param);
    }
}