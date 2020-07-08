using System.Collections.Generic;
namespace TouhouCardEngine.Interfaces
{
    public interface ILogger
    {
        bool enable { get; set; }
        List<string> blackList { get; set; }
        void log(string msg);
        void log(string channel, string msg);
        void logWarn(string msg);
        void logWarn(string channel, string msg);
        void logError(string msg);
        void logError(string channel, string msg);
    }
}
