using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IRequest
    {
        int playerId { get; set; }
    }
    public interface IResponse
    {
        int playerId { get; set; }
    }
    public interface IRequestManager
    {
        IResponse ask(IRequest request);
        IResponse askAll(IRequest request);
        void answer(IResponse response);
    }
    public interface IEventManager
    {
        Task doEvent<T>(T eventArg, Func<T, Task> action) where T : IEventArg;
    }
    public interface IEventArg
    {

    }
}
