using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;
using NitoriNetwork.Common;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    public abstract class CommonClientNetwokingV3 : Networking, INetworkingV3Client, IRoomRPCMethodClient
    {
        public ResourceClient ResClient { get; protected set; } = null;

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
        public event Action<string, object> PostRoomPropChange;

        public event Action OnGameStart;

        /// <summary>
        /// 当玩家确认加入房间的时候，收到房间状况的回应。
        /// </summary>
        public event Action<RoomData> onConfirmJoinAck;

        public event Action<ChatMsg> OnRecvChat;
        public event Action<int, CardPoolSuggestion> OnSuggestCardPools;
        public event Action<CardPoolSuggestion, bool> OnCardPoolsSuggestionAnwsered;

        /// <summary>
        /// 触发onGameStart事件
        /// </summary>
        protected void invokeOnGameStart()
        {
            OnGameStart?.Invoke();
        }
        protected async Task invokeOnReceive(int clientID, byte[] data)
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

        public abstract Task<RoomData> CreateRoom(string name = "", string password = "");
        public abstract Task DestroyRoom();
        public abstract Task GameStart();
        public abstract T GetRoomProp<T>(string name);
        public abstract RoomPlayerData GetSelfPlayerData();
        public abstract Task<RoomData> JoinRoom(string roomID, string password = "");
        public abstract void QuitRoom();
        public abstract Task SetPlayerProp(string name, object val);
        public abstract Task SetRoomProp(string name, object val);
        public abstract Task SetRoomPropBatch(List<KeyValuePair<string, object>> values);
        public abstract int GetLatency();
        public abstract Task RefreshRoomList();
        public abstract Task AlterRoomInfo(LobbyRoomData newInfo);
        public abstract Task SendChat(int channel, string message);
        public abstract Task SuggestCardPools(CardPoolSuggestion suggestion);
        public abstract Task AnwserCardPoolsSuggestion(int playerId, CardPoolSuggestion suggestion, bool agree);
        public abstract Task<byte[]> GetResourceAsync(ResourceType type, string id);
        public abstract Task UploadResourceAsync(ResourceType type, string id, byte[] bytes);
        public abstract Task<bool> ResourceExistsAsync(ResourceType type, string id);
        public abstract Task<bool[]> ResourceBatchExistsAsync(Tuple<ResourceType, string>[] res);
        #endregion

        #region RPC接口
        /// <summary>
        /// 客户端缓存的房间数据
        /// </summary>
        protected RoomData cachedRoomData;

        void IRoomRPCMethodClient.updateRoomData(RoomData data)
        {
            data.ProxyConvertBack();
            log?.logTrace($"{name} 收到房间数据改变事件。房间数据：{data}");

            cachedRoomData = data;
            OnRoomDataChange?.Invoke(cachedRoomData);
        }

        void IRoomRPCMethodClient.onRoomPropChange(string name, object val)
        {
            if (val is ObjectProxy proxy)
            {
                var json = MessagePack.MessagePackSerializer.ConvertToJson(proxy.Content);
                
                log?.logTrace($"{proxy.Type}: {json}");
                log?.logTrace($"{Convert.ToBase64String(proxy.Content)}");

                val = proxy.ConvertBack();
            }

            log?.logTrace($"{this.name} 收到房间属性改变事件。Key: {name}, Value: {val}");
            cachedRoomData.setProp(name, val);
            PostRoomPropChange?.Invoke(name, val);
            OnRoomDataChange?.Invoke(cachedRoomData);
        }

        void IRoomRPCMethodClient.updatePlayerData(RoomPlayerData data)
        {
            data.ProxyConvertBack();
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
            data.ProxyConvertBack();
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
            if (val is ObjectProxy proxy)
            {
                // log?.logTrace($"{this.name} 收到了一个 ObjectProxy. type: {proxy.Type}, value: {proxy.Content}");
                val = proxy.ConvertBack();
            }
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
            public SendOperation() : base(nameof(INetworkingV3Client.Send))
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
        protected Task<byte[]> sendTo(NetPeer peer, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (peer == null)
                throw new ArgumentNullException(nameof(peer));

            SendOperation<byte[]> op = new SendOperation<byte[]>();
            startOperation(op, () =>
            {
                log?.logWarn($"客户端{name}发送请求超时。");
            });

            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.sendRequest);
            writer.Put(op.id);
            writer.Put(clientID);
            writer.PutBytesWithLength(data);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            return op.task;
        }

        public abstract Task<byte[]> Send(byte[] data);
        #endregion
    }


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