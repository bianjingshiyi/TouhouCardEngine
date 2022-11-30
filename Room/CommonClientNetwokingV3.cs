using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using LiteNetLib.Utils;

namespace TouhouCardEngine
{
    public abstract class CommonClientNetwokingV3 : Networking, INetworkingV3Client, IRoomRPCMethodClient
    {
        public CommonClientNetwokingV3(string name, Shared.ILogger logger) : base(name, logger)
        {
            addRPCMethod(this, typeof(IRoomRPCMethodClient));
        }

        /// <summary>
        /// 当前网络初始化后的端口号
        /// </summary>
        public int Port => net.LocalPort;

        public event ResponseHandler onReceive;

        #region 已经实现的事件
        public event Action<RoomPlayerData[]> OnRoomPlayerDataChanged;

        /// <summary>
        /// 触发 OnRoomPlayerDataChanged 事件
        /// </summary>
        /// <param name="data"></param>
        protected void invokeOnRoomPlayerDataChanged(RoomPlayerData[] data)
        {
            OnRoomPlayerDataChanged?.Invoke(data);
        }

        public event Action<RoomData> OnRoomDataChange;

        public event Action OnGameStart;

        /// <summary>
        /// 当玩家确认加入房间的时候，收到房间状况的回应。
        /// </summary>
        public event Action<RoomData> onConfirmJoinAck;

        public event Action<ChatMsg> OnRecvChat;
        public event Action<int, CardPoolSuggestion> OnSuggestCardPools;
        public event Action<int> OnCardPoolsSuggestionCanceled;
        public event Action<CardPoolSuggestion, bool> OnCardPoolsSuggestionAnwsered;

        /// <summary>
        /// 触发onGameStart事件
        /// </summary>
        protected void invokeOnGameStart()
        {
            OnGameStart?.Invoke();
        }
        protected async Task invokeOnReceive(int clientID, object data)
        {
            log?.logTrace($"接收到来自{clientID}的数据{data}");
            if (onReceive != null)
                await onReceive.Invoke(clientID, data);
        }
        /// <summary>
        /// 触发收到聊天消息事件
        /// </summary>
        /// <param name="msg"></param>
        protected void invokeOnRecvChat(ChatMsg msg)
        {
            OnRecvChat?.Invoke(msg);
        }
        /// <summary>
        /// 触发收到卡池建议事件
        /// </summary>
        /// <param name="playerId">玩家ID。</param>
        /// <param name="suggestion">建议。</param>
        protected void invokeOnCardPoolSuggested(int playerId, CardPoolSuggestion suggestion)
        {
            OnSuggestCardPools?.Invoke(playerId, suggestion);
        }
        /// <summary>
        /// 触发取消卡池建议事件
        /// </summary>
        /// <param name="playerId">玩家ID。</param>
        protected void invokeOnCardPoolSuggestionCanceled(int playerId)
        {
            OnCardPoolsSuggestionCanceled?.Invoke(playerId);
        }
        /// <summary>
        /// 触发收到卡池建议回应事件
        /// </summary>
        /// <param name="suggestion">建议。</param>
        /// <param name="agree">是否同意。</param>
        protected void invokeOnCardPoolSuggestionAnwsered(CardPoolSuggestion suggestion, bool agree)
        {
            OnCardPoolsSuggestionAnwsered?.Invoke(suggestion, agree);
        }
        /// <summary>
        /// 触发 onConfirmJoinAck 事件
        /// </summary>
        /// <param name="room"></param>
        protected void invokeOnJoinRoom(RoomData room)
        {
            onConfirmJoinAck?.Invoke(room);
        }
        #endregion

        #region 待实现的接口
        public abstract event Action<LobbyRoomDataList> OnRoomListUpdate;

        public abstract Task<RoomData> CreateRoom();
        public abstract Task DestroyRoom();
        public abstract Task GameStart();
        public abstract T GetRoomProp<T>(string name);
        public abstract RoomPlayerData GetSelfPlayerData();
        public abstract Task<RoomData> JoinRoom(string roomID);
        public abstract void QuitRoom();
        public abstract Task SetPlayerProp(string name, object val);
        public abstract Task SetRoomProp(string name, object val);
        public abstract int GetLatency();
        public abstract Task RefreshRoomList();
        public abstract Task AlterRoomInfo(LobbyRoomData newInfo);
        public abstract Task SendChat(int channel, string message);
        public abstract Task SuggestCardPools(CardPoolSuggestion suggestion);
        public abstract Task CancelCardPoolsSuggestion();
        public abstract Task AnwserCardPoolsSuggestion(int playerId, CardPoolSuggestion suggestion, bool agree); 
        #endregion

