using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
namespace TouhouCardEngine.Interfaces
{
    public interface IClientManager: IClient
    {
        int port { get; }
        float timeout { get; }

        void start();
        void start(int port);
        Task<int> join(string ip, int port);
        event Func<Task> onConnected;

        void disconnect();
        event Action<DisconnectType> onDisconnect;
    }

    public interface IClient
    {
        int id { get; }

        Task<T> send<T>(T obj);

        event Func<int, object, Task> onReceive;
    }
}
