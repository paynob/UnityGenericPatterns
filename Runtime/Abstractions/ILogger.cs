namespace Paynob.Patterns
{
    public interface ILogger
    {
        void Log(string message);
        void Debug(string message);
        void Warn(string message);
        void Error(string message); 
    }
}