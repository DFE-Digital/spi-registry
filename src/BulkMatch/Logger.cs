using System;
using Dfe.Spi.Common.Logging.Definitions;

namespace BulkMatch
{
    class Logger : ILoggerWrapper
    {
        public void Debug(string message, Exception exception = null)
        {
            // if (exception == null)
            // {
            //     Console.WriteLine(message);
            // }
            // else
            // {
            //     Console.WriteLine(message + "\n" + exception.ToString());
            // }
        }

        public void Info(string message, Exception exception = null)
        {
            if (exception == null)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message + "\n" + exception.ToString());
            }
        }

        public void Warning(string message)
        {
            WriteColored(message, ConsoleColor.Yellow);
        }

        public void Warning(string message, Exception exception)
        {
            WriteColored(message + "\n" + exception.ToString(), ConsoleColor.Yellow);
        }

        public void Error(string message)
        {
            WriteColored(message, ConsoleColor.Red);
        }

        public void Error(string message, Exception exception)
        {
            WriteColored(message + "\n" + exception.ToString(), ConsoleColor.Yellow);
        }

        public void Error(Exception exception)
        {
            WriteColored(exception.ToString(), ConsoleColor.Red);
        }

        private void WriteColored(string message, ConsoleColor color)
        {
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}