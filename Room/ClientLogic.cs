using NitoriNetwork.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    public partial class ClientLogic : IDisposable
    {
        const int MAX_PLAYER_COUNT = 2;

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
            room = new RoomData(string.Empty);
            localPlayer = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "本地玩家", RoomPlayerType.human);
            room.playerDataList.Add(localPlayer);
            room.ownerId = localPlayer.id;
        }
        public void switchNetToLAN()
        {
            if (curNetwork != null)
            {
                //切换网络注销事件。
                LANNetwork.onGetRoom -= onGetRoom;
                curNetwork.onNewRoom -= onNewOrUpdateRoomAck;
                curNetwork.onUpdateRoom -= onNewOrUpdateRoomAck;
                curNetwork.onJoinRoomReq -= onJoinRoomReq;
            }
            if (!LANNetwork.isRunning)
                LANNetwork.start();
            curNetwork = LANNetwork;
            LANNetwork.onGetRoom += onGetRoom;
            curNetwork.onNewRoom += onNewOrUpdateRoomAck;
            curNetwork.onUpdateRoom += onNewOrUpdateRoomAck;
            curNetwork.onJoinRoomReq += onJoinRoomReq;
        }
        public RoomPlayerData getLocalPlayerData()
        {
            return curNetwork.getLocalPlayerData();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="port">发送或广播创建房间信息的端口</param>
        /// <returns></returns>
        /// <remarks>port主要是在局域网测试下有用</remarks>
        public async Task createOnlineRoom(int[] ports = null)
        {
            logger?.log("客户端创建在线房间");
            RoomPlayerData localPlayerData = curNetwork.getLocalPlayerData();
            RoomData room = await curNetwork.createRoom(localPlayerData, ports);
            this.room = room;
            //this.room.maxPlayerCount = MAX_PLAYER_COUNT;
            localPlayer = localPlayerData;
        }
        public void refreshRooms(int[] ports = null)
        {
            logger?.log("客户端刷新房间列表");
            curNetwork.refreshRooms(ports);
        }
        public Task<RoomData[]> getRooms(int[] ports = null)
        {
            logger?.log("客户端请求房间列表");
            return curNetwork.getRooms();
        }
        void onNewOrUpdateRoomAck(RoomData roomData)
        {
            logger?.log("客户端更新房间" + roomData.ID);
            lobby.updateOrAddRoom(roomData, out var newRoom);
            if (newRoom)
                onNewRoom?.Invoke(roomData);
            else
                onUpdateRoom.Invoke(roomData);
        }
        public async Task<bool> joinRoom(RoomData room)
        {
            logger?.log("客户端请求加入房间" + room);
            room = await curNetwork.joinRoom(room, curNetwork.getLocalPlayerData());
            if (room != null)
            {
                this.room = room;
                return true;
            }
            else
                return false;
        }
        public async Task addAIPlayer()
        {
            if (curNetwork == null)
                room.playerDataList.Add(new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai));
            else
            {
                RoomPlayerData aiPlayerData = await curNetwork.addAIPlayer();
                room.playerDataList.Add(aiPlayerData);
            }
        }
        public Task setRoomProp(string propName, object value)
        {
            throw new NotImplementedException();
        }
        public Task setPlayerProp(string propName, object value)
        {
            throw new NotImplementedException();
        }
        public Task quitRoom()
        {
            throw new NotImplementedException();
        }
        public event Action<RoomData> onNewRoom;
        public event Action<RoomData> onUpdateRoom;
        public RoomPlayerData localPlayer { get; private set; } = null;
        public RoomData room { get; private set; } = null;
        public Lobby lobby { get; } = new Lobby();
        /// <summary>
        /// 网络端口
        /// </summary>
        public int port => curNetwork != null ? curNetwork.port : -1;
        #endregion
        #region 私有成员
        private RoomData onGetRoom()
        {
            if (room != null)
                return room;
            else
                throw new RPCDontResponseException();
        }
        private RoomData onJoinRoomReq(RoomPlayerData player)
        {
            if (room == null)
                throw new InvalidOperationException("房间不存在");
            if (room.playerDataList.Count < room.maxPlayerCount)
            {
                room.playerDataList.Add(player);
                return room;
            }
            else
                throw new InvalidOperationException("房间已满");
        }
        ClientNetworking curNetwork { get; set; } = null;
        public LANNetworking LANNetwork { get; }
        ClientNetworking clientNetwork { get; }
        ILogger logger { get; }
        //LobbyData _lobby = new LobbyData();
        #endregion
    }
}