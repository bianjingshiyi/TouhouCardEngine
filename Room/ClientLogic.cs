using NitoriNetwork.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;

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
        public void update()
        {
            curNetwork.net.PollEvents();
        }
        public void Dispose()
        {
            if (clientNetwork != null)
                clientNetwork.Dispose();
            if (LANNetwork != null)
                LANNetwork.Dispose();
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
                if (curNetwork == LANNetwork)
                {
                    LANNetwork.onGetRoomReq -= onDiscoverRoomReq;
                    LANNetwork.onAddOrUpdateRoomAck -= onAddOrUpdateRoomAck;
                }
            }
            if (!LANNetwork.isRunning)
                LANNetwork.start();
            curNetwork = LANNetwork;
            LANNetwork.onGetRoomReq += onDiscoverRoomReq;
            LANNetwork.onAddOrUpdateRoomAck += onAddOrUpdateRoomAck;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port">发送或广播创建房间信息的端口</param>
        /// <returns></returns>
        /// <remarks>port主要是在局域网测试下有用</remarks>
        public async Task createOnlineRoom(int port = -1)
        {
            logger?.log("客户端创建在线房间");
            RoomPlayerData localPlayerData = curNetwork.getLocalPlayerData();
            RoomData data = await curNetwork.createRoom(localPlayerData, port);
            room = new Room(data);
            room.addPlayer(new ClientLocalRoomPlayer(localPlayerData.id));
            localPlayer = room.getPlayer(localPlayerData.id);
        }
        public void refreshRooms(int port = -1)
        {
            logger?.log("客户端刷新房间列表");
            curNetwork.refreshRooms(port);
        }
        public Task<RoomData[]> getRooms(int port = -1)
        {
            logger?.log("客户端请求房间列表");
            return curNetwork.getRooms(port);
        }
        void onAddOrUpdateRoomAck(RoomData roomData)
        {
            logger?.log("客户端更新房间" + roomData.ID);
            if (_lobby.containsId(roomData.ID))
            {
                _lobby.Add(roomData);
                onNewRoom?.Invoke(roomData);
            }
            else
            {
                _lobby.UpdateAt(roomData.ID, roomData);
                onUpdateRoom?.Invoke(roomData);
            }
        }
        public event Action<RoomData> onNewRoom;
        public event Action<RoomData> onUpdateRoom;
        public async Task<bool> joinRoom(RoomData roomData)
        {
            logger?.log("客户端请求加入房间" + roomData);
            roomData = await curNetwork.joinRoom(roomData, curNetwork.getLocalPlayerData());
            if (roomData != null)
            {
                room = new Room(roomData);
                foreach (var playerData in roomData.playerDataList)
                {
                    if (playerData.id == curNetwork.getLocalPlayerData().id)
                        room.addPlayer(new ClientLocalRoomPlayer(playerData.id));
                    else
                        room.addPlayer(new ClientRoomPlayer(playerData.id));
                }
                return true;
            }
            else
                return false;
        }
        public async Task addAIPlayer()
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
        public RoomPlayer localPlayer { get; private set; } = null;
        public Room room { get; private set; } = null;
        /// <summary>
        /// 网络端口
        /// </summary>
        public int port => curNetwork != null ? curNetwork.port : -1;
        #endregion
        #region 私有成员
        private RoomData onDiscoverRoomReq()
        {
            if (room != null)
                return room.data;
            else
                throw new RPCDontResponseException();
        }
        ClientNetworking curNetwork { get; set; } = null;
        LANNetworking LANNetwork { get; }
        ClientNetworking clientNetwork { get; }
        ILogger logger { get; }
        LobbyData _lobby = new LobbyData();
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
}