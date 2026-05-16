using System;
using System.IO;

namespace Service
{
    public class SessionWriter : IDisposable
    {
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        private bool _disposed = false;

        public SessionWriter(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            _streamWriter = new StreamWriter(_fileStream);
        }
        public void WriteSessionData(string data)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SessionWriter));
            _streamWriter.WriteLine(data);
            _streamWriter.Flush();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _streamWriter?.Dispose();
                    _fileStream?.Dispose();
                }
                _disposed = true;
            }
        }
        ~SessionWriter()
        {
            Dispose(false);
        }
    }
}
