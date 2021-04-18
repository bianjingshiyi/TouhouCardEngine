using System;
using System.Collections.Generic;
using UnityEngine;

namespace TouhouCardEngine
{
    public class UnityLogger : Shared.ILogger
    {
        string name { get; }
        public UnityLogger(string name = null)
        {
            this.name = name;
        }
        public bool enable { get; set; } = true;

        public List<string> blackList { get; set; } = new List<string>();

        public void log(string channel, string msg)
        {
            if (!enable)
                return;
            if (blackList.Contains(channel))
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
        public void logWarn(string msg)
        {
            logWarn(null, msg);
        }
        public void logWarn(string channel, string msg)
        {
            if (!enable)
                return;
            if (blackList.Contains(channel))
                return;
            if (string.IsNullOrEmpty(channel))
                Debug.LogWarning((string.IsNullOrEmpty(name) ? null : name + "：") + msg);
            else
                Debug.LogWarning((string.IsNullOrEmpty(name) ? null : name + "（" + channel + "）：") + msg);
        }

        public void logError(string msg)
        {
            logError(null, msg);
        }

        public void logError(string channel, string msg)
        {
            if (!enable)
                return;
            if (blackList.Contains(channel))
                return;
            if (string.IsNullOrEmpty(channel))
                Debug.LogError((string.IsNullOrEmpty(name) ? null : name + "：") + msg);
            else
                Debug.LogError((string.IsNullOrEmpty(name) ? null : name + "（" + channel + "）：") + msg);
        }

        public void logTrace(string msg)
        {
            logTrace(null, msg);
        }

        public void logTrace(string channel, string msg)
        {
            if (!enable)
                return;
            if (!enable)
                return;
            if (blackList.Contains(channel))
                return;
            if (string.IsNullOrEmpty(channel))
                Debug.Log("[TRACE] " + (string.IsNullOrEmpty(name) ? null : name + "：") + msg);
            else
                Debug.Log("[TRACE] " + (string.IsNullOrEmpty(name) ? null : name + "（" + channel + "）：") + msg);
        }
    }
}
