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
    public class LANNetworking : CommonClientNetwokingV3, IRoomRPCMethodHost, ILANRPCMethodHost, ILANRPCMethodClient, INetworkingV3LANHost
    {
        #region 公共成员
        /// <summary>
        /// 局域网络构造器，包括RPC方法注册。
        /// </summary>
        /// <param name="logger"></param>
        public LANNetworking(string name, ILogger logger) : base("LAN", logger)
        {
            _playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), name, RoomPlayerType.human);

            addRPCMethod(this, typeof(IRoomRPCMethodHost));
            addRPCMethod(this, typeof(ILANRPCMethodHost));
            addRPCMethod(this, typeof(ILANRPCMethodClient));
        }
        public LANNetworking(string name) : this(name, new UnityLogger(name))
        {
        }

        NetPeer hostPeer { get; set; } = null;

        /// <summary>
        /// 局域网默认玩家使用随机Guid，没有玩家名字
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData GetSelfPlayerData()
        {
            return _playerData;
        }

        public override Task<RoomData> CreateRoom()
        {
            log?.log(name + "创建房间");
            _hostRoomData = new RoomData(Guid.NewGuid().ToString());
            _hostRoomData.playerDataList.Add(_playerData);
            _hostRoomData.ownerId = _playerData.id;
            _roomInfo = new LobbyRoomData("127.0.0.1", Port, _hostRoomData.ID, _playerData.id);
            invokeBroadcast(nameof(ILANRPCMethodClient.addDiscoverRoom), _roomInfo);
            return Task.FromResult(_hostRoomData);
        }
        /// <summary>
        /// 获取房间列表，返回缓存的房间列表
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override Task RefreshRoomList()
        {
            invokeBroadcast(nameof(ILANRPCMethodHost.requestDiscoverRoom));
            return Task.CompletedTask;
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
        public override Task<RoomData> JoinRoom(string roomId)
        {
            if (opList.Any(o => o is JoinRoomOperation))
                throw new InvalidOperationException("客户端已经在执行连接操作");
            //获取缓存的IP地址
            if (!_lanRooms.ContainsKey(roomId))
                throw new ArgumentOutOfRangeException($"无法找到ID为{roomId}的房间");

            var roomInfo = _lanRooms[roomId];
            string msg = name + "连接" + roomInfo.IP + ":" + roomInfo.Port;
            log?.log(msg);
            //发送链接请求
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.joinRequest);
            writer.Put(_playerData.ToJson());
            var peer = net.Connect(roomInfo.IP, roomInfo.Port, writer);

            //peer为null表示已经有一个操作在进行了
            if (peer == null)
                throw new InvalidOperationException("客户端已经在执行连接操作");
            else
            {
                hostPeer = peer;
                JoinRoomOperation operation = new JoinRoomOperation();
                startOperation(operation, () =>
                {
                    log?.log(msg + "超时");
                });
                return operation.task;
            }
        }
        /// <summary>
        /// 向房间内的玩家和局域网中的其他玩家发送房间的更新信息。
        /// </summary>
        /// <param name="roomData"></param>
        /// <returns></returns>
        public Task AddPlayer(RoomPlayerData playerData)
        {
            log?.logTrace($"添加玩家{playerData.id}");
            _hostRoomData.playerDataList.Add(playerData);

            return NotifyPlayerDataChange(playerData);
        }

        private Task NotifyPlayerDataChange(RoomPlayerData playerData)
        {
            // 房间公共信息更新
            updateRoomInfo();

            // 提示上层玩家修改了
            invokeOnRoomPlayerDataChanged(_hostRoomData.playerDataList.ToArray());

            // 向房间中的其他玩家发送通知房间添加玩家
            return Task.WhenAll(_playerInfoDict.
                Where(i => i.Key != playerData.id).
                Select(i => invoke<object>(i.Value.peer, nameof(IRoomRPCMethodClient.onPlayerAdd), playerData)));
        }

        public override Task SetRoomProp(string key, object value)
        {
            // 向房间中的其他玩家发送属性变化通知
            return Task.WhenAll(_playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(IRoomRPCMethodClient.onRoomPropChange), key, value)));
        }

        /// <summary>
        /// 当前是否是Host？亦或者是Client
        /// </summary>
        bool isHost => _hostRoomData != null;

        public override async Task SetPlayerProp(string key, object value)
        {
            if (isHost)
            {
                // 本地玩家，直接修改
                setPlayerProp(key, value, GetSelfPlayerData().id);
            }
            else
            {
                //其他玩家，向房主请求
                await invoke<object>(nameof(IRoomRPCMethodHost.setPlayerProp), key, value);
            }
        }
        public override void QuitRoom()
        {
            if (isHost)
            {
                if (_hostRoomData == null)
                {
                    log?.logWarn("尝试退出一个不存在的房间");
                    return;
                }

                // 主机退出了，和所有其他玩家断开连接，然后广播房间没了
                foreach (var peer in _playerInfoDict.Values.Select(i => i.peer))
                {
                    peer.Disconnect();
                }
                _playerInfoDict.Clear();
                invokeBroadcast(nameof(ILANRPCMethodClient.removeDiscoverRoom), _hostRoomData.ID);
                _hostRoomData = null;
            }
            else
            {
                if (cachedRoomData == null)
                {
                    log?.logWarn("尝试退出一个不存在的房间");
                    return;
                }

                // 是其他玩家，直接断开连接
                hostPeer.Disconnect();
                hostPeer = null;
                cachedRoomData = null;
            }
        }

        public override Task DestroyRoom()
        {
            QuitRoom();
            return Task.CompletedTask;
        }

        public override int GetLatency()
        {
            return isHost ? 0 : latencyAvg.Size;
        }

        /// <summary>
        /// 更新房间信息
        /// </summary>
        /// <param name="newInfo"></param>
        /// <returns></returns>
        public override Task AlterRoomInfo(LobbyRoomData newInfo)
        {
            // todo: 更新房间信息
            invokeBroadcast(nameof(ILANRPCMethodClient.addDiscoverRoom), _roomInfo);
            return Task.CompletedTask;
        }

        public override T GetRoomProp<T>(string name)
        {
            if (isHost)
            {
                return _hostRoomData.getProp<T>(name);
            }
            else
            {
                return cachedRoomData.getProp<T>(name);
            }
        }

        public override Task GameStart()
        {
            if (isHost)
            {
                invokeOnGameStart();
                return Task.WhenAll(_playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(IRoomRPCMethodClient.onGameStart))));
            }
            return Task.CompletedTask;
        }

        public override async Task Send(object obj)
        {
            if (isHost)
            {
                await Task.WhenAll(_playerInfoDict.Values.Select(i => sendTo(i.peer, obj)));
            }
            else
            {
                await sendTo(hostPeer, obj);
            }
        }

        public override async Task<T> invoke<T>(string mehtod, params object[] args)
        {
            if (!isHost)
            {
                return await invoke<T>(hostPeer, mehtod, args);
            }

            // 这个方法实际上仅限Client调用Host的方法，所以直接在这里throw掉就好。
            throw new NotImplementedException();
        }
        /// <summary>
        /// 房间列表更新事件
        /// </summary>
        public override event Action<LobbyRoomDataList> OnRoomListUpdate;

        /// <summary>
        /// 一次更新一整个房间的开销真的可以，这个事件应该被拆成若干个更新房间的事件。
        /// </summary>
        [Obsolete("使用各种更新房间局部状态的事件作为替代", true)]
        public event Action<RoomData> onUpdateRoom;
        /// <summary>
        /// 当玩家请求加入房间的时候，是否回应？
        /// 检查玩家信息和房间信息，判断是否可以加入
        /// 请在这个事件发生时把玩家信息加入到房间列表，防止多人同时连接。
        /// </summary>
        public event Func<RoomPlayerData, RoomData> onJoinRoomReq;
        /// <summary>
        /// 当玩家确认加入房间的时候，请求房间状况。
        /// 就是给玩家返回一个房间信息用的
        /// </summary>
        public event Func<RoomPlayerData, RoomData> onConfirmJoinReq;
        /// <summary>
        /// 当玩家确认加入房间的时候，收到房间状况的回应。
        /// </summary>
        public event Action<RoomData> onConfirmJoinAck;

        public event Action<string, int> onConfirmJoinNtf;
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
            var op = getOperation<JoinRoomOperation>();
            if (op != null && peer == hostPeer)
            {
                _ = confirmJoin(op);
            }
        }
        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            log?.log(name + "与" + peer.EndPoint + "断开连接，原因：" + disconnectInfo.Reason + "，错误类型：" + disconnectInfo.SocketErrorCode);
            switch (disconnectInfo.Reason)
            {
                case DisconnectReason.DisconnectPeerCalled:
                    // 与Peer断开连接的本地消息
                    break;
                case DisconnectReason.RemoteConnectionClose:
                    if (disconnectInfo.AdditionalData.TryGetInt(out int packetInt))
                    {
                        PacketType packetType = (PacketType)packetInt;
                    }
                    if (peer == hostPeer)
                        (this as ILANRPCMethodClient).removeDiscoverRoom(cachedRoomData.ID);
                    else
                        removeRoomPlayer(_playerInfoDict.First(p => p.Value.peer == peer).Key);
                    break;
                default:
                    break;
            }
            // 底层处理了一点点加入时候Disconnect的异常，最好看一眼。
            base.OnPeerDisconnected(peer, disconnectInfo);
        }

        protected override Task OnNetworkReceive(NetPeer peer, NetPacketReader reader, PacketType type)
        {
            switch (type)
            {
                case PacketType.sendRequest:
                    var peers = _playerInfoDict.Select(p => p.Value.peer).ToArray();
                    this.SendRequestForwarder(peers, reader);
                    return Task.CompletedTask;
                default:
                    break;
            }
            return base.OnNetworkReceive(peer, reader, type);
        }

        SlidingAverage latencyAvg = new SlidingAverage(10);
        protected override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // todo: host下各个客户端的平均延迟的处理
            if (peer == hostPeer)
            {
                latencyAvg.Push(latency);
            }
        }

        /// <summary>
        /// 当前RPC请求的Peer
        /// </summary>
        NetPeer currentPeer { get; set; }
        protected override object invokeRPCMethod(NetPeer peer, string methodName, object[] args)
        {
            currentPeer = peer;
            try
            {
                return base.invokeRPCMethod(peer, methodName, args);
            }
            finally
            {
                currentPeer = null;
            }
        }

        #endregion
        #region 私有成员
        /// <summary>
        /// LAN广播搜索局域网上现有房间请求
        /// 以广播形式发送此请求。
        /// </summary>
        void ILANRPCMethodHost.requestDiscoverRoom()
        {
            log?.logTrace(name + "收到请求房间消息");
            invoke(unconnectedInvokeIP, nameof(ILANRPCMethodClient.addDiscoverRoom), _roomInfo);
        }

        /// <summary>
        /// 缓存的局域网房间列表
        /// </summary>
        LobbyRoomDataList _lanRooms = new LobbyRoomDataList();

        void ILANRPCMethodClient.addDiscoverRoom(LobbyRoomData room)
        {
            log?.logTrace(name + "收到创建/发送房间消息");
            room.SetIP(unconnectedInvokeIP.Address.ToString());
            _lanRooms[room.RoomID] = room;

            OnRoomListUpdate?.Invoke(_lanRooms);
        }

        void ILANRPCMethodClient.removeDiscoverRoom(string roomID)
        {
            log?.logTrace(name + "收到删除房间消息");
            if (_lanRooms.ContainsKey(roomID))
                _lanRooms.Remove(roomID);

            OnRoomListUpdate?.Invoke(_lanRooms);
        }

        void ILANRPCMethodClient.updateDiscoverRoom(LobbyRoomData roomData)
        {
            log?.logTrace(name + "收到更新房间消息");
            roomData.SetIP(unconnectedInvokeIP.Address.ToString());
            _lanRooms[roomData.RoomID] = roomData;

            OnRoomListUpdate?.Invoke(_lanRooms);
        }

        /// <summary>
        /// 收到玩家加入房间的请求，玩家能否加入房间取决于客户端逻辑（比如房间是否已满）
        /// </summary>
        /// <param name="request"></param>
        /// <param name="player"></param>
        void reqJoinRoom(ConnectionRequest request, RoomPlayerData player)
        {
            log?.logTrace(request.RemoteEndPoint + $"({player.id}) 请求加入房间。");
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
            // invokeBroadcast(nameof(ntfRoomAddPlayer), roomData.ID, player);
            // invokeBroadcast(nameof(ntfUpdateRoom), roomData);
        }

        /// <summary>
        /// 成功连接上主机后的后续操作
        /// </summary>
        /// <param name="joinRoomOperation"></param>
        /// <returns></returns>
        async Task confirmJoin(JoinRoomOperation joinRoomOperation)
        {
            cachedRoomData = await invoke<RoomData>(nameof(IRoomRPCMethodHost.requestJoinRoom), GetSelfPlayerData());
            onConfirmJoinAck?.Invoke(cachedRoomData);
            completeOperation(joinRoomOperation, cachedRoomData);
        }

        RoomData IRoomRPCMethodHost.requestJoinRoom(RoomPlayerData player)
        {
            log?.logTrace($"接收到玩家{player.id}加入请求。");
            RoomData data = onConfirmJoinReq.Invoke(player);
            // 更新房间信息
            // 不需要在这里添加玩家信息，玩家信息已经在连接时就添加了。
            _ = NotifyPlayerDataChange(player);
            return data;
        }

        void ackJoinRoomReject()
        {
        }
        void ackJoinRoomFailed()
        {

        }

        void IRoomRPCMethodHost.setPlayerProp(string name, object value)
        {
            var playerID = _playerInfoDict.Where(p => p.Value.peer == currentPeer).Select(p => p.Key).FirstOrDefault();
            log?.logTrace(name + "收到远程调用玩家" + playerID + "想要将属性" + name + "变成为" + value);
            setPlayerProp(name, value, playerID);
        }

        private void setPlayerProp(string name, object value, int playerID)
        {
            // 首先假设玩家的属性他自己爱怎么改就怎么改。
            _hostRoomData.setPlayerProp(playerID, name, value);
            // 通知上层变更
            invokeOnRoomPlayerDataChanged(_hostRoomData.playerDataList.ToArray());
            // 然后房主要通知其他玩家属性改变了。
            _playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(IRoomRPCMethodClient.onPlayerPropChange), playerID, name, value));
        }

        private void removeRoomPlayer(int playerId)
        {
            if (_hostRoomData != null && _hostRoomData.ownerId == _playerData.id && _playerInfoDict.ContainsKey(playerId))
            {
                // 是主机，退出的是房间里的人，把这个消息再广播一遍，以及直接通知其他玩家。
                _playerInfoDict.Remove(playerId);
                // 移除实际的房间玩家
                _hostRoomData.playerDataList.RemoveAll(p => p.id == playerId);
                // 提示上层玩家修改了
                invokeOnRoomPlayerDataChanged(_hostRoomData.playerDataList.ToArray());
                // 更新房间公共信息
                updateRoomInfo();
                // 广播
                foreach (var peer in _playerInfoDict.Values.Select(i => i.peer))
                {
                    notify(peer, nameof(IRoomRPCMethodClient.onPlayerRemove), playerId);
                }
            }
        }

        /// <summary>
        /// 更新房间信息
        /// </summary>
        private void updateRoomInfo()
        {
            _roomInfo.PlayerCount = _hostRoomData.playerDataList.Count;
            AlterRoomInfo(_roomInfo);
        }

        /// <summary>
        /// 当前玩家信息
        /// </summary>
        RoomPlayerData _playerData { get; }
        /// <summary>
        /// 主机房间数据
        /// </summary>
        RoomData _hostRoomData { get; set; }

        /// <summary>
        /// 公共房间信息
        /// </summary>
        LobbyRoomData _roomInfo { get; set; }

        /// <summary>
        /// 玩家信息字典
        /// </summary>
        Dictionary<int, LANPlayerInfo> _playerInfoDict = new Dictionary<int, LANPlayerInfo>();

        class LANPlayerInfo
        {
            public IPEndPoint ip;
            public NetPeer peer;
        }
        #endregion
    }
}