using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TouhouCardEngine
{
    [RequireComponent(typeof(Text))]
    public class ScreenLogger : MonoBehaviour, Interfaces.ILogger
    {
        Text textComponent = null;
        Text TextComponent
        {
            get
            {
                textComponent = textComponent ?? GetComponent<Text>();
                return textComponent;
            }
        }

        public bool enable { get; set; } = true;
        public List<string> blackList { get; set; } = new List<string>();

        public void log(string msg)
        {
            TextComponent.text = msg;
        }

        public void log(string channel, string msg)
        {
            TextComponent.text = $"[{channel}] {msg}";
        }

        public void logError(string msg)
        {
            TextComponent.text = $"[Error] {msg}";
        }

        public void logError(string channel, string msg)
        {
            TextComponent.text = $"[Error][{channel}] {msg}";
        }

        public void logWarn(string msg)
        {
            TextComponent.text = $"[Warning] {msg}";
        }

        public void logWarn(string channel, string msg)
        {
            TextComponent.text = $"[Warning][{channel}] {msg}";
        }
    }
}
