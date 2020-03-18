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
        NetManager net { get; set; } = null;
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
                DisconnectTimeout = (int)(timeout * 1000)
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
            net.Start();
            _port = net.LocalPort;
            logger?.log("客户端初始化，本地端口：" + net.LocalPort);
        }
        public void start(int port)
        {
            net.Start(port);
            _port = net.LocalPort;
            logger?.log("客户端初始化，本地端口：" + net.LocalPort);
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
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (tcs != null)
                throw new InvalidOperationException("客户端正在执行另一项操作");
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.sendRequest);
            writer.Put(id);
            writer.Put(obj.GetType().FullName);
            writer.Put(obj.ToJson());
            host.Send(writer, DeliveryMethod.ReliableOrdered);
        }
        public async Task<T> send<T>(T obj)
        {
            send(obj as object);
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
            host.Disconnect();
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.log("客户端" + id + "与主机断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
            onDisconnect?.Invoke();
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
            throw new NotImplementedException();
        }
    }
}
