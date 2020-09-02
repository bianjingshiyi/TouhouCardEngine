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
        [Obsolete("Use InvokeALl(rpcRequest) instead")]
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

        #endregion
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.invokeResponse:
                    try
                    {
                        int rid = reader.GetInt();
                        string typeName = reader.GetString();
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            if (TypeHelper.tryGetType(typeName, out Type objType))
                            {
                                string json = reader.GetString();
                                object obj = BsonSerializer.Deserialize(json, objType);
                                var op = opList[rid];
                                IInvokeOperation invoke = op as IInvokeOperation;
                                if (invoke == null)
                                {
                                    logger?.log("主机接收到客户端" + peer.Id + "未被请求或超时的远程调用" + rid);
                                    break;
                                }
                                opList.Remove(op.id);
                                if (obj is Exception e)
                                {
                                    logger?.log("主机收到客户端" + peer.Id + "的远程调用回应" + rid + "{" + invoke.request + "}在客户端发生异常：" + e);
                                    invoke.setException(e);
                                }
                                else
                                {
                                    logger?.log("主机接收客户端" + peer.Id + "的远程调用" + rid + "{" + invoke.request + ")" + "}返回为" + obj);
                                    invoke.setResult(obj);
                                }
                            }
                            else
                                throw new TypeLoadException("无法识别的类型" + typeName);
                        }
                        else
                        {
                            Operation op = opList[rid];
                            IInvokeOperation invoke = op as IInvokeOperation;
                            if (invoke == null)
                            {
                                logger?.log("主机接收到客户端" + peer.Id + "未被请求或超时的远程调用" + rid);
                                break;
                            }
                            opList.Remove(rid);
                            logger?.log("主机接收客户端" + peer.Id + "的远程调用" + rid + "{" + invoke.request + "}返回为null");
                            invoke.setResult(null);
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    break;
                case PacketType.sendRequest:
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
                    break;
                case PacketType.joinRequest:
                    try
                    {
                        if (!RoomIsValid)
                            break;
                        int rid = reader.GetInt();
                        int id = reader.GetInt();
                        string typeName = reader.GetString();
                        string json = reader.GetString();
                        if (TypeHelper.tryGetType(typeName, out Type objType))
                        {
                            object obj = BsonSerializer.Deserialize(json, objType);
                            if (obj is RoomPlayerInfo info)
                            {
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
                    break;
                case PacketType.playerInfoUpdateRequest:
                    playerInfoUpdateRequestHandler(peer, reader);
                    break;
                default:
                    logger?.log("Warning", "服务端未处理的数据包类型：" + type);
                    break;
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
        /// <param name="playerID"></param>
        public void removePlayer(int playerID)
        {
            foreach (var player in room.playerList)
            {
                if (player.RoomID == playerID)
                {
                    // WARNING: 这里的PlayerInfo的ID是直接用的peerID；如果之后改了这种设定，就要改掉这里
                    net.DisconnectPeer(clientDic[playerID]);
                    // 从网络上断开后，上面的OnDisconnect事件会在房间中删除这个玩家
                }
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
    }
}
