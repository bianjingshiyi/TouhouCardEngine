using System;

namespace TouhouCardEngine.Interfaces
{
    public interface IServerManager
    {
        void start(int port);
        /// <summary>
        /// 向指定ID的客户端发送数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="obj"></param>
        void send(int id, object obj);
        /// <summary>
        /// 向所有客户端广播数据
        /// </summary>
        /// <param name="obj"></param>
        void broadcast(object obj);
        event Action<int, object> onReceive;
    }
}
