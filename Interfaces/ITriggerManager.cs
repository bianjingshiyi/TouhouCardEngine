using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface ITriggerManager : IDisposable
    {
        string getName(IEventArg eventArg);
        string getName<T>() where T : IEventArg;
        void register(string eventName, ITrigger trigger);
        void registerDelayed(string eventName, ITrigger trigger);
        bool remove(string eventName, ITrigger trigger);
        void register<T>(ITrigger<T> trigger) where T : IEventArg;
        bool remove<T>(ITrigger<T> trigger) where T : IEventArg;
        ITrigger<T>[] getTriggers<T>() where T : IEventArg;
        Task<T> doEvent<T>(T eventArg) where T : IEventArg;
        string getNameBefore<T>() where T : IEventArg;
        string getNameAfter<T>() where T : IEventArg;
        void registerBefore<T>(ITrigger<T> trigger) where T : IEventArg;
        void registerAfter<T>(ITrigger<T> trigger) where T : IEventArg;
        bool removeBefore<T>(ITrigger<T> trigger) where T : IEventArg;
        bool removeAfter<T>(ITrigger<T> trigger) where T : IEventArg;
        ITrigger<T>[] getTriggersBefore<T>() where T : IEventArg;
        ITrigger<T>[] getTriggersAfter<T>() where T : IEventArg;
        Task<T> doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg;
        Task<T> doEvent<T>(string[] eventNames, T eventArg, object[] args) where T : IEventArg;
        Task<T> doEvent<T>(string[] beforeNames, string[] afterNames, T eventArg, Func<T, Task> action, object[] args) where T : IEventArg;
        event Action<IEventArg> onEventBefore;
        event Action<IEventArg> onEventAfter;
        IEventArg currentEvent { get; }
        IEventArg[] getEventChain();
        IEventArg[] getRecordedEvents(bool includeCanceled = false, bool includeUncompleted = false);
        EventRecord[] getEventRecords(bool includeCanceled = false, bool includeUncompleted = false);
        EventRecord getEventRecord(IEventArg eventArg, bool includeCanceled = false, bool includeUncompleted = false);
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
        bool isCompleted { get; set; }
        int repeatTime { get; set; }
        int flowNodeId { get; set; }
        Func<IEventArg, Task> action { get; set; }
        IEventArg parent { get; }
        EventState state { get; set; }
        EventRecord record { get; set; }
        void setParent(IEventArg parent);
        IEventArg[] getAllChildEvents();
        IEventArg[] getChildEvents(EventState state);
        /// <summary>
        /// 获取环境变量
        /// </summary>
        /// <param name="varName">变量名</param>
        /// <returns>环境变量值</returns>
        object getVar(string varName);
        /// <summary>
        /// 设置环境变量
        /// </summary>
        /// <param name="varName">环境变量名</param>
        /// <param name="value">环境变量值</param>
        void setVar(string varName, object value);
        string[] getVarNames();
        void Record(IGame game, EventRecord record);
    }
    public interface ICardEventArg : IEventArg
    {
        ICard getCard();
    }
    public interface IMassCardEventArg : IEventArg
    {
        ICard[] getCards();
    }
}
