using System;
using System.Threading.Tasks;
namespace TouhouCardEngine.Interfaces
{
    public interface IGame : IDisposable
    {
        ITriggerManager triggers { get; }
        IAnswerManager answers { get; }
        ITimeManager time { get; }
        Shared.ILogger logger { get; }
        int randomInt(int min, int max);
        Task doActionsAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode action);
        Task<object[]> doActionAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode action);
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
        object getProp(IGame game, string propName);
        Task<IPropChangeEventArg> setProp(IGame game, string propName, object value);
    }
    public interface IPropChangeEventArg : IEventArg
    {
        ICard card { get; }
        string propName { get; }
        object beforeValue { get; }
        object value { get; }
    }
    public interface IAddModiEventArg : IEventArg
    {
        ICard card { get; }
        IPropModifier modifier { get; }
        object valueBefore { get; }
        object valueAfter { get; }
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
