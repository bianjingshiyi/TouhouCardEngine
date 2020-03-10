using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface ITriggerManager
    {
        void register(string eventName, ITrigger trigger);
        bool remove(string eventName, ITrigger trigger);
        ITrigger[] getTriggers(string eventName);
        Task doEvent(string[] eventNames, object[] args);
        string getName<T>();
        void register<T>(ITrigger<T> trigger) where T : IEventArg;
        bool remove<T>(ITrigger<T> trigger) where T : IEventArg;
        ITrigger<T>[] getTriggers<T>() where T : IEventArg;
        Task doEvent<T>(T eventArg) where T : IEventArg;
        string getNameBefore<T>();
        string getNameAfter<T>();
        void registerBefore<T>(ITrigger<T> trigger) where T : IEventArg;
        void registerAfter<T>(ITrigger<T> trigger) where T : IEventArg;
        bool removeBefore<T>(ITrigger<T> trigger) where T : IEventArg;
        bool removeAfter<T>(ITrigger<T> trigger) where T : IEventArg;
        ITrigger<T>[] getTriggersBefore<T>() where T : IEventArg;
        ITrigger<T>[] getTriggersAfter<T>() where T : IEventArg;
        Task doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg;
        Task doEvent<T>(string[] eventNames, T eventArg, object[] args) where T : IEventArg;
        Task doEvent<T>(string[] beforeNames, string[] afterNames, T eventArg, Func<T, Task> action, object[] args) where T : IEventArg;
        IEventArg getEventArg(string[] eventNames, object[] args);
    }
    public interface ITrigger
    {
        bool checkCondition(object[] args);
        Task invoke(object[] args);
    }
    public interface ITrigger<T> : ITrigger where T : IEventArg
    {
    }
    public interface IEventArg
    {
        bool isCanceled { get; set; }
        int repeatTime { get; set; }
        Func<IEventArg, Task> action { get; set; }
    }
}