        #region RPC接口
        /// <summary>
        /// 客户端缓存的房间数据
        /// </summary>
        protected RoomData cachedRoomData;

        void IRoomRPCMethodClient.updateRoomData(RoomData data)
        {
            log?.logTrace($"{name} 收到房间数据改变事件。房间数据：{data}");

            cachedRoomData = data;
            OnRoomDataChange?.Invoke(cachedRoomData);
        }

        void IRoomRPCMethodClient.onRoomPropChange(string name, object val)
        {
            log?.logTrace($"{this.name} 收到房间属性改变事件。Key: {name}, Value: {val}");
            cachedRoomData.setProp(name, val);
            OnRoomDataChange?.Invoke(cachedRoomData);
        }

        void IRoomRPCMethodClient.updatePlayerData(RoomPlayerData data)
        {
            log?.logTrace($"{name} 收到玩家信息改变事件。玩家信息: {data}");

            for (int i = 0; i < cachedRoomData.playerDataList.Count; i++)
            {
                if (cachedRoomData.playerDataList[i].id == data.id)
                {
                    cachedRoomData.playerDataList[i] = data;
                }
            }
            OnRoomPlayerDataChanged?.Invoke(cachedRoomData.playerDataList.ToArray());
        }

        void IRoomRPCMethodClient.onPlayerAdd(RoomPlayerData data)
        {
            log?.logTrace($"{name} 收到新增玩家事件。玩家信息：{data}");
            if (cachedRoomData.containsPlayer(data.id))
            {
                log?.logWarn($"{name} 的房间信息中已经存在ID为{data.id}的玩家，但却收到了 onPlayerAdd 事件，这是有问题的。");
                for (int i = 0; i < cachedRoomData.playerDataList.Count; i++)
                {
                    if (cachedRoomData.playerDataList[i].id == data.id)
                    {
                        cachedRoomData.playerDataList[i] = data;
                    }
                }
            }
            else
            {
                cachedRoomData.playerDataList.Add(data);
            }
            OnRoomPlayerDataChanged?.Invoke(cachedRoomData.playerDataList.ToArray());
        }

        void IRoomRPCMethodClient.onPlayerRemove(int playerID)
        {
            log?.logTrace($"{name} 收到移除玩家事件。玩家ID：{playerID}");

            if (!cachedRoomData.containsPlayer(playerID))
            {
                log?.logWarn($"{name} 的房间信息中不存在存在ID为{playerID}的玩家，但却收到了 onPlayerRemove 事件，这是有问题的。");
                return;
            }
            cachedRoomData.playerDataList.RemoveAll(p => p.id == playerID);
            OnRoomPlayerDataChanged?.Invoke(cachedRoomData.playerDataList.ToArray());
        }

        void IRoomRPCMethodClient.onPlayerPropChange(int playerID, string name, object val)
        {
            log?.logTrace($"{this.name} 收到玩家属性改变事件。玩家ID：{playerID}, Key: {name}, Value: {val}");

            cachedRoomData.setPlayerProp(playerID, name, val);
            OnRoomPlayerDataChanged?.Invoke(cachedRoomData.playerDataList.ToArray());
        }

        void IRoomRPCMethodClient.onGameStart()
        {
            log?.logTrace("收到了游戏开始事件");
            invokeOnGameStart();
        }

        void IRoomRPCMethodClient.onRecvChat(int channel, int playerID, string text)
        {
            log?.logTrace($"收到了聊天消息。[{channel}] {playerID}: {text}");
            invokeOnRecvChat(new ChatMsg(channel, playerID, text));
        }
        void IRoomRPCMethodClient.onCardPoolsSuggested(int playerId, CardPoolSuggestion suggestion)
        {
            log?.logTrace($"收到了来自玩家{playerId}的加入卡池的建议：{suggestion}。");
            invokeOnCardPoolSuggested(playerId, suggestion);
        }
        void IRoomRPCMethodClient.onCardPoolsSuggestionCanceled(int playerId)
        {
            log?.logTrace($"玩家{playerId}取消了加入卡池的建议。");
            invokeOnCardPoolSuggestionCanceled(playerId);
        }
        void IRoomRPCMethodClient.onCardPoolSuggestionAnwsered(CardPoolSuggestion suggestion, bool agree)
        {
            log?.logTrace($"收到了加入卡池建议的回应：{suggestion}，回应是{agree}。");
            invokeOnCardPoolSuggestionAnwsered(suggestion, agree);
        }

