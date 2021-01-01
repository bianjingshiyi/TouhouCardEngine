using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NitoriNetwork.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    /// <summary>
    /// 网络的局域网实现
    /// </summary>
    public class LANNetworking : ClientNetworking
    {
        #region 公共成员
        /// <summary>
        /// 局域网络构造器，包括RPC方法注册。
        /// </summary>
        /// <param name="logger"></param>
        public LANNetworking(ILogger logger = null) : base("LAN", logger)
        {
            addRPCMethod(this, GetType().GetMethod(nameof(ackCreateRoom)));
            addRPCMethod(this, GetType().GetMethod(nameof(reqGetRoom)));
            addRPCMethod(this, GetType().GetMethod(nameof(ackGetRoom)));
            addRPCMethod(this, GetType().GetMethod(nameof(ackJoinRoom)));
        }
        /// <summary>
        /// 局域网默认玩家使用随机Guid，没有玩家名字
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData getLocalPlayerData()
        {
            if (_playerData == null)
                _playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "玩家1", RoomPlayerType.human);
            return _playerData;
        }
        /// <summary>
        /// 局域网创建房间直接返回构造好的房间供ClientLogic持有。
        /// </summary>
        /// <param name="hostPlayerData"></param>
        /// <returns></returns>
        /// <remarks>游戏大厅的话，就应该是返回游戏大厅构造并且保存在列表里的房间了吧</remarks>
        public override Task<RoomData> createRoom(RoomPlayerData hostPlayerData, int port = -1)
        {
            log?.log(name + "创建房间");
            RoomData data = new RoomData(Guid.NewGuid().ToString());
            data.playerDataList.Add(hostPlayerData);
            data.ownerId = hostPlayerData.id;
            invokeBroadcast(nameof(ackCreateRoom), port, data);
            return Task.FromResult(data);
        }
        /// <summary>
        /// 远程调用方法，当收到创建房间消息时被调用
        /// </summary>
        /// <param name="data"></param>
        public void ackCreateRoom(RoomData data)
        {
            log?.log(name + "收到创建房间消息");
            _roomInfoDict[data] = new LANRoomInfo()
            {
                ip = unconnectedInvokeIP
            };
            onAddOrUpdateRoomAck?.Invoke(data);
        }
        public event Action<RoomData> onAddOrUpdateRoomAck;
        /// <summary>
        /// 广播一个刷新房间列表的消息。
        /// </summary>
        /// <param name="port"></param>
        public override void refreshRooms(int port = -1)
        {
            log?.log(name + "刷新房间");
            invokeBroadcast(nameof(reqGetRoom), port);
        }
        public void reqGetRoom()
        {
            log?.log(name + "收到请求房间消息");
            RoomData roomData = onGetRoomReq?.Invoke();
            invoke(unconnectedInvokeIP, nameof(ackGetRoom), roomData);
        }
        /// <summary>
        /// 当局域网收到发现房间的请求的时候被调用，需要返回当前ClientLogic的房间信息。
        /// </summary>
        public event Func<RoomData> onGetRoomReq;
        public void ackGetRoom(RoomData roomData)
        {
            log?.log(name + "收到获取房间消息");
            _roomInfoDict[roomData] = new LANRoomInfo()
            {
                ip = unconnectedInvokeIP
            };
            onAddOrUpdateRoomAck?.Invoke(roomData);
        }
        /// <summary>
        /// 获取房间列表，在局域网实现下实际上是返回发现的第一个房间。
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override async Task<RoomData[]> getRooms(int port = -1)
        {
            RoomData data = await invokeBroadcastAny<RoomData>("discoverRoom", port);
            return new RoomData[] { data };
        }
        /// <summary>
        /// 被远程调用的发现房间方法，提供事件接口给ClientLogic用于回复存在的房间。
        /// </summary>
        /// <returns></returns>
        public RoomData discoverRoom()
        {
            return onGetRoomReq?.Invoke();
        }
        /// <summary>
        /// 添加AI玩家，实际上就是直接构造玩家数据然后返回给ClientLogic，在此之前通知其他玩家。
        /// </summary>
        /// <returns></returns>
        public override Task<RoomPlayerData> addAIPlayer()
        {
            RoomPlayerData aiPlayerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai);
            //通知其他玩家添加AI玩家
            return Task.FromResult(aiPlayerData);
        }
        /// <summary>
        /// 加入指定的房间，客户端必须提供自己的玩家数据
        /// </summary>
        /// <param name="roomData"></param>
        /// <param name="joinPlayerData"></param>
        /// <returns></returns>
        /// <remarks>
        /// 局域网实现思路，RoomData必然对应局域网中一个ip，连接这个ip，
        /// 收到连接请求，要客户端来检查是否可以加入，不能加入就拒绝并返回一个异常
        /// 能加入的话，接受，然后等待玩家连接上来。
        /// </remarks>
        public override Task<RoomData> joinRoom(RoomData roomData, RoomPlayerData joinPlayerData)
        {
            if (opList.Any(o => o is JoinRoomOperation))
                throw new InvalidOperationException("客户端已经在执行连接操作");
            var address = _roomInfoDict[roomData].ip;
            string msg = name + "连接" + address;
            log?.log(msg);
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.joinRequest);
            writer.Put(joinPlayerData.ToJson());
            var peer = net.Connect(address, writer);
            if (peer == null)
                throw new InvalidOperationException("客户端已经在执行连接操作");
            else
            {
                host = peer;
                JoinRoomOperation operation = new JoinRoomOperation();
                startOperation(operation, () =>
                {
                    logger?.Log(msg + "超时");
                });
                return operation.task;
            }
        }
        public event Func<RoomPlayerData,RoomData> onJoinRoomReq;
        #endregion
        #region 私有成员
        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            PacketType packetType = (PacketType)request.Data.GetInt();
            switch (packetType)
            {
                case PacketType.joinRequest:
                    RoomPlayerData joinPlayerData = BsonSerializer.Deserialize<RoomPlayerData>(request.Data.GetString());
                    reqJoinRoom(request, joinPlayerData);
                    break;
                default:
                    log?.log(name + "收到未知的请求连接类型");
                    break;
            }
        }
        protected override void OnPeerConnected(NetPeer peer)
        {
            
        }
        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            log?.log(name + "与" + peer.EndPoint + "断开连接，原因：" + disconnectInfo.Reason + "，错误类型：" + disconnectInfo.SocketErrorCode);
            PacketType packetType = (PacketType)disconnectInfo.AdditionalData.GetInt();
            switch (disconnectInfo.Reason)
            {
                case DisconnectReason.ConnectionRejected:
                    ackJoinRoomReject();
                    break;
                case DisconnectReason.ConnectionFailed:
                    break;
                case DisconnectReason.DisconnectPeerCalled:
                    break;
                case DisconnectReason.HostUnreachable:
                    break;
                case DisconnectReason.InvalidProtocol:
                    break;
                case DisconnectReason.NetworkUnreachable:
                    break;
                case DisconnectReason.Reconnect:
                    break;
                case DisconnectReason.RemoteConnectionClose:
                    break;
                case DisconnectReason.Timeout:
                    break;
                case DisconnectReason.UnknownHost:
                    break;
                default:
                    break;
            }
            base.OnPeerDisconnected(peer, disconnectInfo);
        }
        /// <summary>
        /// 收到玩家加入房间的请求，玩家能否加入房间取决于客户端逻辑（比如房间是否已满）
        /// </summary>
        /// <param name="request"></param>
        /// <param name="player"></param>
        void reqJoinRoom(ConnectionRequest request, RoomPlayerData player)
        {
            try
            {
                RoomData roomData = onJoinRoomReq?.Invoke(player);
                invoke(request.RemoteEndPoint, nameof(ackJoinRoom), roomData);
            }
            catch (Exception e)
            {
                log?.log(request.RemoteEndPoint + "加入房间的请求被拒绝，原因：" + e);
                request.Reject(createRPCResponseWriter(PacketType.joinResponse, e));
                return;
            }
            request.Accept();
        }

        /// <summary>
        /// 加入一方收到加入成功确认，得到房间信息
        /// </summary>
        /// <param name="roomData">房间信息</param>
        public void ackJoinRoom(RoomData roomData) {
            JoinRoomOperation operation = opList.OfType<JoinRoomOperation>().FirstOrDefault();
            completeOperation(operation, roomData);
        }
        void ackJoinRoomReject()
        {
        }
        RoomPlayerData _playerData;
        Dictionary<RoomData, LANRoomInfo> _roomInfoDict = new Dictionary<RoomData, LANRoomInfo>();
        class LANRoomInfo
        {
            public IPEndPoint ip;
        }
        class JoinRoomOperation : Operation<RoomData>
        {
            public JoinRoomOperation() : base(nameof(LANNetworking.joinRoom))
            {
            }
        }
        #endregion
    }
}