namespace TouhouCardEngine.Interfaces
{
    public interface ILogger
    {
        void log(string msg);
        void log(string channel, string msg);
    }
}