        #endregion

        #region 网络底层处理
        protected class JoinRoomOperation : Operation<RoomData>
        {
            public JoinRoomOperation() : base(nameof(INetworkingV3Client.JoinRoom))
            {
            }
        }

        protected class SendOperation : Operation<object>
        {
            public SendOperation(): base(nameof(INetworkingV3Client.Send))
            {
            }
        }
        protected class SendOperation<T> : Operation<T>
        {
            public SendOperation() : base(nameof(INetworkingV3Client.Send))
            {
            }
        }

        /// <summary>
        /// 客户端ID
        /// </summary>
        /// <remarks>
        /// 这个ID等同于玩家ID。
        /// 玩家ID是不会重复的，所以可以这么干。
        /// </remarks>
        protected int clientID => GetSelfPlayerData().id;

        protected override async Task OnNetworkReceive(NetPeer peer, NetPacketReader reader, PacketType type)
        {
            switch (type)
            {
                case PacketType.sendResponse:
                    try
                    {
                        var result = reader.ParseRequest(out int cID, out int requestID);
                        log?.logTrace($"客户端 {name} 收到主机转发的来自客户端{cID}的数据：（{result}）");

                        await invokeOnReceive(cID, result);

                        if (cID == clientID)
                        {
                            var op = getOperation(requestID);
                            if (op != null)
                            {
                                completeOperation(op, result);
                                log?.logTrace($"客户端 {name}:{clientID} 收到客户端{peer.Id}的消息反馈{requestID}为{result.ToJson()}");
                            }
                            else
                            {
                                log?.log($"客户端 {name}:{clientID} 收到客户端{peer.Id}未发送或超时的消息反馈{requestID}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        log?.logError("接收消息回应发生异常：" + e);
                    }
                    break;
                default:
                    break;
            }

            await base.OnNetworkReceive(peer, reader, type);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            log?.log($"网络({name})与端点({peer.EndPoint})断开连接，原因：{disconnectInfo.Reason}，SocketErrorCode：{disconnectInfo.SocketErrorCode}");

            switch (disconnectInfo.Reason)
            {
                case DisconnectReason.ConnectionFailed: // 连接失败
                case DisconnectReason.HostUnreachable: // 无法到达主机
                case DisconnectReason.NetworkUnreachable: // 无法到达网络
                case DisconnectReason.UnknownHost: // 未知主机
                case DisconnectReason.InvalidProtocol: // 错误协议
                case DisconnectReason.ConnectionRejected: // 拒绝连接
                    // 基本上只有正在加入的时候才会触发这些Error。将这些错误转换为Exception交给上层处理，用于显示无法加入的信息。
                    var exception = new NtrNetworkException(disconnectInfo.Reason);
                    var op = getOperation(typeof(JoinRoomOperation));
                    if (op != null)
                        completeOperation(op, exception);
                    break;
                case DisconnectReason.RemoteConnectionClose: // 远程关闭
                    break;
                case DisconnectReason.Reconnect: // 重连后当前连接关闭
                    break;
                case DisconnectReason.Timeout:
                    break;
                case DisconnectReason.DisconnectPeerCalled:
                    break;
                default:
                    break;
            }
        }

        [Obsolete]
        protected override Type getType(string typeName)
        {
            return TypeHelper.getType(typeName);
        }

        protected override void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            log?.logError($"{name} 在与 {endPoint} 通信时发生异常，{socketError}");
        }

        /// <summary>
        /// 向指定Peer发送Object
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected Task<T> sendTo<T>(NetPeer peer, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));

            SendOperation<T> op = new SendOperation<T>();
            startOperation(op, () => {
                log?.logWarn($"客户端{name}发送请求超时。");
            });

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.sendRequest);
            writer.Put(op.id);
            writer.Put(clientID);
            writer.Put(obj.GetType().FullName);
            writer.Put(obj.ToJson());
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            return op.task;
        }

