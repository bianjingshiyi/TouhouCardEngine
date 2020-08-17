using System.Threading.Tasks;
namespace TouhouCardEngine.Interfaces
{
    public interface IGame
    {
        ITriggerManager triggers { get; }
        IAnswerManager answers { get; }
        ITimeManager time { get; }
        ILogger logger { get; }
        int randomInt(int min, int max);
    }
    public interface IPlayer
    {
        int id { get; }
    }
    public interface ICard
    {
        int id { get; }
        ICardDefine define { get; }
        Task<IAddModiEventArg> addModifier(IGame game, PropModifier modifier);
        Task<IRemoveModiEventArg> removeModifier(IGame game, PropModifier modifier);
        T getProp<T>(IGame game, string propName);
        Task<ISetPropEventArg> setProp<T>(IGame game, string propName, T value);
    }
    public interface ISetPropEventArg : IEventArg
    {
        ICard card { get; }
        string propName { get; }
        object value { get; }
    }
    public interface IAddModiEventArg : IEventArg
    {
        ICard card { get; }
        IPropModifier modifier { get; }
    }
    public interface IRemoveModiEventArg : IEventArg
    {
        ICard card { get; }
        IPropModifier modifier { get; }
    }
    public interface IPropModifier
    {
    }
    public interface IBuff
    {
        int instanceID { get; set; }
    }
    public interface ICardDefine
    {
        int id { get; }
        IEffect[] effects { get; }
    }
}
