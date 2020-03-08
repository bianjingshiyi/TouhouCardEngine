using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface ITriggerManager
    {
        void register(string eventName, ITrigger trigger);
        ITrigger[] getTriggers(string eventName);
        Task doEvent(string eventName, object[] args);
        Task doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg;
    }
    public interface ITrigger
    {
        Task invoke(object[] args);
    }
    public interface IEventArg
    {
        string name { get; }
    }
}
