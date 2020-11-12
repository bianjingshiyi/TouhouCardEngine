using LiteNetLib;
using NitoriNetwork.Common;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;
using LiteNetLib.Utils;
namespace TouhouCardEngine
{
    public class ClientLogic : IDisposable
    {
        #region 公共成员
        public ClientLogic(ILogger logger = null)
        {
            this.logger = logger;
            clientNetwork = new ClientNetworking(logger: logger);
            LANNetwork = new LANNetworking(logger: logger);
        }
        public void createLocalRoom()
        {
            logger?.log("客户端创建本地房间");
            room = new Room();
            localPlayer = new LocalRoomPlayer();
            room.addPlayer(localPlayer, new RoomPlayerData("本地玩家", RoomPlayerType.human));
            room.data.ownerId = localPlayer.id;
        }
        public void switchNetToLAN()
        {
            if (curNetwork != null)
            {
                //切换网络注销事件。
            }
            if (!LANNetwork.isRunning)
                LANNetwork.start();
            curNetwork = LANNetwork;
        }
        public async Task createOnlineRoom()
        {
            logger?.log("客户端创建房间");
            RoomPlayerData localPlayerData = curNetwork.getJoinPlayerData();
            RoomData data = await curNetwork.createRoom(localPlayerData);
            room = new Room(data);
            room.addPlayer(new ClientLocalRoomPlayer(localPlayerData.id));
            localPlayer = room.getPlayer(localPlayerData.id);
        }
        public Task<RoomData[]> getRooms()
        {
            return curNetwork.getRooms();
        }
        public async Task joinOnlineRoom(RoomData roomData)
        {
            roomData = await curNetwork.joinRoom(roomData, curNetwork.getJoinPlayerData());
            room = new Room(roomData);
            foreach (var playerData in roomData.playerDataList)
            {
                if (playerData.id == curNetwork.getJoinPlayerData().id)
                    room.addPlayer(new ClientLocalRoomPlayer(playerData.id));
                else
                    room.addPlayer(new ClientRoomPlayer(playerData.id));
            }
        }
        public async Task roomAddAIPlayer()
        {
            if (curNetwork == null)
            {
                room.addPlayer(new AIRoomPlayer(), new RoomPlayerData("AI", RoomPlayerType.ai));
            }
            else
            {
                RoomPlayerData aiPlayerData = await curNetwork.addAIPlayer();
                room.addPlayer(new AIRoomPlayer(aiPlayerData.id), aiPlayerData);
            }
        }
        public void Dispose()
        {
            if (clientNetwork != null)
                clientNetwork.Dispose();
            if (LANNetwork != null)
                LANNetwork.Dispose();
        }
        public RoomPlayer localPlayer { get; private set; } = null;
        public Room room { get; private set; } = null;
        #endregion
        #region 私有成员
        ClientNetworking curNetwork { get; set; } = null;
        LANNetworking LANNetwork { get; }
        ClientNetworking clientNetwork { get; }
        ILogger logger { get; }
        #endregion
    }
    class ClientLocalRoomPlayer : LocalRoomPlayer
    {
        public ClientLocalRoomPlayer(int id) : base(id)
        {
        }
    }
    class ClientRoomPlayer : RoomPlayer
    {
        public ClientRoomPlayer(int id) : base(id)
        {
        }
    }
    public class LANNetworking : ClientNetworking
    {
        #region 公共成员
        public LANNetworking(ILogger logger = null) : base("LAN", logger)
        {
        }
        /// <summary>
        /// 局域网默认玩家使用随机Guid，没有玩家名字
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData getJoinPlayerData()
        {
            if (_playerData == null)
                _playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "玩家1", RoomPlayerType.human);
            return _playerData;
        }
        public override Task<RoomData> createRoom(RoomPlayerData hostPlayerData)
        {
            RoomData data = new RoomData();
            data.playerDataList.Add(hostPlayerData);
            data.ownerId = hostPlayerData.id;
            return Task.FromResult(data);
        }
        public override async Task<RoomData[]> getRooms()
        {
            RoomData data = await invokeBroadcast<RoomData>("discoverRoom");
            return new RoomData[] { data };
        }
        public override Task<RoomPlayerData> addAIPlayer()
        {
            RoomPlayerData aiPlayerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai);
            //通知其他玩家添加AI玩家
            return Task.FromResult(aiPlayerData);
        }
        #endregion
        #region 私有成员
        RoomPlayerData _playerData;
        #endregion
    }
}