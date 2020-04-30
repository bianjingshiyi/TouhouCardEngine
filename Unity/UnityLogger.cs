using UnityEngine;

namespace TouhouCardEngine
{
    public class UnityLogger : Interfaces.ILogger
    {
        string name { get; }
        public UnityLogger(string name = null)
        {
            this.name = name;
        }
        public bool enable { get; set; } = true;
        public void log(string channel, string msg)
        {
            if (!enable)
                return;
            if (channel == "Debug")
                Debug.Log((string.IsNullOrEmpty(name) ? null : name + "：") + msg);
            else if (channel == "Warning")
                Debug.LogWarning((string.IsNullOrEmpty(name) ? null : name + "：") + msg);
            else if (channel == "Error")
                Debug.LogError((string.IsNullOrEmpty(name) ? null : name + "：") + msg);
            else
                Debug.Log((string.IsNullOrEmpty(name) ? null : name + "（" + channel + "）：") + msg);
        }
        public void log(string msg)
        {
            if (!enable)
                return;
            log("Debug", msg);
        }
    }
}
