using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface ITriggerManager
    {
        string getName<T>() where T : IEventArg;
        void register(string eventName, ITrigger trigger);
        bool remove(string eventName, ITrigger trigger);
        void register<T>(ITrigger<T> trigger) where T : IEventArg;
        bool remove<T>(ITrigger<T> trigger) where T : IEventArg;
        ITrigger<T>[] getTriggers<T>() where T : IEventArg;
        Task doEvent<T>(T eventArg) where T : IEventArg;
        string getNameBefore<T>() where T : IEventArg;
        string getNameAfter<T>() where T : IEventArg;
        void registerBefore<T>(ITrigger<T> trigger) where T : IEventArg;
        void registerAfter<T>(ITrigger<T> trigger) where T : IEventArg;
        bool removeBefore<T>(ITrigger<T> trigger) where T : IEventArg;
        bool removeAfter<T>(ITrigger<T> trigger) where T : IEventArg;
        ITrigger<T>[] getTriggersBefore<T>() where T : IEventArg;
        ITrigger<T>[] getTriggersAfter<T>() where T : IEventArg;
        Task doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg;
        Task doEvent<T>(string[] eventNames, T eventArg, object[] args) where T : IEventArg;
        Task doEvent<T>(string[] beforeNames, string[] afterNames, T eventArg, Func<T, Task> action, object[] args) where T : IEventArg;
        event Action<IEventArg> onEventBefore;
        event Action<IEventArg> onEventAfter;
        IEventArg currentEvent { get; }
        IEventArg[] getEventChain();
        IEventArg[] getRecordedEvents();
    }
    public interface ITrigger
    {
        int compare(ITrigger other, IEventArg arg);
        bool checkCondition(IEventArg arg);
        Task invoke(IEventArg arg);
    }
    public interface ITrigger<T> : ITrigger where T : IEventArg
    {
        int compare(ITrigger<T> other, T arg);
        bool checkCondition(T arg);
        Task invoke(T arg);
    }
    public interface IEventArg
    {
        string[] beforeNames { get; set; }
        string[] afterNames { get; set; }
        object[] args { get; set; }
        bool isCanceled { get; set; }
        int repeatTime { get; set; }
        Func<IEventArg, Task> action { get; set; }
        void addChildEvent(IEventArg eventArg);
        IEventArg[] getChildEvents();
        IEventArg[] children { get; }
    }
}
