namespace Paynob.Patterns.Logging
{
    public class UnityDebugLogger : ILogger
    {
        public void Log(string message)
        {
            UnityEngine.Debug.Log($"<b><color=#AAF>[INFO]: </color></b>{message}");
        }

        public void Warn(string message)
        {
            UnityEngine.Debug.LogWarning($"<b><color=#2CD>[WARN]: </color></b>{message}");
        }

        public void Error(string message)
        {
            UnityEngine.Debug.LogError($"<b><color=#F42>[ERROR]: </color></b>{message}");
        }

        public void Debug(string message)
        {
            UnityEngine.Debug.Log($"<b><color=#2C2>[DEBUG]: </color></b>{message}");
        }
    }
}