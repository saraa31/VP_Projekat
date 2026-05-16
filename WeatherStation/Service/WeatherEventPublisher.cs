using System;
using Common;

namespace Service
{
    public class WeatherEventPublisher
    {
        public delegate void TransferEventHandler(object sender, TransferEventArgs e);
        public delegate void SampleEventHandler(object sender, SampleEventArgs e);
        public delegate void WarningEventHandler(object sender, WarningEventArgs e);

        public event TransferEventHandler OnTransferStarted;
        public event SampleEventHandler OnSampleReceived;
        public event TransferEventHandler OnTransferCompleted;
        public event WarningEventHandler OnWarningRaised;

        public void RaiseTransferStarted(string sessionId)
        {            
            if (OnTransferStarted != null)
                OnTransferStarted(this, new TransferEventArgs(sessionId));
        }
        public void RaiseSampleReceived(WeatherSample sample)
        {           
            if (OnSampleReceived != null)
                OnSampleReceived(this, new SampleEventArgs(sample));
        }
        public void RaiseTransferCompleted(string sessionId)
        {           
            if (OnTransferCompleted != null)
                OnTransferCompleted(this, new TransferEventArgs(sessionId));
        }
        public void RaiseWarning(string type, string direction, string message)
        {            
            if (OnWarningRaised != null)
                OnWarningRaised(this, new WarningEventArgs(type, direction, message));
        }
    }
}
