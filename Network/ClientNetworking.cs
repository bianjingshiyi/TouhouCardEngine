using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NitoriNetwork.Common
{
    public class ClientNetworking : INetEventListener
    {
        /// <summary>
        /// 超时时间，以毫秒计
        /// </summary>
        public virtual float timeout { get; set; } = 3;

        /// <summary>
        /// 网络管理引用
        /// </summary>
        public virtual NetManager net { get; set; } = null;

        /// <summary>
        /// host peer 
        /// </summary>
        public virtual NetPeer host { get; set; } = null;

        public INetworkingLogger logger { get; set; } = null;

        /// <summary>
        /// 客户端ID
        /// </summary>
        public int id { get; set; } = -1;

        OperationList opList = new OperationList();

        public Task<int> join(string ip, int port)
        {
            if (opList.Any(o => o is JoinOperation))
                throw new InvalidOperationException("客户端已经在执行加入操作");
            NetDataWriter writer = new NetDataWriter();
            if (IPAddress.TryParse(ip, out var address))
            {
                host = net.Connect(new IPEndPoint(address, port), writer);
                logger?.Log("客户端正在连接主机" + ip + ":" + port);
                JoinOperation operation = new JoinOperation();
                opList.Add(operation);
                _ = operationTimeout(operation, net.DisconnectTimeout / 1000, "客户端连接主机" + ip + ":" + port + "超时");
                return operation.task;
            }
            else
                throw new FormatException(ip + "不是有效的ip地址格式");
        }

        public void OnPeerConnected(NetPeer peer)
        {
            if (peer == host)
                logger?.Log("客户端连接到主机" + peer.EndPoint);
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
            logger?.Log("客户端" + id + "向主机" + host.EndPoint + "发送数据：" + obj);
            SendOperation<T> operation = new SendOperation<T>();
            opList.Add(operation);

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)packetType);
            writer.Put(operation.id);
            writer.Put(id);
            writer.Put(obj.GetType().FullName);
            writer.Put(obj.ToJson());
            host.Send(writer, DeliveryMethod.ReliableOrdered);

            _ = operationTimeout(operation, net.DisconnectTimeout / 1000, "客户端" + id + "向主机" + host.EndPoint + "发送数据响应超时：" + obj);
            return operation.task;
        }

        public Task<T> invokeHost<T>(RPCRequest request)
        {
            InvokeOperation<T> invoke = new InvokeOperation<T>(nameof(invokeHost), -1);
            opList.Add(invoke);

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.invokeRequest);
            writer.Put(invoke.id);
            request.Write(writer);

            host.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.Log("主机远程调用客户端" + id + "的" + request.MethodName + "，参数：" + string.Join("，", request.Arguments));
            _ = operationTimeout(invoke, timeout, "主机请求客户端" + invoke.pid + "远程调用" + invoke.id + "超时");
            return invoke.task;
        }

        async Task operationTimeout(Operation operation, float timeout, string msg)
        {
            await Task.Delay((int)(timeout * 1000));
            if (opList.Remove(operation.id))
            {
                logger?.Log(msg);
                operation.setCancel();
            }
        }

        public async void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.connectResponse:
                    this.id = reader.GetInt();
                    logger?.Log("客户端连接主机成功，获得ID：" + this.id);
                    if (onConnected != null)
                        await onConnected.Invoke();
                    JoinOperation joinOperation = opList.OfType<JoinOperation>().First();
                    joinOperation.setResult(this.id);
                    opList.Remove(joinOperation);
                    break;
                case PacketType.sendResponse:
                    try
                    {
                        var result = OperationResultExt.ParseRequest(reader);
                        logger?.Log("客户端" + id + "收到主机转发的来自客户端" + result.clientID + "的数据：（" + result.obj + "）");

                        if (onReceive != null)
                            await onReceive.Invoke(result.clientID, result.obj);

                        if (result.clientID == id)
                        {
                            if (opList.SetResult(result))
                            {
                                logger?.Log("客户端" + id + "收到客户端" + peer.Id + "的消息反馈" + result.requestID + "为" + result.obj.ToJson());
                            }
                            else
                            {
                                logger?.Log("客户端" + id + "收到客户端" + peer.Id + "未发送或超时的消息反馈" + result.requestID);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.Error("接收消息回应发生异常：" + e);
                    }
                    break;
                case PacketType.joinResponse:
                    var info = RoomInfoHelper.Parse(reader, peer.EndPoint);
                    if (info != null)
                    {
                        logger?.Log($"客户端 {id} 收到了主机的加入响应：" + info.ToJson());
                        roomInfo = info;
                        onJoinRoom?.Invoke(roomInfo);
                    }
                    else
                    {
                        logger?.Log($"主机加入响应解析错误");
                    }
                    break;
                case PacketType.roomInfoUpdate:
                    info = RoomInfoHelper.Parse(reader, peer.EndPoint);
                    if (info != null)
                    {
                        logger?.Log($"客户端 {id} 收到了主机的房间更新信息：" + info.ToJson());
                        onRoomInfoUpdate?.Invoke(roomInfo, info);
                        roomInfo = info;
                    }
                    else
                    {
                        logger?.Log($"房间更新信息解析错误");
                    }
                    break;
                case PacketType.invokeRequest:
                    invokeRequestHandler(peer, reader);
                    break;
                case PacketType.invokeResponse:
                    try
                    {
                        var result = OperationResultExt.ParseInvoke(reader);
                        if (!opList.SetResult(result))
                        {
                            logger?.Log("客户端接收到主机" + peer.Id + "未被请求或超时的远程调用" + result.requestID);
                        }
                        else
                        {
                            logger?.Log("客户端接收到主机" + peer.Id + "的调用结果");
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    break;
                default:
                    logger?.Warning("客户端未处理的数据包类型：" + type);
                    break;
            }
        }

        private void invokeRequestHandler(NetPeer peer, NetPacketReader reader)
        {
            try
            {
                OperationResult result = new OperationResult();
                result.requestID = reader.GetInt();

                try
                {
                    var request = RPCRequest.Parse(reader);

                    logger?.Log("客户端" + id + "执行来自主机的远程调用" + result.requestID + "，" + request);
                    try
                    {
                        if (!rpcExecutor.TryInvoke(request, out result.obj))
                        {
                            throw new MissingMethodException("无法找到方法：" + request);
                        }
                    }
                    catch (Exception invokeException)
                    {
                        result.obj = invokeException;
                        logger?.Log("客户端" + id + "执行来自主机的远程调用" + result.requestID + "{" + request + ")}发生异常：" + invokeException);
                    }
                }
                catch (Exception e)
                {
                    result.obj = e;
                    logger?.Log("客户端" + id + "执行来自主机的远程调用" + result.requestID + "发生异常：" + e);
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

        private static void objectWriter(NetDataWriter writer, object result)
        {
            if (result == null)
            {
                writer.Put(string.Empty);
            }
            else
            {
                writer.Put(result.GetType().FullName);
                writer.Put(result.ToJson());
            }
        }

        RPCExecutor rpcExecutor = new RPCExecutor();

        public void addInvokeTarget(object obj)
        {
            rpcExecutor.AddTargetObject(obj);
        }

        public event Func<int, object, Task> onReceive;
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void disconnect()
        {
            opList.CancleAll();
            if (host != null)
            {
                host.Disconnect();
                host = null;
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.Log("客户端" + id + "与主机断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
            opList.CancleAll();
            host = null;
            onDisconnect?.Invoke();
            onQuitRoom?.Invoke();
        }
        public event Action onDisconnect;
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            logger?.Error("客户端" + id + "与" + endPoint + "发生网络异常：" + socketError);
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
                        var roomInfo = RoomInfoHelper.Parse(reader, remoteEndPoint);
                        if (reqID == 0)
                        {
                            logger?.Log($"客户端找到主机，{remoteEndPoint.Address}:{remoteEndPoint.Port}");
                            if (roomInfo != null) onRoomFound?.Invoke(roomInfo);
                        }
                        else
                        {
                            logger?.Log($"获取到主机 {remoteEndPoint.Address}:{remoteEndPoint.Port} 更新的房间信息。");
                            if (roomCheckTasks.ContainsKey(reqID))
                            {
                                roomCheckTasks[reqID].SetResult(roomInfo);
                                roomCheckTasks.Remove(reqID);
                            }
                            else
                            {
                                logger?.Log($"RequestID {reqID} 不存在。");
                            }
                        }
                    }
                    else
                    {
                        logger?.Log("消息类型不匹配");
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
        public void findRoom(int port)
        {
            var writer = roomDiscoveryRequestWriter(0);
            net.SendBroadcast(writer, port);
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
            logger?.Log("操作超时");
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
            playerInfo.RoomID = id;
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
}
