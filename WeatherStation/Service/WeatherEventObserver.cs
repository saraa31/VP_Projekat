using System;

namespace Service
{
    public class WeatherEventObserver
    {
        public WeatherEventObserver(WeatherEventPublisher publisher)
        {
            publisher.OnTransferStarted += OnTransferStarted;
            publisher.OnSampleReceived += OnSampleReceived;
            publisher.OnTransferCompleted += OnTransferCompleted;
            publisher.OnWarningRaised += OnWarningRaised;
        }

        private void OnTransferStarted(object sender, TransferEventArgs e)
        {
            Console.WriteLine($"[EVENT] Session {e.SessionId} - transfer started.\n");
        }
        private void OnSampleReceived(object sender, SampleEventArgs e)
        {
            Console.WriteLine($"[EVENT] Sample received: Date={e.Sample.Date} T={e.Sample.T} P={e.Sample.Pressure} Tpot={e.Sample.Tpot} Tdew={e.Sample.Tdew} Rh={e.Sample.Rh} Sh={e.Sample.Sh}\n");
        }
        private void OnTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.WriteLine($"[EVENT] Session {e.SessionId} - transfer completed.\n");
        }
        private void OnWarningRaised(object sender, WarningEventArgs e)
        {
            Console.WriteLine($"[EVENT] Warning raised: Type={e.WarningType} Direction={e.Direction} Message={e.Message}\n");
        }
    }
}
