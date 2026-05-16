using System;

namespace Service
{
    public class TransferEventArgs : EventArgs
    {
        public string SessionId { get; }
        public TransferEventArgs(string sessionId)
        {
            SessionId = sessionId;
        }
    }
}
