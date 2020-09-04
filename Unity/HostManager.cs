using System;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Reflection;
using System.Threading.Tasks;
using NitoriNetwork.Common;

namespace TouhouCardEngine
{
    public class HostManager : MonoBehaviour, IHostManager, INetEventListener
    {
        [SerializeField]
        int _port = 9050;
        public int port
        {
            get { return _port; }
        }
        public string address
        {
            get { return ip + ":" + port; }
        }
        public string ip => Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

        public string[] ips => Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).Select(i => i.ToString()).ToArray();

        public string[] addresses => ips.Select(i => i + ":" + port).ToArray();

        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        NetManager net { get; set; }
        public bool isRunning
        {
            get { return net != null ? net.IsRunning : false; }
        }
        Dictionary<int, NetPeer> clientDic { get; } = new Dictionary<int, NetPeer>();
        public Interfaces.ILogger logger { get; set; } = null;
        [SerializeField]
        float _timeout = 3;
        /// <summary>
        /// 超时时间，以毫秒计
        /// </summary>
        public float timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
        protected void Awake()
        {
            net = new NetManager(this)
            {
                AutoRecycle = true,
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = true
            };
            room = null;

            // 注册内置的RPC方法
            rpcMethodRegister();
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
                logger?.log("主机初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "主机已经初始化，本地端口：" + net.LocalPort);
        }
        public void start(int port)
        {
            if (!net.IsRunning)
            {
                net.Start(port);
                _port = net.LocalPort;
                logger?.log("主机初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "主机已经初始化，本地端口：" + net.LocalPort);
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            NetPeer peer = request.Accept();
            clientDic.Add(peer.Id, peer);
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.connectResponse);
            writer.Put(peer.Id);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.log("主机同意" + request.RemoteEndPoint + "的连接请求");
        }
        public void OnPeerConnected(NetPeer peer)
        {
            logger?.log("主机被客户端" + peer.Id + "连接");
            onClientConnected?.Invoke(peer.Id);
        }
        public event Action<int> onClientConnected;

        #region RPC
        RPCExecutor rpcExecutor = new RPCExecutor();

        void rpcMethodRegister()
        {
            rpcExecutor.AddSingleton(this);

            rpcExecutor.AddTargetMethod<HostManager>(x => x.setRoomProp(null, "", ""));
            rpcExecutor.AddTargetMethod<HostManager>(x => x.gameStart(null));
            rpcExecutor.AddTargetMethod<HostManager>(x => x.removePlayer(null, 0));
        }

        /// <summary>
        /// RPC调用指定Client上的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">客户端ID</param>
        /// <param name="method">方法名称</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        [Obsolete("use invoke(id, RPCRequest) instead")]
        public Task<T> invoke<T>(int id, string method, params object[] args)
        {
            var request = new RPCRequest<T>(method, args);
            return invoke<T>(id, request);
        }

        /// <summary>
        /// RPC调用指定Client上的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<T> invoke<T>(int id, RPCRequest request)
        {
            NetPeer peer = clientDic[id];
            InvokeOperation<T> invoke = new InvokeOperation<T>(nameof(invoke), peer.Id, request);

            opList.Add(invoke);

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.invokeRequest);
            writer.Put(invoke.id);
            request.Write(writer);

            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.log("主机远程调用客户端" + id + ": " + request);
            _ = invokeTimeout(invoke);
            return invoke.task;
        }

        /// <summary>
        /// RPC调用指定Client上的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="IdArray">Client列表</param>
        /// <param name="method">方法名</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        [Obsolete("Use invokeAll(rpcRequest) instead")]
        public async Task<Dictionary<int, T>> invokeAll<T>(int[] IdArray, string method, params object[] args)
        {
            Dictionary<int, Task<T>> taskDic = new Dictionary<int, Task<T>>();
            foreach (int id in IdArray)
            {
                taskDic.Add(id, invoke<T>(id, method, args));
            }
            await Task.Run(() => Task.WaitAll(taskDic.Values.ToArray()));
            return taskDic.ToDictionary(p => p.Key, p => p.Value.Result);
        }

        /// <summary>
        /// RPC调用指定Client上的方法
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="IdArray">Client列表</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Dictionary<int, T>> invokeAll<T>(int[] IdArray, RPCRequest request)
        {
            Dictionary<int, Task<T>> taskDic = new Dictionary<int, Task<T>>();
            foreach (int id in IdArray)
            {
                taskDic.Add(id, invoke<T>(id, request));
            }
            await Task.Run(() => Task.WaitAll(taskDic.Values.ToArray()));
            return taskDic.ToDictionary(p => p.Key, p => p.Value.Result);
        }

        /// <summary>
        /// 超时
        /// </summary>
        /// <param name="invoke"></param>
        /// <returns></returns>
        async Task invokeTimeout<T>(InvokeOperation<T> invoke)
        {
            await Task.Delay((int)(timeout * 1000));
            if (opList.Remove(invoke))
            {
                logger?.log("主机请求客户端" + invoke.pid + "远程调用" + invoke.id + "超时");
                invoke.setCancel();
            }
        }

        OperationList opList = new OperationList();

        /// <summary>
        /// Invoke设置结果
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        void invokeResponseHandler(int peerID, NetPacketReader reader)
        {
            try
            {
                var result = OperationResultExt.ParseInvoke(reader);
                if (!opList.SetResult(result))
                {
                    logger?.log("主机接收到客户端" + peerID + "未被请求或超时的远程调用" + result.requestID);
                }
                else
                {
                    logger?.log("主机接收到客户端" + peerID + "的调用结果");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Invoke操作
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        void invokeRequestHandlder(NetPeer peer, NetPacketReader reader)
        {
            try
            {
                OperationResult result = new OperationResult();
                result.requestID = reader.GetInt();

                try
                {
                    var request = RPCRequest.Parse(reader);
                    logger?.log("主机执行来自客户端" + peer.Id + "的远程调用" + result.requestID + "，" + request);

                    RoomPlayerInfo player = room.playerList.Where(p => p.RoomID == peer.Id).FirstOrDefault();
                    try
                    {
                        if (!rpcExecutor.TryInvoke(request, new object[] { player }, out result.obj))
                        {
                            throw new MissingMethodException("无法找到方法：" + request);
                        }
                    }
                    catch (Exception invokeException)
                    {
                        result.obj = invokeException;
                        logger?.log("主机执行来自客户端" + peer.Id + "的远程调用" + result.requestID + "{" + request + ")}发生异常：" + invokeException);
                    }
                }
                catch (Exception e)
                {
                    result.obj = e;
                    logger?.log("主机执行来自客户端" + peer.Id + "的远程调用" + result.requestID + "发生异常：" + e);
                }

                NetDataWriter writer = new NetDataWriter();
                writer.Put((int)PacketType.invokeResponse);
                result.Write(writer);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region packet_handlers

        /// <summary>
        /// request的转发器，会转发给所有的client
        /// </summary>
        /// <param name="reader"></param>
        private void sendRequestForwarder(NetPacketReader reader)
        {
            try
            {
                int rid = reader.GetInt();
                int id = reader.GetInt();
                string typeName = reader.GetString();
                string json = reader.GetString();
                NetDataWriter writer = new NetDataWriter();
                writer.Put((int)PacketType.sendResponse);
                writer.Put(rid);
                writer.Put(id);
                writer.Put(typeName);
                writer.Put(json);
                logger?.log("主机收到来自客户端" + id + "的数据：（" + typeName + "）" + json);
                foreach (var client in clientDic.Values)
                {
                    client.Send(writer, DeliveryMethod.ReliableOrdered);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.invokeResponse:
                    invokeResponseHandler(peer.Id, reader);
                    break;
                case PacketType.invokeRequest:
                    invokeRequestHandlder(peer, reader);
                    break;
                case PacketType.sendRequest:
                    sendRequestForwarder(reader);
                    break;
                case PacketType.joinRequest:
                    joinRoomRequestHandler(peer, reader);
                    break;
                case PacketType.playerInfoUpdateRequest:
                    playerInfoUpdateRequestHandler(peer, reader);
                    break;
                default:
                    logger?.log("Warning", "服务端未处理的数据包类型：" + type);
                    break;
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.log("主机与客户端" + peer.Id + "断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
            // 处理房间问题
            var infos = room?.playerList.Where(c => c.RoomID == peer.Id);
            if (infos != null && infos.Count() > 0)
            {
                var info = infos.First();
                room.playerList.Remove(info);
                onPlayerQuit?.Invoke(info);

                // 通知其他的客户端更新房间信息
                notifyRoomInfoChange(peer.Id);
            }
            clientDic.Remove(peer.Id);
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            logger?.log("Error", "主机与" + endPoint + "发生网络异常：" + socketError);
        }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            switch (messageType)
            {
                // 处理房间发现请求或主机信息更新请求
                case UnconnectedMessageType.Broadcast:
                case UnconnectedMessageType.BasicMessage:
                    if (RoomIsValid && reader.GetInt() == (int)PacketType.discoveryRequest)
                    {
                        logger?.log($"主机房间收到了局域网发现请求或主机信息更新请求");
                        NetDataWriter writer = room.Write(PacketType.discoveryResponse, reader.GetUInt());
                        net.SendUnconnectedMessage(writer, remoteEndPoint);
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
        [SerializeField]
        RoomInfo _room;
        public bool RoomIsValid => room != null && room.id != Guid.Empty;
        public RoomInfo room
        {
            get { return _room; }
            private set { _room = value; }
        }
        public RoomInfo openRoom(RoomInfo roomInfo)
        {
            room = roomInfo;

            if (!net.IsRunning)
            {
                start(roomInfo.port);
            }
            roomInfo.ip = ip;
            roomInfo.port = port;
            return roomInfo;
        }

        public event Action<RoomPlayerInfo> onPlayerJoin;

        /// <summary>
        /// 更新房间信息，会在Host保存最新的房间信息和将更新的房间信息发送给所有的Client
        /// </summary>
        /// <param name="roomInfo"></param>
        [Obsolete("不可以直接调用这个方法更新信息，必须通过Client更新。")]
        public void updateRoomInfo(RoomInfo roomInfo)
        {
            room = roomInfo;

            // 通知其他所有的Client
            notifyRoomInfoChange();
        }

        /// <summary>
        /// [RPC] 设置房间选项
        /// </summary>
        /// <param name="reqPlayer">调用玩家（自动注入）</param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        void setRoomProp(RoomPlayerInfo reqPlayer, string name, object value)
        {
            if (reqPlayer.PlayerID != room.OwnerID)
                throw new PermissionDenyException("非房主不可设置房间选项");

            room.setProp(name, value);
        }

        /// <summary>
        /// [RPC] 移除一个玩家
        /// </summary>
        /// <param name="reqPlayer">调用玩家（自动注入）</param>
        /// <param name="targetPlayerID">要移除的玩家ID</param>
        /// <returns></returns>
        void removePlayer(RoomPlayerInfo reqPlayer, int targetPlayerID)
        {
            if (reqPlayer.PlayerID != room.OwnerID)
                throw new PermissionDenyException("非房主不可移除玩家");

            var player = room.playerList.Where(p => p.PlayerID == targetPlayerID).FirstOrDefault();
            if (player != null)
            {
                net.DisconnectPeer(clientDic[player.RoomID]);
            }
        }
        
        /// <summary>
        /// [RPC] 游戏开始
        /// </summary>
        /// <param name="reqPlayer"></param>
        void gameStart(RoomPlayerInfo reqPlayer)
        {
            if (reqPlayer.PlayerID != room.OwnerID)
                throw new PermissionDenyException("非房主不可开始游戏");

            _ = invokeAll<object>(room.playerList.Select(p => p.RoomID).ToArray(), new RPCRequest(typeof(void), "start"));
        }

        /// <summary>
        /// 通知客户端房间信息变更了
        /// </summary>
        /// <param name="ignoreID">忽略的ID</param>
        private void notifyRoomInfoChange(int ignoreID = -1)
        {
            var writer = room.Write(PacketType.roomInfoUpdate);
            foreach (var client in clientDic.Values)
            {
                if (client.Id != ignoreID)
                {
                    client.Send(writer, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        public event Action<RoomPlayerInfo> onPlayerQuit;
        public void closeRoom()
        {
            net.DisconnectAll();
            room = null;
        }
        /// <summary>
        /// 移除一个玩家
        /// </summary>
        /// <param name="roomID">玩家的RoomID</param>
        [Obsolete("不可以直接调用")]
        public void removePlayer(int roomID)
        {
            foreach (var player in room.playerList)
            {
                if (player.RoomID == roomID)
                {
                    // WARNING: 这里的PlayerInfo的ID是直接用的peerID；如果之后改了这种设定，就要改掉这里
                    net.DisconnectPeer(clientDic[roomID]);
                    // 从网络上断开后，上面的OnDisconnect事件会在房间中删除这个玩家
                }
            }
        }


        /// <summary>
        /// 请求房间信息更新的处理器
        /// </summary>
        /// <param name="reader"></param>
        void playerInfoUpdateRequestHandler(NetPeer peer, NetPacketReader reader)
        {
            if (!RoomIsValid)
                return;

            int rid = reader.GetInt();
            int id = reader.GetInt();
            string typeName = reader.GetString();
            string json = reader.GetString();
            if (TypeHelper.tryGetType(typeName, out Type objType))
            {
                object obj = BsonSerializer.Deserialize(json, objType);
                if (obj is RoomPlayerInfo info)
                {
                    for (int i = 0; i < room.playerList.Count; i++)
                    {
                        if (room.playerList[i].RoomID == id)
                        {
                            room.playerList[i] = info;
                            Debug.Log("更新id:" + id + "的玩家信息: " + info.ToJson());
                        }
                    }

                    // 发送一个空的响应包
                    NetDataWriter writer1 = new NetDataWriter();
                    writer1.Put((int)PacketType.sendResponse);
                    writer1.Put(rid);
                    writer1.Put(id);
                    writer1.Put("".GetType().FullName);
                    writer1.Put("".ToJson());
                    peer.Send(writer1, DeliveryMethod.ReliableOrdered);

                    // 通知所有玩家修改
                    notifyRoomInfoChange();
                }
            }
        }

        /// <summary>
        /// 加入房间请求处理器
        /// </summary>
        void joinRoomRequestHandler(NetPeer peer, NetPacketReader reader)
        {
            try
            {
                if (!RoomIsValid)
                    return;
                int rid = reader.GetInt();
                int id = reader.GetInt();
                string typeName = reader.GetString();
                string json = reader.GetString();
                if (TypeHelper.tryGetType(typeName, out Type objType))
                {
                    object obj = BsonSerializer.Deserialize(json, objType);
                    if (obj is RoomPlayerInfo info)
                    {
                        if (room.playerList.Where(e => e.PlayerID == info.PlayerID).Count() > 0)
                        {
                            logger?.log($"主机房间收到了客户端 {info.name} 的加入请求，ID 重复！");
                            return;
                        }

                        room.playerList.Add(info);
                        onPlayerJoin?.Invoke(info);
                        logger?.log($"主机房间收到了客户端 {info.name} 的加入请求，当前人数 {room.playerList.Count}");

                        // 接受加入，返回房间信息
                        peer.Send(room.Write(PacketType.joinResponse), DeliveryMethod.ReliableOrdered);

                        // 其他的更新房间信息
                        notifyRoomInfoChange(peer.Id);
                    }
                    else
                        logger?.log($"主机房间信息类型错误，收到了 {typeName}");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion
    }
    enum PacketType
    {
        /// <summary>
        /// 连接请求的响应
        /// </summary>
        connectResponse,
        /// <summary>
        /// Client发送游戏数据包
        /// </summary>
        sendRequest,
        /// <summary>
        /// Host收到后转发游戏数据包
        /// </summary>
        sendResponse,
        /// <summary>
        /// RPC请求
        /// </summary>
        invokeRequest,
        /// <summary>
        /// RPC请求结果
        /// </summary>
        invokeResponse,
        /// <summary>
        /// 局域网发现请求
        /// </summary>
        discoveryRequest,
        /// <summary>
        /// 局域网发现响应
        /// </summary>
        discoveryResponse,
        /// <summary>
        /// 加入房间请求
        /// </summary>
        joinRequest,
        /// <summary>
        /// 加入房间响应
        /// </summary>
        joinResponse,
        /// <summary>
        /// 房间信息更新事件
        /// </summary>
        roomInfoUpdate,
        /// <summary>
        /// 更新房间信息请求
        /// </summary>
        playerInfoUpdateRequest
    }

    static class RoomInfoHelper
    {
        public static void Write(this RoomInfo room, NetDataWriter writer)
        {
            writer.Put(room.GetType().FullName);
            writer.Put(room.serialize().ToJson());
        }

        public static NetDataWriter Write(this RoomInfo room, PacketType type)
        {
            var writer = new NetDataWriter();
            writer.Put((int)type);
            room.Write(writer);
            return writer;
        }

        public static NetDataWriter Write(this RoomInfo room, PacketType type, uint requestID)
        {
            var writer = new NetDataWriter();
            writer.Put((int)type);
            writer.Put(requestID);
            room.Write(writer);
            return writer;
        }

        public static bool Read(this RoomInfo room, NetDataReader reader, IPEndPoint remoteEndPoint)
        {
            var type = reader.GetString();
            var json = reader.GetString();
            if (type != typeof(RoomInfo).FullName)
                return false;

            room = BsonSerializer.Deserialize<RoomInfo>(json)?.deserialize();

            room.ip = remoteEndPoint.Address.ToString();
            room.port = remoteEndPoint.Port;

            return true;
        }

        public static RoomInfo Parse(NetDataReader reader, IPEndPoint remoteEndPoint)
        {
            var type = reader.GetString();
            var json = reader.GetString();
            if (type != typeof(RoomInfo).FullName)
                return null;

            var room = BsonSerializer.Deserialize<RoomInfo>(json)?.deserialize();
            if (room != null)
            {
                room.ip = remoteEndPoint.Address.ToString();
                room.port = remoteEndPoint.Port;
            }

            return room;
        }
    }
}
