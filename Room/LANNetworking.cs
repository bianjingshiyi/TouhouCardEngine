﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using NitoriNetwork.Common;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    /// <summary>
    /// 网络的局域网实现
    /// </summary>
    public class LANNetworking : CommonClientNetwokingV3, INetworkingV3LANHost
    {
        #region 公共成员
        #region 构造器
        /// <summary>
        /// 局域网络构造器，包括RPC方法注册。
        /// </summary>
        /// <param name="logger"></param>
        public LANNetworking(string name, ILogger logger, IResourceProvider resProvider = null) : base("LAN", logger)
        {
            // 初始化资源服务器
            // todo: 替换这一实际逻辑
            ResProvider = resProvider ?? new SimpleResourceProvider(Path.Combine(UnityEngine.Application.persistentDataPath, "cache"));
            ResServ = new ResourceServerLite(logger, ResProvider);
            // 局域网的玩家应当随机分配玩家名称
            var playerID = Guid.NewGuid().GetHashCode();
            var playerName = $"Player#{((uint)playerID) % 10000}";
            _playerData = new RoomPlayerData(playerID, playerName, RoomPlayerType.human);

            addRPCMethod(typeof(LanOperator));
        }
        public LANNetworking(string name) : this(name, new UnityLogger(name))
        {
        }
        #endregion

        #region 房间相关
        public override Task<RoomData> CreateRoom(int maxPlayerCount, string name, string password)
        {
            log?.log($"{name}创建房间");
            _hostRoomData = new RoomData(Guid.NewGuid().ToString());
            _hostRoomData.playerDataList.Add(_playerData);
            _hostRoomData.ownerId = _playerData.id;
            _hostRoomData.maxPlayerCount = maxPlayerCount;
            _hostRoomData.setProp(RoomData.PROP_ROOM_NAME, name);
            _hostRoomData.setProp(RoomData.PROP_ROOM_PASSWORD, password);

            _publicRoomData = new LobbyRoomData("127.0.0.1", Port, _hostRoomData.ID, _playerData.id, _hostRoomData.maxPlayerCount, _hostRoomData.playerDataList.Count, name, password);
            invokeBroadcast(nameof(ILANRPCMethodClient.addDiscoverRoom), _publicRoomData.ToMaskedData());

            // 创建资源服务器
            pollCancelToken = new CancellationTokenSource();
            var token = pollCancelToken.Token;
            pollTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    ResServ.Routine();
                }
            });

            return Task.FromResult(_hostRoomData);
        }
        /// <summary>
        /// 获取房间列表，返回缓存的房间列表
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override Task RefreshRoomList()
        {
            _lanRooms.Clear();
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
        public override Task<RoomData> JoinRoom(string roomId, string password)
        {
            //获取缓存的IP地址
            if (!_lanRooms.ContainsKey(roomId))
                throw new ArgumentOutOfRangeException($"无法找到ID为{roomId}的房间");

            var roomInfo = _lanRooms[roomId];
            return JoinRoom(roomInfo.IP, roomInfo.Port, password);
        }

        public Task<RoomData> JoinRoom(string addr, int port, string password)
        {
            if (opList.Any(o => o is JoinRoomOperation))
                throw new InvalidOperationException("客户端已经在执行连接操作");

            string msg = $"{name}连接{addr}:{port}";
            log?.log(msg);
            // 发送连接请求
            RoomJoinRequest request = new RoomJoinRequest("", password, "", _playerData);
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.joinRequest);
            request.Write(writer);
            var peer = net.Connect(addr, port, writer);

            //peer为null表示已经有一个操作在进行了
            if (peer == null)
                throw new InvalidOperationException("客户端已经在执行连接操作");
            else
            {
                hostPeer = peer;
                JoinRoomOperation operation = new JoinRoomOperation();
                startOperation(operation, () =>
                {
                    log?.log($"{msg}超时");
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

            return castPlayerAddedAsHost(playerData);
        }

        public override Task SetRoomProp(string key, object value)
        {
            value = handleRoomProp(key, value);

            // 向房间中的其他玩家发送属性变化通知
            var tasks = _playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(IRoomRPCMethodClient.onRoomPropChange), key, ObjectProxy.TryProxy(value)));
            return Task.WhenAll(tasks);
        }


        public override Task SetRoomPropBatch(List<KeyValuePair<string, object>> values)
        {
            foreach (var item in values)
            {
                handleRoomProp(item.Key, item.Value);
            }
            // 向房间中的其他玩家发送属性变化通知
            var tasks = _playerInfoDict.Values.Select(i => invoke<object>(i.peer, nameof(IRoomRPCMethodClient.updateRoomData), _hostRoomData.GetProxiedClone()));
            return Task.WhenAll(tasks);
        }

        public override async Task SetPlayerProp(string key, object value)
        {
            if (isHost)
            {
                // 房主，直接修改
                castPlayerPropAsHost(key, value, GetSelfPlayerData().id);
            }
            else
            {
                //其他玩家，向房主请求
                await invoke<object>(nameof(IRoomRPCMethodHost.setPlayerProp), key, ObjectProxy.TryProxy(value));
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

                // 取消资源服务器
                if (pollTask != null && !pollTask.IsCompleted)
                {
                    pollCancelToken.Cancel();
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

        /// <summary>
        /// 向所有大厅的玩家广播更新房间信息
        /// </summary>
        /// <param name="newInfo"></param>
        /// <returns></returns>
        public override Task AlterRoomInfo(LobbyRoomData newInfo)
        {
            // 更新房间信息
            invokeBroadcast(nameof(ILANRPCMethodClient.addDiscoverRoom), newInfo.ToMaskedData());
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
                return invokeAll<object>(_playerInfoDict.Values.Select(i => i.peer), nameof(IRoomRPCMethodClient.onGameStart));
            }
            return Task.CompletedTask;
        }

        public override Task SendChat(int channel, string message)
        {
            if (isHost)
            {
                var playerID = GetSelfPlayerData().id;
                var msg = new ChatMsg(channel, playerID, message);
                castChatAsHost(msg);
                return Task.CompletedTask;
            }
            else
            {
                return invoke<object>(nameof(IRoomRPCMethodHost.sendChat), channel, message);
            }
        }
        public override Task SuggestCardPools(CardPoolSuggestion suggestion)
        {
            if (isHost)
            {
                log?.logWarn("房主不应该调用这个方法提议卡池！");
                return Task.CompletedTask;
            }
            else
            {
                return invoke<object>(nameof(IRoomRPCMethodHost.suggestCardPools), suggestion);
            }
        }
        public override Task AnwserCardPoolsSuggestion(int playerId, CardPoolSuggestion suggestion, bool agree)
        {
            if (isHost)
            {
                var suggesterPeer = _playerInfoDict.Where(p => p.Key == playerId).Select(p => p.Value.peer).FirstOrDefault();
                return invoke<object>(suggesterPeer, nameof(IRoomRPCMethodClient.onCardPoolSuggestionAnwsered), suggestion, agree);
            }
            return Task.CompletedTask;
        }
        #endregion

        #region 资源相关
        public override Task<byte[]> GetResourceAsync(ResourceType type, string id)
        {
            if (isHost)
            {
                // 局域网房主直接复制资源。
                string resType = type.GetString();
                if (ResProvider.ResourceInfo(resType, id, out long length))
                {
                    using (var stream = ResProvider.OpenReadResource(resType, id))
                    {
                        byte[] buffer = new byte[length];
                        stream.Read(buffer, 0, (int)length);
                        return Task.FromResult(buffer);
                    }
                }
                return Task.FromResult<byte[]>(null);
            }
            else
            {
                return ResClient.GetResourceAsync(type, id);
            }
        }
        public override Task UploadResourceAsync(ResourceType type, string id, byte[] bytes)
        {
            if (isHost)
            {
                // 局域网房主直接复制资源。
                string resType = type.GetString();
                if (!ResProvider.ResourceInfo(resType, id, out _))
                {
                    using (var stream = ResProvider.OpenWriteResource(resType, id, bytes.LongLength))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                return Task.CompletedTask;
            }
            else
            {
                return ResClient.UploadResourceAsync(type, id, bytes);
            }
        }
        public override Task<bool> ResourceExistsAsync(ResourceType type, string id)
        {
            if (isHost)
            {
                return Task.FromResult(ResProvider.ResourceInfo(type.GetString(), id, out _));
            }
            else
            {
                return ResClient.ResourceExistsAsync(type, id);
            }
        }
        public override Task<bool[]> ResourceBatchExistsAsync(Tuple<ResourceType, string>[] res)
        {
            if (isHost)
            {
                bool[] results = new bool[res.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    results[i] = ResProvider.ResourceInfo(res[i].Item1.GetString(), res[i].Item2, out _);
                }
                return Task.FromResult(results);
            }

            return ResClient.ResourceExistsBatchAsync(res);
        }
        #endregion

        public override bool start(int port = 0)
        {
            bool success = base.start(port);
            if (!success) return success;

            success = ResServ.Start(net.LocalPort);
            if (!success) net.Stop();

            return success;
        }
        /// <summary>
        /// 局域网默认玩家使用随机Guid，没有玩家名字
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData GetSelfPlayerData()
        {
            return _playerData;
        }
        public override int GetLatency()
        {
            return isHost ? 0 : latencyAvg.Size;
        }
        public override async Task<byte[]> Send(byte[] data)
        {
            if (isHost)
            {
                this.SendRequest(_playerInfoDict.Values.Select(i => i.peer), _playerData.id, -1, data);
                await invokeOnReceive(_playerData.id, data);
                return data;
            }
            else
            {
                return await sendTo(hostPeer, data);
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
        #endregion

        #region 私有成员

        #region 事件回调
        /// <summary>
        /// 房主接收到连接请求时
        /// </summary>
        /// <param name="request"></param>
        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            PacketType packetType = (PacketType)request.Data.GetInt();
            switch (packetType)
            {
                case PacketType.joinRequest:
                    RoomJoinRequest joinRequest = new RoomJoinRequest(request.Data);
                    receiveConnectionRequest(request, joinRequest);
                    break;
                default:
                    log?.log($"{name}收到未知的请求连接类型");
                    break;
            }
        }
        /// <summary>
        /// 玩家成功和房间建立连接
        /// </summary>
        /// <param name="peer"></param>
        protected override void OnPeerConnected(NetPeer peer)
        {
            // 目前正在进行加入房间操作并且连接上了主机
            var op = getOperation<JoinRoomOperation>();
            if (op != null && peer == hostPeer)
            {
                _ = sendJoinRoomRequest(op);
            }
        }
        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            log?.log($"{name}与{peer.EndPoint}断开连接，原因：{disconnectInfo.Reason}，错误类型：{disconnectInfo.SocketErrorCode}");
            switch (disconnectInfo.Reason)
            {
                case DisconnectReason.DisconnectPeerCalled:
                    // 与Peer断开连接的本地消息
                    break;
                case DisconnectReason.RemoteConnectionClose:
                    if (peer == hostPeer)
                        removeRoomFromList(cachedRoomData.ID);
                    else
                    {
                        var first = _playerInfoDict.FirstOrDefault(p => p.Value.peer == peer);
                        if (first.Value != null)
                        {
                            removeRoomPlayer(first.Key);
                        }
                    }
                    break;
                default:
                    break;
            }
            // 底层处理了一点点加入时候Disconnect的异常，最好看一眼。
            base.OnPeerDisconnected(peer, disconnectInfo);

            // 更新当前允许的IP列表
            updateResServAllowedIPs();
        }
        protected override async Task OnNetworkReceive(NetPeer peer, NetPacketReader reader, PacketType type)
        {
            switch (type)
            {
                case PacketType.sendRequest:
                    // 交给上层处理
                    var result = reader.ParseRequest(out int clientID, out int requestID);
                    await invokeOnReceive(clientID, result);

                    // 转发给其他客户端
                    var peers = _playerInfoDict.Select(p => p.Value.peer).ToArray();
                    this.SendRequest(peers, clientID, requestID, result);
                    return;
                default:
                    break;
            }
            await base.OnNetworkReceive(peer, reader, type);
        }
        protected override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // todo: host下各个客户端的平均延迟的处理
            if (peer == hostPeer)
            {
                latencyAvg.Push(latency);
            }
        }

        #endregion

        #region RPC
        [Obsolete]
        protected override object invokeRPCMethod(NetPeer peer, string methodName, object[] args)
        {
            var playerID = _playerInfoDict.Where(p => p.Value.peer == peer).Select(p => p.Key).FirstOrDefault();
            return executor.invokeMethod(methodName, args, new object[] { this, peer.EndPoint, playerID });
        }
        protected override RPCResponseV3 invokeRPCMethod(NetPeer peer, RPCRequestV3 request)
        {
            var playerID = _playerInfoDict.Where(p => p.Value.peer == peer).Select(p => p.Key).FirstOrDefault();
            return executor.invoke(request, new object[] { this, peer.EndPoint, playerID });
        }
        protected override RPCResponseV3 invokeRPCMethod(IPEndPoint ip, RPCRequestV3 request)
        {
            return executor.invoke(request, new object[] { this, ip, 0 });
        }
        #endregion

        #region 房间列表
        /// <summary>
        /// 向房间列表中加入房间。
        /// </summary>
        /// <param name="room"></param>
        private void addRoomToList(LobbyRoomData room)
        {
            _lanRooms[room.RoomID] = room;
            OnRoomListUpdate?.Invoke(_lanRooms);
        }

        /// <summary>
        /// 从房间列表中移除房间。
        /// </summary>
        /// <param name="roomID"></param>
        private void removeRoomFromList(string roomID)
        {
            if (_lanRooms.ContainsKey(roomID))
                _lanRooms.Remove(roomID);

            OnRoomListUpdate?.Invoke(_lanRooms);
        }

        private void updateRoomInList(LobbyRoomData roomData)
        {
            _lanRooms[roomData.RoomID] = roomData;
            OnRoomListUpdate?.Invoke(_lanRooms);
        }
        #endregion

        #region 房间连接/加入
        // 加入房间分以下步骤：
        // 1、玩家尝试与房间建立连接（JoinRoom）
        // 2、房主收到连接请求并同意（receiveConnectionRequest）
        // 3、玩家建立连接成功（onPeerConnected）

        // 4、建立连接成功后，发送进入房间请求（sendJoinRoomRequest）
        // 5、房主同意房间加入请求（acceptJoinRoomRequest）
        // 6、玩家成功加入房间（succeedJoinRoomRequest）


        /// <summary>
        /// 房主收到玩家连接的请求，玩家能否加入房间取决于客户端逻辑（比如房间是否已满）
        /// </summary>
        /// <param name="request"></param>
        /// <param name="req"></param>
        void receiveConnectionRequest(ConnectionRequest request, RoomJoinRequest req)
        {
            var player = req.player;
            log?.logTrace($"{request.RemoteEndPoint}({player.id}) 请求加入房间。");

            if (_publicRoomData.IsLocked && req.roomPassword != _publicRoomData.Password)
            {
                log?.log($"{request.RemoteEndPoint}密码错误，拒绝加入房间。");
                var rejectResult = new RejectResult("密码错误");
                var response = new RPCResponseV3(rejectResult);
                var writer = createRPCResponseWriter(PacketType.joinResponse, response);
                request.Reject(writer);
                return;
            }

            RoomData roomData = null;
            try
            {
                roomData = onJoinRoomReq?.Invoke(player);
            }
            catch (Exception e)
            {
                log?.log($"{request.RemoteEndPoint}加入房间的请求被拒绝，原因：{e}");
                var rejectResult = new RejectResult(e.Message);
                var response = new RPCResponseV3(rejectResult);
                var writer = createRPCResponseWriter(PacketType.joinResponse, response);
                request.Reject(writer);
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

            updateResServAllowedIPs();
        }
        /// <summary>
        /// 客户成功连接上主机，请求服务器同意加入请求
        /// </summary>
        /// <param name="joinRoomOperation"></param>
        /// <returns></returns>
        async Task sendJoinRoomRequest(JoinRoomOperation joinRoomOperation)
        {
            // 请求服务器同意加入请求
            cachedRoomData = await invoke<RoomData>(nameof(IRoomRPCMethodHost.requestJoinRoom), GetSelfPlayerData());
            succeedJoinRoomRequest(joinRoomOperation);
        }
        /// <summary>
        /// 房主同意加入请求的操作
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        RoomData acceptJoinRoomRequest(RoomPlayerData player)
        {
            RoomData data = onConfirmJoinReq.Invoke(player);
            // 更新房间信息
            // 不需要在这里添加玩家信息，玩家信息已经在连接时就添加了。
            _ = castPlayerAddedAsHost(player);
            return data.GetProxiedClone();
        }
        /// <summary>
        /// 房间加入请求被同意，加入房间
        /// </summary>
        /// <param name="joinRoomOperation"></param>
        private void succeedJoinRoomRequest(JoinRoomOperation joinRoomOperation)
        {
            cachedRoomData.ProxyConvertBack();
            // 更新资源客户端的信息
            ResClient = new ResourceClient($"http://{hostPeer.EndPoint.Address}:{hostPeer.EndPoint.Port}");

            invokeOnJoinRoom(cachedRoomData);
            completeOperation(joinRoomOperation, cachedRoomData);
        }
        #endregion

        #region 房主操作
        /// <summary>
        /// 以房主的身份转发或发送聊天消息
        /// </summary>
        /// <param name="msg"></param>
        private void castChatAsHost(ChatMsg msg)
        {
            // 通知房主本地客户端
            onReceiveChatCallback(msg);
            // 转发给其他客户端
            foreach (var p in _playerInfoDict.Values)
            {
                if (p.peer != hostPeer)
                    invoke<object>(p.peer, nameof(IRoomRPCMethodClient.onRecvChat), msg.Channel, msg.Sender, msg.Message);
            }
        }

        /// <summary>
        /// 以房主的身份转发玩家属性更改
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="playerID"></param>
        private void castPlayerPropAsHost(string name, object value, int playerID)
        {
            setLocalPlayerProp(_hostRoomData, playerID, name, value);
            // 然后房主要通知其他玩家属性改变了。
            foreach (var p in _playerInfoDict.Values)
            {
                if (p.peer != hostPeer)
                    invoke<object>(p.peer, nameof(IRoomRPCMethodClient.onPlayerPropChange), playerID, name, value);
            }
        }

        /// <summary>
        /// 以房主的身份转发玩家加入
        /// </summary>
        /// <param name="playerData"></param>
        private Task castPlayerAddedAsHost(RoomPlayerData playerData)
        {
            // 房间公共信息更新
            updateRoomInfo();

            // 提示房主本地，玩家修改了
            onRoomPlayerDataListChangedCallback(_hostRoomData.playerDataList.ToArray());

            // 向房间中，出了新加入之外的其他玩家发送通知房间添加玩家
            var otherPlayers = _playerInfoDict.Where(i => i.Key != playerData.id);
            var tasks = otherPlayers.Select(i => invoke<object>(i.Value.peer, nameof(IRoomRPCMethodClient.onPlayerAdd), playerData));
            return Task.WhenAll(tasks);
        }
        /// <summary>
        /// 房主移除一个玩家。
        /// </summary>
        /// <param name="playerId"></param>
        private void removeRoomPlayer(int playerId)
        {
            if (!isHost) //不是房主
                return;
            if (!_playerInfoDict.ContainsKey(playerId)) // 房间中没有这个玩家
                return;
            // 是主机，退出的是房间里的人，把这个消息再广播一遍，以及直接通知其他玩家。
            _playerInfoDict.Remove(playerId);
            // 移除本地数据的玩家。
            removePlayerAsLocal(_hostRoomData, playerId);
            // 更新房间公共信息
            updateRoomInfo();
            // 广播
            foreach (var peer in _playerInfoDict.Values.Select(i => i.peer))
            {
                notify(peer, nameof(IRoomRPCMethodClient.onPlayerRemove), playerId);
            }
        }
        #endregion

        /// <summary>
        /// 更新资源服务器的允许连接列表
        /// </summary>
        void updateResServAllowedIPs()
        {
            var addrs = _playerInfoDict.Select(p => p.Value.peer.EndPoint.Address).ToArray();
            ResServ.SetAllowedIPs(addrs);
        }

        /// <summary>
        /// 更新房间信息
        /// </summary>
        private void updateRoomInfo()
        {
            syncRoomInfo();
            AlterRoomInfo(_publicRoomData);
        }

        /// <summary>
        /// 反向同步房间数据。注意房间名称和密码不会同步
        /// </summary>
        private void syncRoomInfo()
        {
            _publicRoomData.PlayerCount = _hostRoomData.playerDataList.Count;
            _publicRoomData.MaxPlayerCount = _hostRoomData.maxPlayerCount;
            _publicRoomData.OwnerId = _hostRoomData.ownerId;
        }

        private object handleRoomProp(string key, object value)
        {
            // 对特殊Property做处理，需要更新外部的房间信息
            switch (key)
            {
                case RoomData.PROP_ROOM_PASSWORD:
                    _publicRoomData.Password = value as string;
                    // Mask掉，防止泄露
                    value = string.IsNullOrEmpty(value as string) ? "" : "******";
                    _hostRoomData.setProp(key, value);
                    // 通知外部更新
                    invokeBroadcast(nameof(ILANRPCMethodClient.addDiscoverRoom), _publicRoomData.ToMaskedData());
                    break;
                case RoomData.PROP_ROOM_NAME:
                    // 通知外部更新
                    _publicRoomData.RoomName = value as string;
                    invokeBroadcast(nameof(ILANRPCMethodClient.addDiscoverRoom), _publicRoomData.ToMaskedData());
                    break;
                default:
                    // 对于房主而言，其他Property已经在调用此方法的外部更新了
                    // 所以不需要手动在这里更新对应Property
                    break;
            }

            return value;
        }
        #endregion

        #region 事件
        /// <summary>
        /// 房间列表更新事件
        /// </summary>
        public override event Action<LobbyRoomDataList> OnRoomListUpdate;
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
        #endregion

        #region 属性字段
        SlidingAverage latencyAvg = new SlidingAverage(10);
        ResourceServerLite ResServ { get; }
        IResourceProvider ResProvider { get; }
        Task pollTask { get; set; }
        CancellationTokenSource pollCancelToken { get; set; }
        NetPeer hostPeer { get; set; } = null;
        /// <summary>
        /// 当前是否是Host？亦或者是Client
        /// </summary>
        bool isHost => _hostRoomData != null;
        /// <summary>
        /// 缓存的局域网房间列表
        /// </summary>
        LobbyRoomDataList _lanRooms = new LobbyRoomDataList();
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
        LobbyRoomData _publicRoomData { get; set; }
        /// <summary>
        /// 玩家信息字典，只对房主有效有，用于记录IP和Peer（不包括房主本身）
        /// </summary>
        Dictionary<int, LANPlayerInfo> _playerInfoDict = new Dictionary<int, LANPlayerInfo>();
        LobbyRoomData publicRoomData
        {
            get
            {
                if (_publicRoomData == null || _hostRoomData == null)
                    return null;
                syncRoomInfo();
                return _publicRoomData;
            }
        }
        #endregion

        #region 内嵌类
        /// <summary>
        /// 网络相关处理层
        /// </summary>
        class LanOperator : IRoomRPCMethodHost, ILANRPCMethodClient, ILANRPCMethodHost
        {
            LANNetworking nw { get; }
            ILogger log => nw.log;
            IPEndPoint ip { get; } = null;
            int uid { get; } = 0;

            public LanOperator(LANNetworking nw, IPEndPoint from, int playerID)
            {
                this.nw = nw;
                ip = from;
                uid = playerID;
            }

            public RoomData requestJoinRoom(RoomPlayerData player)
            {
                log?.logTrace($"接收到玩家{player.id}加入请求。");
                return nw.acceptJoinRoomRequest(player);
            }
            public void setPlayerProp(string name, object value)
            {
                log?.logTrace($"{name}收到远程调用玩家{uid}想要将属性{name}变成为{value}");
                nw.castPlayerPropAsHost(name, value, uid);
            }

            public void sendChat(int channel, string message)
            {
                nw.castChatAsHost(new ChatMsg(channel, uid, message));
            }

            public void suggestCardPools(CardPoolSuggestion suggestion)
            {
                // 通知房主本地客户端
                nw.invokeOnCardPoolSuggested(uid, suggestion);
            }

            /// <summary>
            /// LAN广播搜索局域网上现有房间请求
            /// 以广播形式发送此请求。
            /// </summary>
            public void requestDiscoverRoom()
            {
                log?.logTrace("收到请求房间消息");
                if (nw.publicRoomData != null)
                {
                    nw.invoke(ip, nameof(ILANRPCMethodClient.addDiscoverRoom), nw.publicRoomData);
                }
            }

            public void addDiscoverRoom(LobbyRoomData room)
            {
                log?.logTrace($"收到创建/发送房间消息");
                room.SetIP(ip.Address.ToString());
                nw.addRoomToList(room);
            }

            public void removeDiscoverRoom(string roomID)
            {
                log?.logTrace($"收到删除房间消息");
                nw.removeRoomFromList(roomID);
            }

            public void updateDiscoverRoom(LobbyRoomData roomData)
            {
                log?.logTrace("收到更新房间消息");
                roomData.SetIP(ip.Address.ToString());
                nw.updateRoomInList(roomData);
            }
        }
        class LANPlayerInfo
        {
            public IPEndPoint ip;
            public NetPeer peer;
        }
        #endregion
    }
}