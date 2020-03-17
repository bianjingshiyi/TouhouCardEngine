using UnityEngine;

namespace TouhouCardEngine
{
    public class UnityLogger : Interfaces.ILogger
    {
        public void log(string channel, string msg)
        {
            if (channel == "Debug")
                Debug.Log(msg);
            else if (channel == "Warning")
                Debug.LogWarning(msg);
            else if (channel == "Error")
                Debug.LogError(msg);
        }
        public void log(string msg)
        {
            log("Debug", msg);
        }
    }
}
