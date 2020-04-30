namespace TouhouCardEngine.Interfaces
{
    public interface IGame
    {
        ITriggerManager triggers { get; }
        IAnswerManager answers { get; }
        ITimeManager time { get; }
        ILogger logger { get; }
    }
    public interface IPlayer
    {
        int id { get; }
    }
    public interface ICard
    {
        int id { get; }
        ICardDefine define { get; }
        T getProp<T>(string propName);
        void setProp<T>(string propName, T value);
    }
    public interface ICardDefine
    {
        int id { get; }
        IEffect[] effects { get; }
    }
}