        public abstract Task<T> Send<T>(object obj);
        #endregion
    }

    public interface INetworkingV3Client
    {
        #region Player
        /// <summary>
        /// 获取当前玩家（自己）的玩家信息
        /// </summary>
        /// <returns></returns>
        RoomPlayerData GetSelfPlayerData();
        #endregion

        #region Lobby
        /// <summary>
        /// 以当前玩家为房主创建一个房间
        /// </summary>
        /// <returns></returns>
        Task<RoomData> CreateRoom();

        /// <summary>
        /// 关闭当前已经创建的房间（部分情况下用不上）
        /// </summary>
        /// <returns></returns>
        Task DestroyRoom();

        /// <summary>
        /// 获取当前可加入的房间信息。
        /// 调用此方法后会立即返回，在获取到房间信息后多次触发OnRoomListUpdate事件
        /// </summary>
        /// <returns></returns>
        Task RefreshRoomList();

        /// <summary>
        /// 可加入的房间列表更新事件
        /// </summary>
        event Action<LobbyRoomDataList> OnRoomListUpdate;

        /// <summary>
        /// 修改当前房间的信息
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        Task AlterRoomInfo(LobbyRoomData newInfo);
        #endregion

        #region Room
        /// <summary>
        /// 使用当前用户加入一个房间
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        Task<RoomData> JoinRoom(string roomID);

        /// <summary>
        /// 退出当前加入的房间
        /// </summary>
        /// <returns></returns>
        void QuitRoom();

        /// <summary>
        /// 房间玩家信息改变事件
        /// </summary>
        /// <returns></returns>
        event Action<RoomPlayerData[]> OnRoomPlayerDataChanged;

        /// <summary>
        /// 修改玩家的属性
        /// </summary>
        /// <returns></returns>
        Task SetPlayerProp(string name, object val);

        /// <summary>
        /// 获取房间属性
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetRoomProp<T>(string name);

        /// <summary>
        /// 修改房间的属性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        Task SetRoomProp(string name, object val);

        /// <summary>
        /// 房间数据被修改后调用此方法
        /// 注意用户数据并不算房间数据，所以有用户加入实际上不会触发这个方法
        /// </summary>
        event Action<RoomData> OnRoomDataChange;

        /// <summary>
        /// 请求开始游戏！
        /// 注意只有房主能调用。
        /// </summary>
        /// <returns></returns>
        Task GameStart();

        /// <summary>
        /// 获取当前网络的延迟
        /// </summary>
        /// <returns></returns>
        int GetLatency();

        /// <summary>
        /// 收到聊天消息
        /// </summary>
        event Action<ChatMsg> OnRecvChat;

        /// <summary>
        /// 发送聊天消息
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendChat(int channel, string message);
        #endregion

        #region Game
        /// <summary>
        /// 发送GameRequest
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task<T> Send<T>(object obj);

        /// <summary>
        /// 收到GameResponse
        /// </summary>
        event ResponseHandler onReceive;

        /// <summary>
        /// 游戏开始事件
        /// </summary>
        event Action OnGameStart;
        #endregion
    }
    
    /// <summary>
    /// 本地Host扩展的一些方法
    /// </summary>
    public interface INetworkingV3LANHost
    {
        Task AddPlayer(RoomPlayerData player);

        /// <summary>
        /// 当玩家请求加入房间的时候，是否回应？
        /// 检查玩家信息和房间信息，判断是否可以加入
        /// </summary>
        event Func<RoomPlayerData, RoomData> onJoinRoomReq;
    }

    /// <summary>
    /// GameResponse处理器
    /// </summary>
    /// <param name="clientID">发送者ID</param>
    /// <param name="obj">发送的数据</param>
    /// <returns></returns>
    public delegate Task ResponseHandler(int clientID, object obj);

    /// <summary>
    /// 聊天消息
    /// </summary>
    public class ChatMsg
    {
        public int Channel { get; set; }
        public int Sender { get; set; }
        public string Message { get; set; }

        public ChatMsg(int channel, int playerID, string text)
        {
            Channel = channel;
            Sender = playerID;
            Message = text;
        }
    }

    /// <summary>
    /// 滑动窗口平均
    /// </summary>
    class SlidingAverage
    {
        public int Size { get; }
        int[] buffer;

        int index = 0, count = 0;
        long sum = 0;

        /// <summary>
        /// 窗口大小
        /// </summary>
        /// <param name="size"></param>
        public SlidingAverage(int size)
        {
            Size = size;
            buffer = new int[Size];
        }

        /// <summary>
        /// 插入一个值
        /// </summary>
        /// <param name="num"></param>
        public void Push(int num)
        {
            if (count == Size)
            {
                sum -= buffer[index];
                sum += num;
            }

            buffer[index++] = num;
            index = index % Size;

            if (count < Size) count++;
        }

        /// <summary>
        /// 获取平均值
        /// </summary>
        /// <returns></returns>
        public float GetAvg()
        {
            return (float)sum / count;
        }
    }

    [Serializable]
    public class NtrNetworkException : Exception
    {
        public NtrNetworkException() { }
        public NtrNetworkException(DisconnectReason reason) : base(reason.ToString()) { }
        protected NtrNetworkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}