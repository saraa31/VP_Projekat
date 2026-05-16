using System;
using System.IO;

namespace Client
{
    public class CSVReader : IDisposable
    {
        private FileStream _fileStream;
        private StreamReader _streamReader;
        private bool _disposed = false;

        public CSVReader(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            _streamReader = new StreamReader(_fileStream);
        }
        public string ReadLine()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CSVReader));

            return _streamReader.ReadLine();
        }
        public bool EndOfStream => _streamReader.EndOfStream;

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
                    _streamReader?.Dispose();
                    _fileStream?.Dispose();
                }

                _disposed = true;
            }
        }

        ~CSVReader()
        {
            Dispose(false);
        }
    }
}
