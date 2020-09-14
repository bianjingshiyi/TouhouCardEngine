using System;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Threading;
using System.Linq;
using NitoriNetwork.Common;

namespace TouhouCardEngine
{
    public class ClientManager : MonoBehaviour, IClientManager
    {
        [SerializeField]
        int _port = 9050;
        public int port
        {
            get { return _port; }
        }
        [SerializeField]
        float _timeout = 30;
        public float timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                if (net != null)
                    net.DisconnectTimeout = (int)(value * 1000);
            }
        }
        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Host和Client都有NetManager是因为在同一台电脑上如果要开Host和Client进行网络对战的话，就必须得开两个端口进行通信，出于这样的理由
        /// Host和Client都必须拥有一个NetManager实例并使用不同的端口。
        /// </remarks>
        NetManager net { get => client.net; set => client.net = value; }

        ClientNetworking client = new ClientNetworking();

        public bool isRunning
        {
            get { return net != null ? net.IsRunning : false; }
        }

        Interfaces.ILogger _logger = null;

        public Interfaces.ILogger logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
                client.logger = new NetworkingLoggerAdapter(value);
            }
        }
        /// <summary>
        /// 客户端ID
        /// </summary>
        public int id => client.id;

        protected void Awake()
        {
            net = new NetManager(client)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true,
                DisconnectTimeout = (int)(timeout * 1000),
                IPv6Enabled = true,
            };
        }
        protected void Start()
        {
            if (autoStart)
            {
                if (port > 0)
                    start(port);
                else
                    start();
            }
        }
        protected void Update()
        {
            net.PollEvents();
        }
        public void start()
        {
            if (!net.IsRunning)
            {
                net.Start();
                _port = net.LocalPort;
                logger?.log("客户端初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "客户端已经初始化，本地端口：" + net.LocalPort);
        }
        public void start(int port)
        {
            if (!net.IsRunning)
            {
                net.Start(port);
                _port = net.LocalPort;
                logger?.log("客户端初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "客户端已经初始化，本地端口：" + net.LocalPort);
        }

        public Task<int> join(string ip, int port)
        {
            return client.join(ip, port);
        }

        /// <summary>
        /// 加入服务器房间
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="session"></param>
        /// <param name="roomID"></param>
        /// <returns></returns>
        public Task<int> joinServer(string ip, int port, string session, string roomID)
        {
            return client.join(ip, port, session, roomID);
        }

        public event Func<Task> onConnected
        {
            add
            {
                client.onConnected += value;
            }
            remove
            {
                client.onConnected -= value;
            }
        }

        public Task send(object obj)
        {
            return client.send(obj);
        }
        public async Task<T> send<T>(T obj)
        {
            return await client.send<T>(obj);
        }

        public Task<T> invokeHost<T>(RPCRequest request)
        {
            return client.invokeHost<T>(request);
        }

        /// <summary>
        /// 返回值是void的Request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<object> invokeHost(RPCRequest request)
        {
            return invokeHost<object>(request);
        }

        public void addInvokeTarget(object obj)
        {
            client.addInvokeTarget(obj);
        }

        public event Func<int, object, Task> onReceive
        {
            add
            {
                client.onReceive += value;
            }
            remove
            {
                client.onReceive -= value;
            }
        }

        public void disconnect()
        {
            client.disconnect();
        }

        public event Action onDisconnect
        {
            add
            {
                client.onDisconnect += value;
            }
            remove
            {
                client.onDisconnect -= value;
            }
        }

        public void stop()
        {
            net.Stop();
        }
        #region Room
        /// <summary>
        /// 局域网发现是Host收到了给回应，你不可能知道Host什么时候回应，也不知道局域网里有多少个可能会回应的Host，所以这里不返回任何东西。
        /// </summary>
        /// <param name="port">搜索端口。默认9050</param>
        public void findRoom(int port = 9050)
        {
            client.findRoom(port);
        }

        public event Action<RoomInfo> onRoomFound
        {
            add => client.onRoomFound += value;
            remove => client.onRoomFound -= value;
        }

        /// <summary>
        /// 向目标房间请求新的房间信息，如果目标房间已经不存在了，那么会返回空，否则返回更新的房间信息。
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        public Task<RoomInfo> checkRoomInfo(RoomInfo roomInfo)
        {
            return client.checkRoomInfo(roomInfo);
        }
        public event Action onQuitRoom
        {
            add => client.onQuitRoom += value;
            remove => client.onQuitRoom -= value;
        }
        public event Action<RoomInfo> onJoinRoom
        {
            add => client.onJoinRoom += value;
            remove => client.onJoinRoom -= value;
        }
        /// <summary>
        /// 加入指定房间，你必须告诉房主你的个人信息。
        /// </summary>
        /// <param name="room"></param>
        /// <param name="playerInfo"></param>
        /// <returns></returns>
        public async Task joinRoom(RoomInfo room, RoomPlayerInfo playerInfo)
        {
            await client.joinRoom(room, playerInfo);
        }

        /// <summary>
        /// 请求更新
        /// </summary>
        /// <param name="playerInfo"></param>
        /// <returns></returns>
        public async Task updatePlayerInfo(RoomPlayerInfo playerInfo)
        {
            await client.updatePlayerInfo(playerInfo);
        }

        /// <summary>
        /// 当前所在房间信息，如果不在任何房间中则为空。
        /// </summary>
        public RoomInfo roomInfo => client.roomInfo;

        public event ClientNetworking.RoomInfoUpdateDelegate onRoomInfoUpdate
        {
            add => client.onRoomInfoUpdate += value;
            remove => client.onRoomInfoUpdate -= value;
        }

        public void quitRoom()
        {
            client.quitRoom();
        }
        #endregion
    }

    public class RPCHelper
    {
        public static RPCRequest GameStart()
        {
            return new RPCRequest(typeof(void), "gameStart");
        }

        public static RPCRequest RoomPropSet(string name, object value)
        {
            return new RPCRequest(typeof(void), "setRoomProp", name, value);
        }

        public static RPCRequest RemovePlayer(int playerID)
        {
            return new RPCRequest(typeof(void), "removePlayer", playerID);
        }
    }
}
