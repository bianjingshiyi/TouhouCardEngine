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
using Newtonsoft.Json;
using System.Text.RegularExpressions;
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
                IPv6Enabled = false
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
        public Task<T> invoke<T>(int id, string method, params object[] args)
        {
            NetPeer peer = clientDic[id];
            InvokeOperation<T> invoke = new InvokeOperation<T>()
            {
                pid = peer.Id,
                rid = ++_lastInvokeId,
                method = method,
                args = args,
                tcs = new TaskCompletionSource<T>()
            };
            _invokeList.Add(invoke);

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.invokeRequest);
            writer.Put(invoke.rid);
            writer.Put(typeof(T).FullName);
            writer.Put(method);
            writer.Put(args.Length);
            foreach (object arg in args)
            {
                if (arg != null)
                {
                    writer.Put(arg.GetType().FullName);
                    writer.Put(arg.ToJson());
                }
                else
                    writer.Put(string.Empty);
            }
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.log("主机远程调用客户端" + id + "的" + method + "，参数：" + string.Join("，", args));
            _ = invokeTimeout(invoke);
            return invoke.tcs.Task;
        }
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
        async Task invokeTimeout(InvokeOperation invoke)
        {
            await Task.Delay((int)(timeout * 1000));
            if (_invokeList.Remove(invoke))
            {
                logger?.log("主机请求客户端" + invoke.pid + "远程调用" + invoke.rid + "超时");
                invoke.setCancel();
            }
        }
        abstract class InvokeOperation
        {
            public int rid;
            public int pid;
            public string method;
            public object[] args;
            public abstract void setResult(object obj);
            public abstract void setException(Exception e);
            public abstract void setCancel();
        }
        class InvokeOperation<T> : InvokeOperation
        {
            public TaskCompletionSource<T> tcs;
            public override void setResult(object obj)
            {
                if (obj == null)
                    tcs.SetResult(default);
                else if (obj is T t)
                    tcs.SetResult(t);
                else
                    throw new InvalidCastException();
            }
            public override void setException(Exception e)
            {
                tcs.SetException(e);
            }
            public override void setCancel()
            {
                tcs.SetCanceled();
            }
        }
        int _lastInvokeId = 0;
        List<InvokeOperation> _invokeList = new List<InvokeOperation>();
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
                                InvokeOperation invoke = _invokeList.Find(i => i.rid == rid);
                                if (invoke == null)
                                {
                                    logger?.log("主机接收到客户端" + peer.Id + "未被请求或超时的远程调用" + rid);
                                    break;
                                }
                                _invokeList.Remove(invoke);
                                if (obj is Exception e)
                                {
                                    logger?.log("主机收到客户端" + peer.Id + "的远程调用回应" + rid + "{" + invoke.method + "(" + string.Join(",", invoke.args) + ")" + "}在客户端发生异常：" + e);
                                    invoke.setException(e);
                                }
                                else
                                {
                                    logger?.log("主机接收客户端" + peer.Id + "的远程调用" + rid + "{" + invoke.method + "(" + string.Join(",", invoke.args) + ")" + "}返回为" + obj);
                                    invoke.setResult(obj);
                                }
                            }
                            else
                                throw new TypeLoadException("无法识别的类型" + typeName);
                        }
                        else
                        {
                            InvokeOperation invoke = _invokeList.Find(i => i.rid == rid);
                            if (invoke == null)
                            {
                                logger?.log("主机接收到客户端" + peer.Id + "未被请求或超时的远程调用" + rid);
                                break;
                            }
                            _invokeList.Remove(invoke);
                            logger?.log("主机接收客户端" + peer.Id + "的远程调用" + rid + "{" + invoke.method + "(" + string.Join(",", invoke.args) + ")" + "}返回为null");
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
                        if (room == null)
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

                                NetDataWriter writer = RoomInfoUpdateWriter();

                                foreach (var client in clientDic.Values)
                                {
                                    if (client.Id == peer.Id)
                                    {
                                        // 接受加入，返回房间信息
                                        NetDataWriter writer2 = RoomInfoResponseWriter();
                                        client.Send(writer2, DeliveryMethod.ReliableOrdered);
                                    }
                                    else
                                    {
                                        // 其他的更新房间信息
                                        client.Send(writer, DeliveryMethod.ReliableOrdered);
                                    }
                                }
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
            var infos = room?.playerList.Where(c => c.id == peer.Id);
            if (infos != null && infos.Count() > 0)
            {
                var info = infos.First();
                room.playerList.Remove(info);
                onPlayerQuit?.Invoke(info);

                var writer = RoomInfoUpdateWriter();
                foreach (var client in clientDic.Values)
                {
                    if (client.Id != peer.Id)
                    {
                        // 其他的更新房间信息
                        client.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
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
                case UnconnectedMessageType.Broadcast:
                case UnconnectedMessageType.BasicMessage:
                    if (room != null && reader.GetInt() == (int)PacketType.discoveryRequest)
                    {
                        logger?.log($"主机房间收到了局域网发现请求或主机信息更新请求");
                        NetDataWriter writer = RoomInfoDiscoveryWriter(reader.GetUInt());
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
        RoomInfo _room = null;
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
        private NetDataWriter RoomInfoUpdateWriter()
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.roomInfoUpdate);
            RoomInfoWriter(writer);
            return writer;
        }

        private NetDataWriter RoomInfoDiscoveryWriter(uint requestID)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.discoveryResponse);
            writer.Put(requestID);
            RoomInfoWriter(writer);
            return writer;
        }

        private void RoomInfoWriter(NetDataWriter writer)
        {
            writer.Put(room.GetType().FullName);
            writer.Put(room.serialize().ToJson());
        }
        private NetDataWriter RoomInfoResponseWriter()
        {
            var writer = new NetDataWriter();
            writer.Put((int)PacketType.joinResponse);
            RoomInfoWriter(writer);
            return writer;
        }


        public event Action<RoomPlayerInfo> onPlayerJoin;
        /// <summary>
        /// 当前房间信息，在没有打开房间的情况下为空。
        /// </summary>
        public RoomInfo roomInfo => room;

        /// <summary>
        /// 更新房间信息，会在Host保存最新的房间信息和将更新的房间信息发送给所有的Client
        /// </summary>
        /// <param name="roomInfo"></param>
        public void updateRoomInfo(RoomInfo roomInfo)
        {
            room = roomInfo;
            var writer = RoomInfoUpdateWriter();

            foreach (var client in clientDic.Values)
            {
                client.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }
        public event Action<RoomPlayerInfo> onPlayerQuit;
        public void closeRoom()
        {
            net.DisconnectAll();
            room = null;
        }
        #endregion
    }
    [Serializable]
    public class RoomInfo
    {
        public string ip;
        public int port;
        public List<RoomPlayerInfo> playerList = new List<RoomPlayerInfo>();
        [SerializeField]
        List<string> _persistDataList = new List<string>();
        public bool isOne(RoomInfo other)
        {
            if (other == null)
                return false;
            return ip == other.ip && port == other.port;
        }
        public void setProp(string name, object value)
        {
            runtimeDic[name] = value;
        }
        [NonSerialized]
        public Dictionary<string, object> runtimeDic = new Dictionary<string, object>();
        public T getProp<T>(string name)
        {
            return (T)runtimeDic[name];
        }
        public object getProp(string name)
        {
            return runtimeDic[name];
        }
        public bool tryGetProp<T>(string name, out T value)
        {
            if (runtimeDic.ContainsKey(name) && runtimeDic[name] is T t1)
            {
                value = t1;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        public static Type getType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
                return type;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            return type;
        }
        public RoomInfo serialize()
        {
            _persistDataList.Clear();
            foreach (var pair in runtimeDic)
            {
                _persistDataList.Add(pair.Key + ":(" + pair.Value.GetType().FullName + ")" + JsonConvert.SerializeObject(pair.Value));
            }
            return this;
        }
        public RoomInfo deserialize()
        {
            runtimeDic.Clear();
            foreach (string data in _persistDataList)
            {
                if (Regex.Match(data, @"(?<name>.+):\((?<type>.+)\)(?<json>.+)") is var m && m.Success)
                {
                    runtimeDic.Add(m.Groups["name"].Value, JsonConvert.DeserializeObject(m.Groups["json"].Value, getType(m.Groups["type"].Value)));
                }
                else
                    throw new FormatException("错误的序列化格式：" + data);
            }
            return this;
        }
    }
    [Serializable]
    public class RoomPlayerInfo
    {
        public int id = 0;
        public string name = null;
        public Dictionary<string, KeyValuePair<string, string>> propJsonDic = new Dictionary<string, KeyValuePair<string, string>>();
        public void setProp(string name, object value)
        {
            propJsonDic.Add(name, new KeyValuePair<string, string>(value.GetType().FullName, value.ToJson()));
        }
        [NonSerialized]
        Dictionary<string, object> cacheDic = new Dictionary<string, object>();
        public T getProp<T>(string name)
        {
            if (cacheDic.ContainsKey(propJsonDic[name].Value) && cacheDic[propJsonDic[name].Value] is T t1)
                return t1;
            if (BsonSerializer.Deserialize(propJsonDic[name].Value, RoomInfo.getType(propJsonDic[name].Key)) is T t2)
            {
                cacheDic.Add(propJsonDic[name].Value, t2);
                return t2;
            }
            else
                throw new InvalidTypeException(name + "的类型" + propJsonDic[name].Key + "与返回类型" + typeof(T).FullName + "不一致");
        }
        public bool tryGetProp<T>(string name, out T value)
        {
            if (!propJsonDic.ContainsKey(name))
            {
                value = default;
                return false;
            }
            if (cacheDic.ContainsKey(propJsonDic[name].Value) && cacheDic[propJsonDic[name].Value] is T t1)
            {
                value = t1;
                return true;
            }
            if (RoomInfo.getType(propJsonDic[name].Key) is Type type && BsonSerializer.Deserialize(propJsonDic[name].Value, type) is T t2)
            {
                cacheDic.Add(propJsonDic[name].Value, t2);
                value = t2;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
    enum PacketType
    {
        connectResponse,
        sendRequest,
        sendResponse,
        invokeRequest,
        invokeResponse,
        discoveryRequest,
        discoveryResponse,
        joinRequest,
        joinResponse,
        roomInfoUpdate,
    }
    public class TypeHelper
    {
        public static bool tryGetType(string typeName, out Type type)
        {
            type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return true;
                }
                return false;
            }
            else
                return true;
        }
    }
}
