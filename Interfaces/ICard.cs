namespace TouhouCardEngine.Interfaces
{
    public interface IGame
    {
        ITriggerManager triggers { get; }
        IAnswerManager answers { get; }
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
    }
    public interface ICardDefine
    {
        int id { get; }
        IEffect[] effects { get; }
    }
}
