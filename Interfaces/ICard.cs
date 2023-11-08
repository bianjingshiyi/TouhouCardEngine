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
        int responseRandomInt(int min, int max);
        CardSnapshot snapshotCard(Card card);
        Task runActions(Flow flow, ControlInput port);
        Task runActions(Flow flow, ControlOutput port);
        Task<object> getValue(Flow flow, ValueInput port);
        Task<T> getValue<T>(Flow flow, ValueInput port);
        Task<object> getValue(Flow flow, ValueOutput port);
        Task<T> getValue<T>(Flow flow, ValueOutput port);
        ActionDefine getActionDefine(ActionReference defineRef);
    }
    public interface IPlayer
    {
        int id { get; }
    }
    public interface ICard : ICardData
    {
        T getProp<T>(IGame game, string propName, bool raw);
        object getProp(IGame game, string propName, bool raw);
        Task<IAddModiEventArg> addModifier(IGame game, PropModifier modifier);
        Task<IRemoveModiEventArg> removeModifier(IGame game, PropModifier modifier);
        Task<IPropChangeEventArg> setProp(IGame game, string propName, object value);
        void enableEffect(IBuff buff, IEffect effect);
        void disableEffect(IBuff buff, IEffect effect);
        bool isEffectEnabled(IBuff buff, IEffect effect);
    }
    public interface ICardData
    {
        int id { get; }
        Player owner { get; }
        CardDefine define { get; }
        T getProp<T>(IGame game, string propName);
        object getProp(IGame game, string propName);
        Buff[] getBuffs();
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
}
