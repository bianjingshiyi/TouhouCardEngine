using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;

namespace TouhouCardEngine
{
    public class LobbyClientNetworking : CommonClientNetwokingV3
    {
        /// <summary>
        /// 服务器通信客户端
        /// </summary>
        ServerClient serverClient { get; }

        /// <summary>
        /// 本地玩家
        /// </summary>
        RoomPlayerData localPlayer { get; set; } = null;

        /// <summary>
        /// 主机对端
        /// </summary>
        public NetPeer hostPeer { get; set; } = null;

        public LobbyClientNetworking(ServerClient servClient, Shared.ILogger logger) : base("lobbyClient", logger)
        {
            serverClient = servClient;
        }

        #region 外部方法实现

        /// <summary>
        /// 获取当前用户的数据
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData GetSelfPlayerData()
        {
            // 仅在更换了用户后更新这个PlayerData
            var info = serverClient.GetUserInfo(serverClient.UID);
            if (localPlayer?.id != info.UID)
                localPlayer = new RoomPlayerData(info.UID, info.Name, RoomPlayerType.human);

            return localPlayer;
        }

        /// <summary>
        /// 创建一个空房间，房主为自己
        /// </summary>
        /// <returns></returns>
        public async override Task<RoomData> CreateRoom()
        {
            // step 1: 在服务器上创建房间
            var roomInfo = await serverClient.CreateRoomAsync();
            // step 2: 加入这个房间
            return await joinRoom(roomInfo);
        }

        public override event Action<LobbyRoomDataList> OnRoomListUpdate;

        /// <summary>
        /// 缓存的服务器上房间列表
        /// </summary>
        LobbyRoomDataList lobby = new LobbyRoomDataList();

        /// <summary>
        /// 获取当前服务器的房间信息
        /// </summary>
        /// <returns></returns>
        public async override Task RefreshRoomList()
        {
            var roomInfos = await serverClient.GetRoomInfosAsync();
            lobby.Clear();
            foreach (var item in roomInfos)
            {
                lobby[item.RoomID] = item;
            }
            OnRoomListUpdate?.Invoke(lobby);
        }

        public override Task<RoomData> JoinRoom(string roomId)
        {
            if (!lobby.ContainsKey(roomId))
                throw new ArgumentOutOfRangeException("roomID", "指定ID的房间不存在");

            var roomInfo = lobby[roomId];
            return joinRoom(roomInfo);
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        private Task<RoomData> joinRoom(LobbyRoomData roomInfo)
        {
            var writer = new NetDataWriter();

            GetSelfPlayerData(); // 更新缓存的player数据

            writer.Put(roomInfo.RoomID);
            writer.Put(serverClient.UserSession);
            writer.Put(localPlayer.ToJson());
            writer.Put(localPlayer.id);
            log.logTrace($"尝试以 {localPlayer.id}: {serverClient.UserSession} 连接");

            hostPeer = net.Connect(roomInfo.IP, roomInfo.Port, writer);
            JoinRoomOperation op = new JoinRoomOperation();
            startOperation(op, () =>
            {
                log?.logWarn($"连接到 {roomInfo} 响应超时。");
            });
            return op.task;
        }

        /// <summary>
        /// 销毁房间。
        /// 这个方法不要使用，请使用QuitRoom退出当前房间，服务器会在没有更多玩家的情况下销毁房间。
        /// </summary>
        /// <returns></returns>
        public override Task DestroyRoom()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 修改房间信息
        /// 暂时没有能够修改的房间信息，也没有对应的服务器接口，先不实现
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        public override Task AlterRoomInfo(LobbyRoomData changedInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 退出房间
        /// </summary>
        /// <returns></returns>
        public override void QuitRoom()
        {
            // 直接断开就好了吧
            net.DisconnectPeer(hostPeer);
        }

        public override T GetRoomProp<T>(string name)
        {
            return cachedRoomData.getProp<T>(name);
        }

        public override Task SetRoomProp(string key, object value)
        {
            return invoke<object>(nameof(IRoomRPCMethodLobby.setRoomProp), key, value);
        }

        public override Task SetPlayerProp(string name, object val)
        {
            return invoke<object>(nameof(IRoomRPCMethodLobby.setPlayerProp), name, val);
        }

        public override Task GameStart()
        {
            return invoke<object>(nameof(IRoomRPCMethodLobby.gameStart));
        }

        public override Task<T> Send<T>(object obj)
        {
            return sendTo<T>(hostPeer, obj);
        }

        public override Task SendChat(int channel, string message)
        {
            return invoke<object>(nameof(IRoomRPCMethodLobby.sendChat), channel, message);
        }
        #endregion

        #region 交互逻辑
        async Task requestJoinRoom()
        {
            var op = getOperation(typeof(JoinRoomOperation));
            if (op == null)
            {
                log?.logWarn($"{name} 当前没有加入房间的操作，但是却想要发出加入房间的请求。");
                return;
            }

            var roomInfo = await invoke<RoomData>(nameof(IRoomRPCMethodLobby.requestJoinRoom), GetSelfPlayerData());
            cachedRoomData = roomInfo;
            invokeOnJoinRoom(cachedRoomData);
            completeOperation(op, roomInfo);
        }
        #endregion
        #region 底层实现
        public override Task<T> invoke<T>(string method, params object[] args)
        {
            return invoke<T>(hostPeer, method, args);
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
            log?.logWarn($"另一个客户端({request.RemoteEndPoint})尝试连接到本机({name})，由于当前网络是客户端网络故拒绝。");
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            if (peer == hostPeer)
            {
                _ = requestJoinRoom();
            }
        }

        SlidingAverage latencyAvg = new SlidingAverage(10);
        protected override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peer == hostPeer)
            {
                latencyAvg.Push(latency);
            }
        }

        public override int GetLatency()
        {
            return (int)latencyAvg.GetAvg();
        }
        #endregion
    }
}