using System;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface ITriggerManager
    {
        Task doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg;
    }
    public interface IEventArg
    {

    }
}
