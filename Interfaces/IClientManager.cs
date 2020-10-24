using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
namespace TouhouCardEngine.Interfaces
{
    public interface IClientManager
    {
        int port { get; }
        float timeout { get; }
        int id { get; }
        void start();
        void start(int port);
        Task<int> join(string ip, int port);
        event Func<Task> onConnected;
        Task<T> send<T>(T obj);

        event Func<int, object, Task> onReceive;
        void disconnect();
        event Action<DisconnectType> onDisconnect;
    }
}
