using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VMC.Misc;
using VMC.Controller;

namespace VMC.Measurement
{
    public interface IMeasure
    {
        Task Measure(string directory, TriaController controller, IProgress<TaskProgReport> progress, CancellationToken caTok);
    }
}
