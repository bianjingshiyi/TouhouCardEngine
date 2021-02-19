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
        public LANNetworking(string name, ILogger logger) : base("LAN", logger)
        {
            _playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), name, RoomPlayerType.human);
            addRPCMethod(this, nameof(reqGetRoom));
            addRPCMethod(this, nameof(ackGetRoom));
            addRPCMethod(this, nameof(ntfNewRoom));
            addRPCMethod(this, nameof(ntfUpdateRoom));
            addRPCMethod(this, nameof(ntfRemoveRoom));
            addRPCMethod(this, nameof(rpcConfirmJoin));
            addRPCMethod(this, nameof(ntfRoomAddPlayer));
            addRPCMethod(this, nameof(ntfRoomRemovePlayer));
            addRPCMethod(this, nameof(ntfRoomSetProp));
            addRPCMethod(this, nameof(rpcRoomPlayerSetProp));
            addRPCMethod(this, nameof(ntfRoomPlayerSetProp));
        }
        public LANNetworking(string name) : this(name, new UnityLogger(name))
        {
        }
        /// <summary>
        /// 局域网默认玩家使用随机Guid，没有玩家名字
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData getLocalPlayerData()
        {
            return _playerData;
        }
        /// <summary>
        /// 局域网创建房间直接返回构造好的房间供ClientLogic持有。
        /// </summary>
        /// <param name="hostPlayerData"></param>
        /// <returns></returns>
        /// <remarks>游戏大厅的话，就应该是返回游戏大厅构造并且保存在列表里的房间了吧</remarks>
        public override Task<RoomData> createRoom(RoomPlayerData hostPlayerData)
        {
            log?.log(name + "创建房间");
            _roomData = new RoomData(Guid.NewGuid().ToString());
            _roomData.playerDataList.Add(hostPlayerData);
            _roomData.ownerId = hostPlayerData.id;
            invokeBroadcast(nameof(ntfNewRoom), _roomData);
            return Task.FromResult(_roomData);
        }
        /// <summary>
        /// 获取房间列表，返回缓存的房间列表
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override Task<RoomData[]> getRooms()
        {
            invokeBroadcast(nameof(reqGetRoom));
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
        public override Task<RoomData> joinRoom(string roomId, RoomPlayerData joinPlayerData)
        {
            if (opList.Any(o => o is JoinRoomOperation))
                throw new InvalidOperationException("客户端已经在执行连接操作");
            //获取缓存的IP地址
            var address = _roomInfoDict[roomId].ip;
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
        /// <summary>
        /// 向房间内的玩家和局域网中的其他玩家发送房间的更新信息。
        /// </summary>
        /// <param name="roomData"></param>
        /// <returns></returns>
        public override Task addPlayer(RoomPlayerData playerData)
        {
            //广播房间中玩家数量变化
            invokeBroadcast(nameof(ntfRoomAddPlayer), _roomData.ID, playerData);
            //向房间中的其他玩家发送通知房间添加玩家
            return Task.WhenAll(_playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(ntfRoomAddPlayer), _roomData.ID, playerData)));
            //invokeAll<object>(_playerInfoDict.Values.Select(i => i.peer), nameof(ntfRoomAddPlayer), _roomData.ID, playerData);
        }
        public override Task setRoomProp(string key, object value)
        {
            //广播房间中的属性变化
            invokeBroadcast(nameof(ntfRoomSetProp), _roomData.ID, key, value);
            //想房间中的其他玩家发送属性变化通知
            return Task.WhenAll(_playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(ntfRoomSetProp), _roomData.ID, key, value)));
        }
        public override async Task setRoomPlayerProp(int playerId, string key, object value)
        {
            if (_roomData.ownerId == playerId)
            {
                //房主，直接广播
                await Task.WhenAll(_playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(ntfRoomPlayerSetProp), playerId, key, value)));
            }
            else
            {
                //其他玩家，向房主请求
                await invoke<object>(nameof(rpcRoomPlayerSetProp), playerId, key, value);
            }
        }
        public override void quitRoom(int playerId)
        {
            if (_roomData == null)
                return;//你本来就不在房间里，退个屁？
            if (_roomData.ownerId == playerId)
            {
                //主机退出了，和所有其他玩家断开连接，然后广播房间没了
                foreach (var peer in _playerInfoDict.Values.Select(i => i.peer))
                {
                    peer.Disconnect();
                }
                _playerInfoDict.Clear();
                invokeBroadcast(nameof(ntfRemoveRoom), _roomData.ID);
            }
            else
            {
                //是其他玩家，直接断开连接
                host.Disconnect();
                host = null;
            }
            _roomData = null;
        }
        /// <summary>
        /// 当局域网中发现新的房间
        /// </summary>
        public override event Action<RoomData> onNewRoomNtf;
        /// <summary>
        /// 当局域网的房间被移除
        /// </summary>
        public override event Action<string> onRemoveRoomNtf;
        /// <summary>
        /// 一次更新一整个房间的开销真的可以，这个事件应该被拆成若干个更新房间的事件。
        /// </summary>
        [Obsolete("使用各种更新房间局部状态的事件作为替代")]
        public override event Action<RoomData> onUpdateRoom;
        /// <summary>
        /// 当局域网收到发现房间的请求的时候被调用，需要返回当前ClientLogic的房间信息。
        /// </summary>
        public event Func<RoomData> onGetRoom;
        /// <summary>
        /// 当玩家请求加入房间的时候，是否回应？
        /// </summary>
        public override event Func<RoomPlayerData, RoomData> onJoinRoomReq;
        /// <summary>
        /// 当玩家确认加入房间的时候，请求房间状况。
        /// </summary>
        public event Func<RoomPlayerData, RoomData> onConfirmJoinReq;
        /// <summary>
        /// 当玩家确认加入房间的时候，收到房间状况的回应。
        /// </summary>
        public event Action<RoomData> onConfirmJoinAck;
        /// <summary>
        /// 当房间添加玩家时收到通知。
        /// </summary>
        public override event Action<string, RoomPlayerData> onRoomAddPlayerNtf;
        /// <summary>
        /// 当房间移除玩家时收到通知。
        /// </summary>
        public override event Action<string, int> onRoomRemovePlayerNtf;
        /// <summary>
        /// 当房间属性更改时收到通知。
        /// </summary>
        public override event Action<string, string, object> onRoomSetPropNtf;
        /// <summary>
        /// 当房间中的玩家更改属性时收到通知。
        /// </summary>
        public override event Action<int, string, object> onRoomPlayerSetPropNtf;
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
            //目前正在进行加入房间操作并且连接上了主机
            if (opList.OfType<JoinRoomOperation>().FirstOrDefault() is JoinRoomOperation joinRoomOperation && peer == host)
            {
                _ = confirmJoin(joinRoomOperation);
            }
        }
        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            log?.log(name + "与" + peer.EndPoint + "断开连接，原因：" + disconnectInfo.Reason + "，错误类型：" + disconnectInfo.SocketErrorCode);
            switch (disconnectInfo.Reason)
            {
                case DisconnectReason.ConnectionRejected:
                    ackJoinRoomReject();
                    break;
                case DisconnectReason.ConnectionFailed:
                    ackJoinRoomFailed();
                    break;
                case DisconnectReason.DisconnectPeerCalled:
                    //与Peer断开连接的本地消息
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
                    if (disconnectInfo.AdditionalData.TryGetInt(out int packetInt))
                    {
                        PacketType packetType = (PacketType)packetInt;
                    }
                    if (peer == host)
                        ntfRemoveRoom(_roomData.ID);
                    else
                        ntfRoomRemovePlayer(_roomData.ID, _playerInfoDict.First(p => p.Value.peer == peer).Key);
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
        void ackGetRoom(RoomData room)
        {
            log?.log(name + "收到获取房间消息");
            _roomInfoDict[room.ID] = new LANRoomInfo()
            {
                ip = unconnectedInvokeIP
            };
            onUpdateRoom?.Invoke(room);
        }
        /// <summary>
        /// 远程调用方法，当收到创建房间消息时被调用
        /// </summary>
        /// <param name="room"></param>
        void ntfNewRoom(RoomData room)
        {
            log?.log(name + "收到创建房间消息");
            updateRoomInfo(room);
            onNewRoomNtf?.Invoke(room);
        }
        void ntfUpdateRoom(RoomData room)
        {
            log?.log(name + "收到房间信息更新消息");
            updateRoomInfo(room);
            onUpdateRoom?.Invoke(room);
        }
        void ntfRemoveRoom(string roomId)
        {
            log?.log(name + "收到房间" + roomId + "移除消息");
            if (_roomInfoDict.ContainsKey(roomId))
                _roomInfoDict.Remove(roomId);
            if (_roomData != null && _roomData.ID == roomId)
                _roomData = null;
            onRemoveRoomNtf?.Invoke(roomId);
        }
        void updateRoomInfo(RoomData room)
        {
            if (!_roomInfoDict.ContainsKey(room.ID))
            {
                _roomInfoDict[room.ID] = new LANRoomInfo()
                {
                    ip = unconnectedInvokeIP
                };
            }
        }
        /// <summary>
        /// 收到玩家加入房间的请求，玩家能否加入房间取决于客户端逻辑（比如房间是否已满）
        /// </summary>
        /// <param name="request"></param>
        /// <param name="player"></param>
        void reqJoinRoom(ConnectionRequest request, RoomPlayerData player)
        {
            RoomData roomData = null;
            try
            {
                roomData = onJoinRoomReq?.Invoke(player);
            }
            catch (Exception e)
            {
                log?.log(request.RemoteEndPoint + "加入房间的请求被拒绝，原因：" + e);
                request.Reject(createRPCResponseWriter(PacketType.joinResponse, e));
                return;
            }
            //接受了加入请求，回复请求并广播房间更新信息。
            NetPeer peer = request.Accept();
            _playerInfoDict[player.id] = new LANPlayerInfo()
            {
                ip = request.RemoteEndPoint,
                peer = peer
            };
            invokeBroadcast(nameof(ntfUpdateRoom), roomData);
        }
        async Task confirmJoin(JoinRoomOperation joinRoomOperation)
        {
            _roomData = await invoke<RoomData>(nameof(rpcConfirmJoin), getLocalPlayerData());
            onConfirmJoinAck.Invoke(_roomData);
            completeOperation(joinRoomOperation, _roomData);
        }
        RoomData rpcConfirmJoin(RoomPlayerData player)
        {
            RoomData data = onConfirmJoinReq.Invoke(player);
            invokeBroadcast(nameof(ntfUpdateRoom), data);
            return data;
        }
        void ackJoinRoomReject()
        {
        }
        void ackJoinRoomFailed()
        {

        }
        void ntfRoomAddPlayer(string roomId, RoomPlayerData playerData)
        {
            log?.log(name + "收到通知房间" + roomId + "加入玩家" + playerData.name);
            onRoomAddPlayerNtf?.Invoke(roomId, playerData);
        }
        void ntfRoomSetProp(string roomId, string propName, object value)
        {
            log?.log(name + "收到通知房间" + roomId + "的属性" + propName + "变更为" + value);
            onRoomSetPropNtf?.Invoke(roomId, propName, value);
        }
        Task rpcRoomPlayerSetProp(int playerId, string propName, object value)
        {
            log?.log(name + "收到远程调用玩家" + playerId + "想要将属性" + propName + "变成为" + value);
            //首先假设玩家的属性他自己爱怎么改就怎么改。
            onRoomPlayerSetPropNtf?.Invoke(playerId, propName, value);
            //然后房主要通知其他玩家属性改变了。
            return Task.WhenAll(_playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(ntfRoomPlayerSetProp), playerId, propName, value)));
        }
        void ntfRoomPlayerSetProp(int playerId, string propName, object value)
        {
            log?.log(name + "收到通知玩家" + playerId + "将属性" + propName + "更改为" + value);
            onRoomPlayerSetPropNtf?.Invoke(playerId, propName, value);
        }
        void ntfRoomRemovePlayer(string roomId, int playerId)
        {
            log?.log(name + "收到通知玩家" + playerId + "退出了房间" + roomId);
            onRoomRemovePlayerNtf?.Invoke(roomId, playerId);
            if (_roomData != null && _roomData.ownerId == _playerData.id && _playerInfoDict.ContainsKey(playerId))
            {
                //是主机，退出的是房间里的人，把这个消息再广播一遍，以及直接通知其他玩家。
                _playerInfoDict.Remove(playerId);
                invokeBroadcast(nameof(ntfRoomRemovePlayer), roomId, playerId);
                foreach (var peer in _playerInfoDict.Values.Select(i => i.peer))
                {
                    notify(peer, nameof(ntfRoomRemovePlayer), roomId, playerId);
                }
            }
        }
        readonly RoomPlayerData _playerData;
        RoomData _roomData;
        Dictionary<string, LANRoomInfo> _roomInfoDict = new Dictionary<string, LANRoomInfo>();
        Dictionary<int, LANPlayerInfo> _playerInfoDict = new Dictionary<int, LANPlayerInfo>();
        class LANRoomInfo
        {
            public IPEndPoint ip;
        }
        class LANPlayerInfo
        {
            public IPEndPoint ip;
            public NetPeer peer;
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