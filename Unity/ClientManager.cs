using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace TouhouCardEngine
{
    public class ClientManager : MonoBehaviour, IClientManager, INetEventListener
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
        NetManager net { get; set; } = null;
        public bool isRunning
        {
            get { return net != null ? net.IsRunning : false; }
        }
        NetPeer host { get; set; } = null;
        public Interfaces.ILogger logger { get; set; } = null;
        [SerializeField]
        int _id = -1;
        public int id
        {
            get { return _id; }
            private set { _id = value; }
        }
        protected void Awake()
        {
            net = new NetManager(this)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true,
                DisconnectTimeout = (int)(timeout * 1000),
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
        TaskCompletionSource<object> tcs { get; set; } = null;
        public async Task<int> join(string ip, int port)
        {
            if (tcs != null)
                throw new InvalidOperationException("客户端正在执行另一项操作");
            NetDataWriter writer = new NetDataWriter();
            host = net.Connect(new IPEndPoint(IPAddress.Parse(ip), port), writer);
            logger?.log("客户端正在连接主机" + ip + ":" + port);
            tcs = new TaskCompletionSource<object>();
            _ = Task.Run(async () =>
            {
                await Task.Delay(net.DisconnectTimeout);
                if (tcs != null)
                    tcs.SetException(new TimeoutException("客户端连接主机" + ip + ":" + port + "超时"));
            });
            await tcs.Task;
            int result = tcs.Task.IsCompleted ? (int)tcs.Task.Result : -1;
            tcs = null;
            return result;
        }
        public void OnPeerConnected(NetPeer peer)
        {
            if (peer == host)
                logger?.log("客户端连接到主机" + peer.EndPoint);
        }
        public event Action onConnected;
        public void send(object obj)
        {
            send(obj, PacketType.sendRequest);
        }
        void send(object obj, PacketType packetType)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (tcs != null)
                throw new InvalidOperationException("客户端正在执行另一项操作");
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)packetType);
            writer.Put(id);
            writer.Put(obj.GetType().FullName);
            writer.Put(obj.ToJson());
            host.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        public async Task<T> send<T>(T obj)
        {
            return await send<T>(obj, PacketType.sendRequest);
        }
        async Task<T> send<T>(T obj, PacketType packetType)
        {
            send(obj as object, packetType);
            tcs = new TaskCompletionSource<object>();
            _ = Task.Run(async () =>
            {
                await Task.Delay(net.DisconnectTimeout);
                if (tcs != null)
                    tcs.SetException(new TimeoutException("客户端" + id + "向主机" + host.EndPoint + "发送数据响应超时：" + obj));
            });
            await tcs.Task;
            T result = tcs.Task.IsCompleted ? (T)tcs.Task.Result : default;
            tcs = null;
            return result;
        }
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.connectResponse:
                    this.id = reader.GetInt();
                    logger?.log("客户端连接主机成功，获得ID：" + this.id);
                    tcs.SetResult(this.id);
                    onConnected?.Invoke();
                    break;
                case PacketType.sendResponse:
                    int id = reader.GetInt();
                    string typeName = reader.GetString();
                    string json = reader.GetString();
                    logger?.log("客户端" + this.id + "收到主机转发的来自客户端" + id + "的数据：（" + typeName + "）" + json);
                    Type objType = Type.GetType(typeName);
                    if (objType == null)
                    {
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            objType = assembly.GetType(typeName);
                            if (objType != null)
                                break;
                        }
                    }
                    object obj = BsonSerializer.Deserialize(json, objType);
                    if (tcs != null)
                        tcs.SetResult(obj);
                    onReceive?.Invoke(id, obj);
                    break;
                case PacketType.joinResponse:
                    var info = parseRoomInfo(peer.EndPoint, reader);
                    if (info != null) onJoinRoom?.Invoke(info);
                    logger?.log($"客户端 {this.id} 收到了主机的加入响应");
                    break;
                case PacketType.roomInfoUpdate:
                    info = parseRoomInfo(peer.EndPoint, reader);
                    if (info != null) onRoomInfoUpdate?.Invoke(info);
                    logger?.log($"客户端 {this.id} 收到了主机的房间更新信息。当前房间人数: {info?.playerList?.Count}");
                    break;

                default:
                    logger?.log("Warning", "客户端未处理的数据包类型：" + type);
                    break;
            }
        }
        public event Action<int, object> onReceive;
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void disconnect()
        {
            if (tcs != null)
            {
                tcs.SetCanceled();
                tcs = null;
            }
            if (host != null)
            {
                host.Disconnect();
                host = null;
            }
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.log("客户端" + id + "与主机断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
            if (tcs != null)
            {
                tcs.SetCanceled();
                tcs = null;
            }
            host = null;
            onDisconnect?.Invoke();
            onQuitRoom?.Invoke();
        }
        public event Action onDisconnect;
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            logger?.log("Error", "客户端" + id + "与" + endPoint + "发生网络异常：" + socketError);
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            throw new NotImplementedException();
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            switch (messageType)
            {
                case UnconnectedMessageType.DiscoveryResponse:
                    if (reader.GetInt() == (int)PacketType.discoveryResponse)
                    {
                        logger?.log($"客户端找到主机，{remoteEndPoint.Address}:{remoteEndPoint.Port}");
                        var roomInfo = parseRoomInfo(remoteEndPoint, reader);
                        if (roomInfo != null) onRoomFound?.Invoke(roomInfo);
                    }
                    else
                    {
                        logger?.log("消息类型不匹配");
                    }
                    break;
                default:
                    break;
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
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.discoveryRequest);
            // todo: 来一个可以编辑的端口
            net.SendDiscoveryRequest(writer, port);
        }
        RoomInfo parseRoomInfo(IPEndPoint remoteEndPoint, NetPacketReader reader)
        {
            var type = reader.GetString();
            var json = reader.GetString();
            if (type != typeof(List<RoomPlayerInfo>).FullName)
            {
                logger?.log($"主机房间信息类型错误，收到了 {type}");
                return null;
            }
            return new RoomInfo()
            {
                ip = remoteEndPoint.Address.ToString(),
                port = remoteEndPoint.Port,
                playerList = BsonSerializer.Deserialize<List<RoomPlayerInfo>>(json)
            };
        }
        public event Action<RoomInfo> onRoomFound;
        /// <summary>
        /// 向目标房间请求新的房间信息，如果目标房间已经不存在了，那么会返回空，否则返回更新的房间信息。
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        public Task<RoomInfo> checkRoomInfo(RoomInfo roomInfo)
        {
            throw new NotImplementedException();
        }
        public event Action onQuitRoom;
        public event Action<RoomInfo> onJoinRoom;
        /// <summary>
        /// 加入指定房间，你必须告诉房主你的个人信息。
        /// </summary>
        /// <param name="room"></param>
        /// <param name="playerInfo"></param>
        /// <returns></returns>
        public async Task joinRoom(RoomInfo room, RoomPlayerInfo playerInfo)
        {
            var id = await join(room.ip, room.port);
            if (id == -1)
                throw new TimeoutException();
            playerInfo.id = id;
            send(playerInfo as object, PacketType.joinRequest);
        }
        /// <summary>
        /// 当前所在房间信息，如果不在任何房间中则为空。
        /// </summary>
        public RoomInfo roomInfo
        {
            get { throw new NotImplementedException(); }
        }
        public event Action<RoomInfo> onRoomInfoUpdate;
        public void quitRoom()
        {
            host.Disconnect();
        }
        #endregion
    }
}
