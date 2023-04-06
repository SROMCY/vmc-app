using System;
using System.Threading;
using System.Windows;

namespace VMC.Measurement
{
    public class ExtTrigger : IMeasureDevice
    {
        public string EndOfTravelMessage { get; set; }
        public TimeSpan PreDelay { get; set; }
        public TimeSpan PostDelay { get; set; }

        private bool endOfTravelReached;

        public ExtTrigger()
        {
            EndOfTravelMessage = string.Empty;
            PreDelay = TimeSpan.FromMilliseconds(500);
            PostDelay = TimeSpan.FromMilliseconds(0);
        }

        public ExtTrigger(TimeSpan preDelay, TimeSpan postDelay)
        {
            EndOfTravelMessage = string.Empty;
            PreDelay = preDelay;
            PostDelay = postDelay;
        }

        public void ShowMessage(bool isEndOfTravel)
        {
            if (!endOfTravelReached && isEndOfTravel && !string.IsNullOrEmpty(EndOfTravelMessage))
            {
                endOfTravelReached = true;
                MessageBox.Show(EndOfTravelMessage, "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            endOfTravelReached = isEndOfTravel;
        }

        public void WaitPreDelay()
        {
            Thread.Sleep(PreDelay);
        }

        public double GetFeedback(double onTimeMs = 30)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(onTimeMs));
            return 1;
        }

        public void WaitPostDelay()
        {
            Thread.Sleep(PostDelay);
        }
    }
}
