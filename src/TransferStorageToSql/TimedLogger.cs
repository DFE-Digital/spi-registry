using System;
using Dfe.Spi.Common.Logging.Definitions;

namespace TransferStorageToSql
{
    public class TimedLogger : ILoggerWrapper
    {
        private readonly ILoggerWrapper _innerLogger;
        private DateTime? _lastLogMessage = null;

        public TimedLogger(ILoggerWrapper innerLogger)
        {
            _innerLogger = innerLogger;
        }
        
        public void Debug(string message, Exception exception = null)
        {
            _innerLogger.Debug(message, exception);
        }

        public void Info(string message, Exception exception = null)
        {
            message = GetMessageAndUpdateTimer(message);
            _innerLogger.Info(message, exception);
        }

        public void Warning(string message, Exception exception = null)
        {
            message = GetMessageAndUpdateTimer(message);
            _innerLogger.Warning(message, exception);
        }

        public void Error(string message, Exception exception = null)
        {
            message = GetMessageAndUpdateTimer(message);
            _innerLogger.Error(message, exception);
        }

        private string GetMessageAndUpdateTimer(string message)
        {
            var timestamp = DateTime.Now;

            if (_lastLogMessage.HasValue)
            {
                var duration = timestamp - _lastLogMessage.Value;
                _lastLogMessage = DateTime.Now;
                return $"{timestamp:HH:mm:ss} {message} ({duration.Minutes}m {duration.Seconds:00})";
            }

            _lastLogMessage = DateTime.Now;
            return $"{timestamp:HH:mm:ss} {message}";
        }
    }
}