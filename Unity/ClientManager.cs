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
using System.Threading;
using System.Linq;
using System.Reflection.Emit;
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
            if (_operationList.Any(o => o is JoinOperation))
                throw new InvalidOperationException("客户端已经在执行加入操作");
            NetDataWriter writer = new NetDataWriter();
            if (IPAddress.TryParse(ip, out var address))
            {
                host = net.Connect(new IPEndPoint(address, port), writer);
                logger?.log("客户端正在连接主机" + ip + ":" + port);
                JoinOperation operation = new JoinOperation(this);
                _operationList.Add(operation);
                _ = operationTimeout(operation, net.DisconnectTimeout / 1000, "客户端连接主机" + ip + ":" + port + "超时");
                return operation.task;
            }
            else
                throw new FormatException(ip + "不是有效的ip地址格式");
        }
        class JoinOperation : Operation<int>
        {
            public JoinOperation(ClientManager manager) : base(manager, nameof(join))
            {
            }
        }
        public void OnPeerConnected(NetPeer peer)
        {
            if (peer == host)
                logger?.log("客户端连接到主机" + peer.EndPoint);
        }
        public event Func<Task> onConnected;
        public Task send(object obj)
        {
            return send(obj, PacketType.sendRequest);
        }
        public async Task<T> send<T>(T obj)
        {
            return await send(obj, PacketType.sendRequest);
        }
        Task<T> send<T>(T obj, PacketType packetType)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (host == null)
                throw new InvalidOperationException("客户端没有与主机连接，无法发送消息");
            logger?.log("客户端" + id + "向主机" + host.EndPoint + "发送数据：" + obj);
            SendOperation<T> operation = new SendOperation<T>(this);

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)packetType);
            writer.Put(operation.id);
            writer.Put(id);
            writer.Put(obj.GetType().FullName);
            writer.Put(obj.ToJson());
            host.Send(writer, DeliveryMethod.ReliableOrdered);

            _operationList.Add(operation);
            _ = operationTimeout(operation, net.DisconnectTimeout / 1000, "客户端" + id + "向主机" + host.EndPoint + "发送数据响应超时：" + obj);
            return operation.task;
        }
        class SendOperation<T> : Operation<T>
        {
            public SendOperation(ClientManager manager) : base(manager, nameof(send))
            {
            }
        }
        public Task<T> invokeHost<T>(string method, params object[] args)
        {
            InvokeOperation<T> invoke = new InvokeOperation<T>(this, nameof(invokeHost), -1);
            _operationList.Add(invoke);

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.invokeRequest);
            writer.Put(invoke.id);
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
            host.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.log("主机远程调用客户端" + id + "的" + method + "，参数：" + string.Join("，", args));
            _ = operationTimeout(invoke, timeout, "主机请求客户端" + invoke.pid + "远程调用" + invoke.id + "超时");
            return invoke.task;
        }
        async Task operationTimeout(Operation operation, float timeout, string msg)
        {
            await Task.Delay((int)(timeout * 1000));
            if (_operationList.Remove(operation))
            {
                logger?.log(msg);
                operation.setCancel();
            }
        }
        [Serializable]
        class Operation
        {
            [SerializeField]
            int _id;
            public int id => _id;
            [SerializeField]
            string _name;
            public string name => _name;
            public Operation(ClientManager manager, string name)
            {
                _id = ++manager._lastOperationId;
                _name = name;
            }
            public virtual void setCancel()
            {
                throw new NotImplementedException();
            }
        }
        [Serializable]
        class Operation<T> : Operation, IOperation
        {
            TaskCompletionSource<T> tcs { get; } = new TaskCompletionSource<T>();
            public Task<T> task => tcs.Task;
            public Operation(ClientManager manager, string name) : base(manager, name)
            {
            }
            public virtual void setResult(object obj)
            {
                if (obj == null)
                    tcs.SetResult(default);
                else if (obj is T t)
                    tcs.SetResult(t);
                else
                    throw new InvalidCastException();
            }
            public virtual void setException(Exception e)
            {
                tcs.SetException(e);
            }
            public override void setCancel()
            {
                if (tcs.Task.IsCompleted || tcs.Task.IsFaulted || tcs.Task.IsCanceled)
                    return;
                tcs.SetCanceled();
            }
        }
        interface IOperation
        {
            int id { get; }
            void setResult(object obj);
            void setException(Exception e);
        }
        interface IInvokeOperation : IOperation
        {
            int pid { get; }
        }
        class InvokeOperation<T> : Operation<T>, IInvokeOperation
        {
            public int pid { get; }
            public InvokeOperation(ClientManager manager, string name, int pid) : base(manager, name)
            {
                this.pid = pid;
            }
        }
        int _lastOperationId = 0;
        List<Operation> _operationList = new List<Operation>();
        public async void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.connectResponse:
                    this.id = reader.GetInt();
                    logger?.log("客户端连接主机成功，获得ID：" + this.id);
                    if (onConnected != null)
                        await onConnected.Invoke();
                    JoinOperation joinOperation = _operationList.OfType<JoinOperation>().First();
                    joinOperation.setResult(this.id);
                    _operationList.Remove(joinOperation);
                    break;
                case PacketType.sendResponse:
                    try
                    {
                        int rid = reader.GetInt();
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
                        if (onReceive != null)
                            await onReceive.Invoke(id, obj);
                        if (id == this.id)
                        {
                            IOperation invoke = _operationList.OfType<IOperation>().FirstOrDefault(i => i.id == rid);
                            if (invoke == null)
                            {
                                logger?.log("客户端" + this.id + "收到客户端" + peer.Id + "未发送或超时的消息反馈" + rid);
                                break;
                            }
                            _operationList.Remove(invoke as Operation);
                            if (obj is Exception e)
                            {
                                logger?.log("客户端" + this.id + "收到客户端" + peer.Id + "在收到消息" + rid + "时发生异常：" + e);
                                invoke.setException(e);
                            }
                            else
                            {
                                logger?.log("客户端" + this.id + "收到客户端" + peer.Id + "的消息反馈" + rid + "为" + obj.ToJson());
                                invoke.setResult(obj);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.logError("Network", "接收消息回应发生异常：" + e);
                    }
                    break;
                case PacketType.joinResponse:
                    var info = parseRoomInfo(peer.EndPoint, reader);
                    if (info != null)
                    {
                        logger?.log($"客户端 {id} 收到了主机的加入响应：" + info.ToJson());
                        roomInfo = info.deserialize();
                        onJoinRoom?.Invoke(roomInfo);
                    }
                    break;
                case PacketType.roomInfoUpdate:
                    info = parseRoomInfo(peer.EndPoint, reader);
                    if (info != null)
                    {
                        logger?.log($"客户端 {id} 收到了主机的房间更新信息：" + info.ToJson());
                        var newInfo = info.deserialize();
                        onRoomInfoUpdate?.Invoke(roomInfo, newInfo);
                        roomInfo = newInfo;
                    }
                    break;
                case PacketType.invokeRequest:
                    try
                    {
                        int rid = reader.GetInt();
                        object result = null;
                        NetDataWriter writer = new NetDataWriter();
                        try
                        {
                            string returnTypeName = reader.GetString();
                            Type returnType = Type.GetType(returnTypeName);
                            if (returnType == null)
                            {
                                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                                {
                                    returnType = assembly.GetType(returnTypeName);
                                    if (returnType != null)
                                        break;
                                }
                            }
                            string methodName = reader.GetString();
                            int argLength = reader.GetInt();
                            object[] args = new object[argLength];
                            for (int i = 0; i < args.Length; i++)
                            {
                                string typeName = reader.GetString();
                                if (!string.IsNullOrEmpty(typeName))
                                {
                                    string json = reader.GetString();
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
                                    args[i] = obj;
                                }
                                else
                                    args[i] = null;
                            }
                            logger?.log("客户端" + id + "执行来自主机的远程调用" + rid + "，方法：" + methodName + "，参数：" + string.Join("，", args));
                            try
                            {
                                if (!tryInvoke(returnType, methodName, args, out result))
                                {
                                    throw new MissingMethodException("无法找到方法：" + returnTypeName + " " + methodName + "(" + string.Join(",", args.Select(a => a.GetType().Name)) + ")");
                                }
                            }
                            catch (Exception invokeException)
                            {
                                writer.Put((int)PacketType.invokeResponse);
                                writer.Put(rid);
                                writer.Put(invokeException.GetType().FullName);
                                string exceptionJson = invokeException.ToJson();
                                writer.Put(exceptionJson);
                                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                                logger?.log("客户端" + id + "执行来自主机的远程调用" + rid + "{" + methodName + "(" + string.Join(",", args) + ")}发生异常：" + invokeException);
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            writer.Put((int)PacketType.invokeResponse);
                            writer.Put(rid);
                            writer.Put(e.GetType().FullName);
                            string exceptionJson = e.ToJson();
                            writer.Put(exceptionJson);
                            peer.Send(writer, DeliveryMethod.ReliableOrdered);
                            logger?.log("客户端" + id + "执行来自主机的远程调用" + rid + "发生异常：" + e);
                            break;
                        }
                        writer.Put((int)PacketType.invokeResponse);
                        writer.Put(rid);
                        if (result == null)
                            writer.Put(string.Empty);
                        else
                        {
                            writer.Put(result.GetType().FullName);
                            writer.Put(result.ToJson());
                        }
                        peer.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    break;
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
                                IInvokeOperation invoke = _operationList.OfType<IInvokeOperation>().FirstOrDefault(i => i.id == rid);
                                if (invoke == null)
                                {
                                    logger?.log("主机接收到客户端" + peer.Id + "未被请求或超时的远程调用" + rid);
                                    break;
                                }
                                _operationList.Remove(invoke as Operation);
                                if (obj is Exception e)
                                {
                                    logger?.log("主机收到客户端" + peer.Id + "的远程调用回应" + rid + "在客户端发生异常：" + e);
                                    invoke.setException(e);
                                }
                                else
                                {
                                    logger?.log("主机接收客户端" + peer.Id + "的远程调用" + rid + "返回为" + obj);
                                    invoke.setResult(obj);
                                }
                            }
                            else
                                throw new TypeLoadException("无法识别的类型" + typeName);
                        }
                        else
                        {
                            IInvokeOperation invoke = _operationList.OfType<IInvokeOperation>().FirstOrDefault(i => i.id == rid);
                            if (invoke == null)
                            {
                                logger?.log("主机接收到客户端" + peer.Id + "未被请求或超时的远程调用" + rid);
                                break;
                            }
                            _operationList.Remove(invoke as Operation);
                            logger?.log("主机接收客户端" + peer.Id + "的远程调用" + rid + "返回为null");
                            invoke.setResult(null);
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    break;
                default:
                    logger?.log("Warning", "客户端未处理的数据包类型：" + type);
                    break;
            }
        }
        private bool tryInvoke(Type returnType, string methodName, object[] args, out object result)
        {
            foreach (var target in invokeTargetList)
            {
                foreach (var method in target.GetType().GetMethods())
                {
                    if (tryInvoke(returnType, method, methodName, target, args, out result))
                        return true;
                }
            }
            result = null;
            return false;
        }
        bool tryInvoke(Type returnType, MethodInfo method, string methodName, object obj, object[] args, out object result)
        {
            if (method.ReturnType != typeof(void) && method.ReturnType != returnType)
            {
                result = null;
                return false;
            }
            if (method.Name != methodName)
            {
                result = null;
                return false;
            }
            var @params = method.GetParameters();
            if (@params.Length != args.Length)
            {
                result = null;
                return false;
            }
            for (int i = 0; i < @params.Length; i++)
            {
                if (!@params[i].ParameterType.IsInstanceOfType(args[i]))
                {
                    result = null;
                    return false;
                }
            }
            try
            {
                result = method.Invoke(obj, args);
                return true;
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }
        List<object> invokeTargetList { get; } = new List<object>();
        public void addInvokeTarget(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (!invokeTargetList.Contains(obj))
                invokeTargetList.Add(obj);
        }
        public bool removeInvokeTarget(object obj)
        {
            return invokeTargetList.Remove(obj);
        }
        public event Func<int, object, Task> onReceive;
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void disconnect()
        {
            cancleAllOperation();
            if (host != null)
            {
                host.Disconnect();
                host = null;
            }
        }

        private void cancleAllOperation()
        {
            foreach (var operation in _operationList)
            {
                operation.setCancel();
            }
            _operationList.Clear();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.log("客户端" + id + "与主机断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
            cancleAllOperation();
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
                case UnconnectedMessageType.BasicMessage:
                    if (reader.GetInt() == (int)PacketType.discoveryResponse)
                    {
                        uint reqID = reader.GetUInt();
                        var roomInfo = parseRoomInfo(remoteEndPoint, reader).deserialize();
                        if (reqID == 0)
                        {
                            logger?.log($"客户端找到主机，{remoteEndPoint.Address}:{remoteEndPoint.Port}");
                            if (roomInfo != null) onRoomFound?.Invoke(roomInfo);
                        }
                        else
                        {
                            logger?.log($"获取到主机 {remoteEndPoint.Address}:{remoteEndPoint.Port} 更新的房间信息。");
                            if (roomCheckTasks.ContainsKey(reqID))
                            {
                                roomCheckTasks[reqID].SetResult(roomInfo);
                                roomCheckTasks.Remove(reqID);
                            }
                            else
                            {
                                logger?.log($"RequestID {reqID} 不存在。");
                            }
                        }
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
            var writer = roomDiscoveryRequestWriter(0);
            net.SendBroadcast(writer, port);
        }
        RoomInfo parseRoomInfo(IPEndPoint remoteEndPoint, NetPacketReader reader)
        {
            var type = reader.GetString();
            var json = reader.GetString();
            Type objType = Type.GetType(type);
            Debug.Log("Recv Json: " + json);
            if (objType == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    objType = assembly.GetType(type);
                    if (objType != null)
                        break;
                }
            }
            object obj = BsonSerializer.Deserialize(json, objType);
            if (obj is RoomInfo info)
            {
                info.ip = remoteEndPoint.Address.ToString();
                info.port = remoteEndPoint.Port;
                return info;
            }
            else
            {
                logger?.log($"主机房间信息类型错误，收到了 {type}");
                return null;
            }
        }
        NetDataWriter roomDiscoveryRequestWriter(uint reqID)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.discoveryRequest);
            writer.Put(reqID);
            return writer;
        }

        Dictionary<uint, TaskCompletionSource<RoomInfo>> roomCheckTasks = new Dictionary<uint, TaskCompletionSource<RoomInfo>>();

        public event Action<RoomInfo> onRoomFound;

        void taskChecker(uint id)
        {
            Thread.Sleep(5000);
            logger?.log("操作超时");
            if (roomCheckTasks.ContainsKey(id) && !roomCheckTasks[id].Task.IsCompleted)
            {
                roomCheckTasks[id].SetResult(null);
                roomCheckTasks.Remove(id);
            }
        }

        /// <summary>
        /// 向目标房间请求新的房间信息，如果目标房间已经不存在了，那么会返回空，否则返回更新的房间信息。
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        public Task<RoomInfo> checkRoomInfo(RoomInfo roomInfo)
        {
            uint reqID;
            var random = new System.Random();

            do { reqID = (uint)random.Next(); }
            while (roomCheckTasks.ContainsKey(reqID) || reqID == 0);

            NetDataWriter writer = roomDiscoveryRequestWriter(reqID);
            var result = net.SendUnconnectedMessage(writer, new IPEndPoint(IPAddress.Parse(roomInfo.ip), roomInfo.port));

            TaskCompletionSource<RoomInfo> task = new TaskCompletionSource<RoomInfo>();
            if (result)
            {
                roomCheckTasks.Add(reqID, task);
                var t = new Task(() => taskChecker(reqID));
                t.Start();
            }
            else
            {
                task.SetResult(null);
            }
            return task.Task;
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
        /// 请求更新
        /// </summary>
        /// <param name="playerInfo"></param>
        /// <returns></returns>
        public async Task updatePlayerInfo(RoomPlayerInfo playerInfo)
        {
            await send(playerInfo as object, PacketType.playerInfoUpdateRequest);
        }
         
        /// <summary>
        /// 当前所在房间信息，如果不在任何房间中则为空。
        /// </summary>
        public RoomInfo roomInfo
        {
            get; private set;
        }
        public delegate void RoomInfoUpdateDelegate(RoomInfo now, RoomInfo updated);
        public event RoomInfoUpdateDelegate onRoomInfoUpdate;
        public void quitRoom()
        {
            if (host != null)
                host.Disconnect();
        }
        #endregion
    }

    [Serializable]
    public class RPCException : Exception
    {
        public RPCException() { }
        public RPCException(string message) : base(message) { }
        public RPCException(string message, Exception inner) : base(message, inner) { }
        protected RPCException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
