namespace TouhouCardEngine.Interfaces
{
    public interface ILogger
    {
        bool enable { get; set; }
        void log(string msg);
        void log(string channel, string msg);
    }
}
