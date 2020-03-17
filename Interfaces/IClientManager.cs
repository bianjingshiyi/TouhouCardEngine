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
        event Action onConnected;
        /// <summary>
        /// 向服务端发送数据
        /// </summary>
        /// <param name="obj"></param>
        void send(object obj);
        Task<T> send<T>(T obj);

        event Action<int, object> onReceive;
        void disconnect();
        event Action onDisconnect;
    }
}
