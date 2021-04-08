using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace TouhouCardEngine
{
    public class LobbyClientNetworking : Networking, INetworkingV3Client
    {
        ServerClient serverClient { get; }

        RoomPlayerData localPlayer { get; set; } = null;

        public LobbyClientNetworking(ServerClient servClient, Shared.ILogger logger) : base("lobbyClient", logger)
        {
            serverClient = servClient;
        }

        /// <summary>
        /// host peer 
        /// </summary>
        public NetPeer hostPeer { get; set; } = null;

        /// <summary>
        /// 获取当前用户的数据
        /// </summary>
        /// <returns></returns>
        public RoomPlayerData GetSelfPlayerData()
        {
            // 仅在更换了用户后更新这个PlayerData
            var info = serverClient.GetUserInfoCached();
            if (localPlayer?.id != info.UID)
                localPlayer = new RoomPlayerData(info.UID, info.Name, RoomPlayerType.human);
            
            return localPlayer;
        }

        public async Task<RoomData> CreateRoom()
        {
            // todo: 这里需要与其他地方配合，得到真正的房间信息。
            // 在没有连接到房间之前，房间内玩家是0，所以不设置房间。
            var roomInfo = await serverClient.CreateRoomAsync();
            var roomData = new RoomData(roomInfo.id);
            roomData.ownerId = localPlayer.id;
            return roomData;
        }

        Dictionary<string, BriefRoomInfo> cachedRoomInfos = new Dictionary<string, BriefRoomInfo>();

        /// <summary>
        /// 获取当前服务器的房间信息
        /// </summary>
        /// <returns></returns>
        public async Task<RoomData[]> GetRooms()
        {
            var roomInfos = await serverClient.GetRoomInfosAsync();
            List<RoomData> rooms = new List<RoomData>();

            cachedRoomInfos.Clear();
            foreach (var item in roomInfos)
            {
                cachedRoomInfos.Add(item.id, item);

                var room = new RoomData(item.id);
                room.ownerId = item.ownerID;
                foreach (var p in item.players)
                {
                    var userInfo = await serverClient.GetUserInfoAsync(p, false);
                    room.playerDataList.Add(new RoomPlayerData(p, userInfo.Name, RoomPlayerType.human));
                }
                foreach (var propKV in item.properties)
                {
                    // todo: object convert
                    room.propDict[propKV.Key] = propKV.Value;
                }
            }

            return rooms.ToArray();
        }

        public Task<RoomData> JoinRoom(string roomId)
        {
            if (!cachedRoomInfos.ContainsKey(roomId))
                throw new ArgumentOutOfRangeException("roomID", "指定ID的房间不存在");

            var roomInfo = cachedRoomInfos[roomId];
            var writer = new NetDataWriter();
            // todo: 规定加入的数据格式。
            // 在老的网络中，Connect和JoinRoom是两个操作，但在新的网络中似乎是一个。
            writer.Put(roomId);

            hostPeer = net.Connect(roomInfo.ip, roomInfo.port, writer);
            JoinLobbyRoomOperation op = new JoinLobbyRoomOperation();
            startOperation(op, () =>
            {
                log?.logWarn($"连接到 {roomInfo} 响应超时。");
            });
            return op.task;
        }

        public Task SetRoomProp(string key, object value)
        {
            // todo: 这个应该是调用一个RPC
            return invoke<object>(hostPeer, "setRoomProp", key, value);
        }

        public Task SetPlayerProp(int playerId, string key, object value)
        {
            // todo: 这个应该是调用一个RPC
            return invoke<object>(hostPeer, "setPlayerProp", playerId, key, value);
        }

        class JoinLobbyRoomOperation : Operation<RoomData>
        {
            public JoinLobbyRoomOperation() : base(nameof(LobbyClientNetworking.JoinRoom))
            {
            }
        }

        /// <summary>
        /// 销毁房间。
        /// 这个方法不要使用，请使用QuitRoom退出当前房间，服务器会在没有更多玩家的情况下销毁房间。
        /// </summary>
        /// <returns></returns>
        public Task DestroyRoom()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 修改房间信息
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        public Task AlterRoomInfo(RoomInfo changedInfo)
        {
            // todo: 应该是调用一个RPC，和SetProp差不多
            throw new NotImplementedException();
        }

        /// <summary>
        /// 退出房间
        /// </summary>
        /// <returns></returns>
        public void QuitRoom()
        {
            // 直接断开就好了吧
            net.DisconnectPeer(hostPeer);
        }

        /// <summary>
        /// 获取所有用户信息
        /// </summary>
        /// <returns></returns>
        public Task<RoomPlayerData> QueryAllPlayerData()
        {
            // todo: 请求拉取，或者从缓存中返回一个值
            throw new NotImplementedException();
        }

        public Task SetPlayerProp(string name, object val)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetRoomProp(string name)
        {
            throw new NotImplementedException();
        }

        public Task GameStart()
        {
            throw new NotImplementedException();
        }

        protected override Task OnNetworkReceive(NetPeer peer, NetPacketReader reader, PacketType type)
        {
            return base.OnNetworkReceive(peer, reader, type);
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            // todo
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            // todo
        }

        public void pollEvents()
        {
            throw new NotImplementedException();
        }

        public override Task<T> invoke<T>(string mehtod, params object[] args)
        {
            throw new NotImplementedException();
        }

        protected override Type getType(string typeName)
        {
            return TypeHelper.getType(typeName);
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            throw new NotImplementedException();
        }

        protected override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            throw new NotImplementedException();
        }

        protected override void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            throw new NotImplementedException();
        }
    }

    interface INetworkingV3Client
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
        /// 获取当前课加入的房间信息
        /// </summary>
        /// <remarks>
        /// 对开发者的提示：
        /// 请在实现时缓存详细的IP和端口等信息，方便后面JoinRoom时连接。
        /// </remarks>
        /// <returns></returns>
        Task<RoomData[]> GetRooms();

        /// <summary>
        /// 修改当前房间的信息
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        Task AlterRoomInfo(RoomInfo changedInfo);
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
        /// 获取房间内所有玩家的数据
        /// //? 为啥会有这个API？
        /// </summary>
        /// <returns></returns>
        Task<RoomPlayerData> QueryAllPlayerData();

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
        Task<object> GetRoomProp(string name);

        /// <summary>
        /// 修改房间的属性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        Task SetRoomProp(string name, object val);

        /// <summary>
        /// 开始游戏！
        /// </summary>
        /// <returns></returns>
        Task GameStart();
        #endregion

        #region Game
        // todo
        #endregion
    }
}