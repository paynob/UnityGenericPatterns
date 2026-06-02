using System;

namespace Paynob.Patterns.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void Warn(string message)
        {
            Console.WriteLine($"WARN: {message}");
        }

        public void Error(string message)
        {
            Console.Error.WriteLine($"ERROR: {message}");
        }

        public void Debug(string message)
        {
            Console.WriteLine($"DEBUG: {message}");
        }
    }
}