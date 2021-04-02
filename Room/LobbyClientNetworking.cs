using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TouhouCardEngine
{
    public class LobbyClientNetworking : ClientNetworking, IClientNetworking
    {
        int connectionTimeout = 3;

        ServerClient serverClient { get; }

        RoomPlayerData localPlayer { get; set; } = null;

        public LobbyClientNetworking(ServerClient servClient)
        {
            serverClient = servClient;
        }

        /// <summary>
        /// host peer 
        /// </summary>
        public NetPeer hostPeer { get; set; } = null;

        /// <summary>
        /// 设置当前用户的信息
        /// 用户登录成功或注销后，应当调用此方法设置/更新用户信息。
        /// </summary>
        /// <param name="info"></param>
        public void SetUserInfo(PublicBasicUserInfo info)
        {
            if (info == null)
            {
                localPlayer = null;
            }
            else
            {
                localPlayer = new RoomPlayerData(info.UID, info.Name, RoomPlayerType.human);
            }
        }

        public RoomPlayerData getLocalPlayerData()
        {
            return localPlayer;
        }

        public async Task<RoomData> createRoom(RoomPlayerData hostPlayerData)
        {
            if (hostPlayerData != null && hostPlayerData != localPlayer)
                throw new ArgumentException("房主玩家和当前玩家不是同一个玩家。");

            // todo: 这里需要与其他地方配合，得到真正的房间信息。
            // 在没有连接到房间之前，房间内玩家是0，所以不设置房间。
            var roomInfo = await serverClient.CreateRoomAsync();
            var roomData = new RoomData(roomInfo.id);
            roomData.ownerId = localPlayer.id;
            return roomData;
        }

        /// <summary>
        /// DO NOT CALL THIS!
        /// 在服务器上无法加入一个新的玩家。
        /// </summary>
        /// <param name="playerData"></param>
        /// <returns></returns>
        public Task addPlayer(RoomPlayerData playerData)
        {
            throw new NotImplementedException();
        }

        Dictionary<string, BriefRoomInfo> cachedRoomInfos = new Dictionary<string, BriefRoomInfo>();

        /// <summary>
        /// 获取当前服务器的房间信息
        /// </summary>
        /// <returns></returns>
        public async Task<RoomData[]> getRooms()
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

        public Task<RoomData> joinRoom(string roomId, RoomPlayerData joinPlayerData)
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

        public Task setRoomProp(string key, object value)
        {
            // todo: 这个应该是调用一个RPC
            return invoke<object>(hostPeer, "setRoomProp", key, value);
        }

        public Task setRoomPlayerProp(int playerId, string key, object value)
        {
            // todo: 这个应该是调用一个RPC
            return invoke<object>(hostPeer, "setRoomPlayerProp", playerId, key, value);
        }

        class JoinLobbyRoomOperation : Operation<RoomData>
        {
            public JoinLobbyRoomOperation() : base(nameof(LobbyClientNetworking.joinRoom))
            {
            }
        }

        protected override Task OnNetworkReceive(NetPeer peer, NetPacketReader reader, PacketType type)
        {
            return base.OnNetworkReceive(peer, reader, type);
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            base.OnPeerConnected(peer);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            base.OnPeerDisconnected(peer, disconnectInfo);
        }

        public void pollEvents()
        {
            throw new NotImplementedException();
        }

        public event Action<RoomData> onRoomDiscovered;
        public event Action<RoomData> onRoomDataChanged;
        public event Action<string> onRemoveRoomNtf;
        public event Action<string, RoomPlayerData> onRoomAddPlayerNtf;
        public event Action<string, int> onRoomRemovePlayerNtf;
        public event Action<string, string, object> onRoomSetPropNtf;
        public event Action<int, string, object> onRoomPlayerSetPropNtf;
    }
}