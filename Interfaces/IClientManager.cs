using System;

namespace TouhouCardEngine.Interfaces
{
    public interface IClientManager
    {
        int id { get; }
        void start();
        void connect(string ip, int port);
        event Action onConnected;
        /// <summary>
        /// 向服务端发送数据
        /// </summary>
        /// <param name="obj"></param>
        void send(object obj);
        event Action<int, object> onReceive;
        void disconnect();
        event Action onDisconnect;
    }
}
