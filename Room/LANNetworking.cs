using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NitoriNetwork.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    partial class ClientLogic
    {

    }
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
            addRPCMethod(this, nameof(reqGetRoom));
            addRPCMethod(this, nameof(ackGetRoom));
            addRPCMethod(this, nameof(ntfNewRoom));
            addRPCMethod(this, nameof(ackJoinRoom));
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
        public override Task<RoomData> createRoom(RoomPlayerData hostPlayerData, int[] ports = null)
        {
            log?.log(name + "创建房间");
            RoomData data = new RoomData(Guid.NewGuid().ToString());
            data.playerDataList.Add(hostPlayerData);
            data.ownerId = hostPlayerData.id;
            invokeBroadcast(nameof(ntfNewRoom), ports, data);
            return Task.FromResult(data);
        }
        /// <summary>
        /// 获取房间列表，返回缓存的房间列表
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override Task<RoomData[]> getRooms(int[] ports = null)
        {
            invokeBroadcast(nameof(reqGetRoom), ports);
            return Task.FromResult(new RoomData[0]);
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
            //获取缓存的IP地址
            var address = _roomInfoDict[roomData.ID].ip;
            string msg = name + "连接" + address;
            log?.log(msg);
            //发送链接请求
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.joinRequest);
            writer.Put(joinPlayerData.ToJson());
            var peer = net.Connect(address, writer);
            //peer为null表示已经有一个操作在进行了
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
        public override event Action<RoomData> onNewRoom;
        [Obsolete("应该注册更高层的onNewRoom")]
        public event Action<RoomData> onAddOrUpdateRoomAck;
        /// <summary>
        /// 广播一个刷新房间列表的消息。
        /// </summary>
        /// <param name="port"></param>
        public override void refreshRooms(int[] ports = null)
        {
            log?.log(name + "刷新房间");
            invokeBroadcast(nameof(reqGetRoom), ports);
        }
        public override event Action<RoomData> onUpdateRoom;
        /// <summary>
        /// 被远程调用的发现房间方法，提供事件接口给ClientLogic用于回复存在的房间。
        /// </summary>
        /// <returns></returns>
        public RoomData discoverRoom()
        {
            return onGetRoom?.Invoke();
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
        public override event Func<RoomPlayerData, RoomData> onJoinRoomReq;
        #endregion
        #region 方法重写
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
        #endregion
        #region 私有成员
        void reqGetRoom()
        {
            log?.log(name + "收到请求房间消息");
            RoomData roomData = onGetRoom?.Invoke();
            invoke(unconnectedInvokeIP, nameof(ackGetRoom), roomData);
        }
        void ackGetRoom(RoomData roomData)
        {
            log?.log(name + "收到获取房间消息");
            _roomInfoDict[roomData.ID] = new LANRoomInfo()
            {
                ip = unconnectedInvokeIP
            };
            onUpdateRoom?.Invoke(roomData);
        }
        /// <summary>
        /// 远程调用方法，当收到创建房间消息时被调用
        /// </summary>
        /// <param name="data"></param>
        void ntfNewRoom(RoomData data)
        {
            log?.log(name + "收到创建房间消息");
            _roomInfoDict[data.ID] = new LANRoomInfo()
            {
                ip = unconnectedInvokeIP
            };
            onNewRoom?.Invoke(data);
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
                onUpdateRoom?.Invoke(roomData);
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
        public void ackJoinRoom(RoomData roomData)
        {
            JoinRoomOperation operation = opList.OfType<JoinRoomOperation>().FirstOrDefault();
            onUpdateRoom?.Invoke(roomData);
            completeOperation(operation, roomData);
        }
        void ackJoinRoomReject()
        {
        }
        RoomPlayerData _playerData;
        Dictionary<string, LANRoomInfo> _roomInfoDict = new Dictionary<string, LANRoomInfo>();
        /// <summary>
        /// 当局域网收到发现房间的请求的时候被调用，需要返回当前ClientLogic的房间信息。
        /// </summary>
        public event Func<RoomData> onGetRoom;
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