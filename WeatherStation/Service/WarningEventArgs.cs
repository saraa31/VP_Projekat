using System;

namespace Service
{
    public class WarningEventArgs : EventArgs
    {
        public string WarningType { get; }
        public string Direction { get; }
        public string Message { get; }
        public WarningEventArgs(string warningType, string direction, string message)
        {
            WarningType = warningType;
            Direction = direction;
            Message = message;
        }
    }
}
