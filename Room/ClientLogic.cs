using NitoriNetwork.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    public partial class ClientLogic : IDisposable
    {
        const int MAX_PLAYER_COUNT = 2;

        #region 公共成员
        public ClientLogic(string name, ILogger logger)
        {
            this.logger = logger;
            clientNetwork = new ClientNetworking(logger: logger);
            LANNetwork = new LANNetworking(name, logger);
        }
        public void update()
        {
            curNetwork.pollEvents();
            //curNetwork.net.PollEvents();
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
                curNetwork.onNewRoomNtf -= onNewOrUpdateRoomNtf;
                //curNetwork.onUpdateRoom -= onNewOrUpdateRoomNtf;
                curNetwork.onRemoveRoomNtf -= onRemoveRoomNtf;
                curNetwork.onJoinRoomReq -= onJoinRoomReq;
                curNetwork.onRoomAddPlayerNtf -= onRoomAddPlayerNtf;
                curNetwork.onRoomRemovePlayerNtf -= onRoomRemovePlayerNtf;
                curNetwork.onRoomSetPropNtf -= onRoomSetPropNtf;
                curNetwork.onRoomPlayerSetPropNtf -= onRoomPlayerSetPropNtf;
                if (curNetwork == LANNetwork)
                {
                    LANNetwork.onGetRoom -= onGetRoomReq;
                    LANNetwork.onConfirmJoinReq -= onConfirmJoinReq;
                    LANNetwork.onConfirmJoinAck -= onConfirmJoinAck;
                }
            }
            if (!LANNetwork.isRunning)
                LANNetwork.start();
            curNetwork = LANNetwork;
            curNetwork.onNewRoomNtf += onNewOrUpdateRoomNtf;
            //curNetwork.onUpdateRoom += onNewOrUpdateRoomNtf;
            curNetwork.onRemoveRoomNtf += onRemoveRoomNtf;
            curNetwork.onJoinRoomReq += onJoinRoomReq;
            curNetwork.onRoomAddPlayerNtf += onRoomAddPlayerNtf;
            curNetwork.onRoomRemovePlayerNtf += onRoomRemovePlayerNtf;
            curNetwork.onRoomSetPropNtf += onRoomSetPropNtf;
            curNetwork.onRoomPlayerSetPropNtf += onRoomPlayerSetPropNtf;
            if (curNetwork == LANNetwork)
            {
                LANNetwork.onGetRoom += onGetRoomReq;
                LANNetwork.onConfirmJoinReq += onConfirmJoinReq;
                LANNetwork.onConfirmJoinAck += onConfirmJoinAck;
            }
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
        public async Task createOnlineRoom()
        {
            logger?.log("客户端创建在线房间");
            RoomPlayerData localPlayerData = curNetwork.getLocalPlayerData();
            RoomData room = await curNetwork.createRoom(localPlayerData);
            localPlayer = localPlayerData;
            this.room = room;
            lobby.addRoom(room);
            //this.room.maxPlayerCount = MAX_PLAYER_COUNT;
        }
        //public void refreshRooms()
        //{
        //    logger?.log("客户端刷新房间列表");
        //    curNetwork.refreshRooms();
        //}
        public Task<RoomData[]> getRooms()
        {
            logger?.log("客户端请求房间列表");
            return curNetwork.getRooms();
        }
        public async Task<bool> joinRoom(string roomId)
        {
            logger?.log("客户端请求加入房间" + roomId);
            RoomPlayerData joinPlayerData = curNetwork.getLocalPlayerData();
            return await curNetwork.joinRoom(roomId, joinPlayerData) != null;
        }
        public Task addAIPlayer()
        {
            logger?.log("主机添加AI玩家");
            RoomPlayerData playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai);
            room.playerDataList.Add(playerData);
            if (curNetwork != null)
                return curNetwork.addPlayer(playerData);
            return Task.CompletedTask;
        }
        public Task setRoomProp(string propName, object value)
        {
            logger?.log("主机更改房间属性" + propName + "为" + value);
            room.setProp(propName, value);
            if (curNetwork != null)
                return curNetwork.setRoomProp(propName, value);
            return Task.CompletedTask;
        }
        public async Task setPlayerProp(string propName, object value)
        {
            logger?.log("玩家更改房间属性" + propName + "为" + value);
            room.setPlayerProp(localPlayer.id, propName, value);
            if (curNetwork != null)
                await curNetwork.setRoomPlayerProp(localPlayer.id, propName, value);
        }
        public Task quitRoom()
        {
            logger?.log("玩家退出房间" + room.ID);
            room = null;
            if (curNetwork != null)
                curNetwork.quitRoom(localPlayer.id);
            return Task.CompletedTask;
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
        public LANNetworking LANNetwork { get; }
        public IClientNetworking curNetwork { get; set; } = null;
        #endregion
        #region 私有成员
        private RoomData onGetRoomReq()
        {
            if (room != null)
                return room;
            else
                throw new RPCDontResponseException();
        }
        void onNewOrUpdateRoomNtf(RoomData roomData)
        {
            logger?.log("客户端更新房间" + roomData.ID);
            lobby.updateOrAddRoom(roomData, out var newRoom);
            if (newRoom)
                onNewRoom?.Invoke(roomData);
            else
                onUpdateRoom.Invoke(roomData);
        }
        void onRemoveRoomNtf(string roomId)
        {
            if (room != null && room.ID == roomId)
            {
                logger?.log("客户端与房间" + roomId + "断开连接");
                room = null;
            }
            else
                logger?.log("客户端移除房间" + roomId);
            lobby.removeRoom(roomId);
        }
        private RoomData onJoinRoomReq(RoomPlayerData player)
        {
            if (room == null)
                throw new InvalidOperationException("房间不存在");
            if (room.maxPlayerCount < 1 || room.playerDataList.Count < room.maxPlayerCount)
            {
                player.state = ERoomPlayerState.connecting;
                room.playerDataList.Add(player);
                return room;
            }
            else
                throw new InvalidOperationException("房间已满");
        }
        private RoomData onConfirmJoinReq(RoomPlayerData player)
        {
            if (room == null)
                throw new InvalidOperationException("房间不存在");
            player = room.playerDataList.Find(p => p.id == player.id);
            if (player != null)
                player.state = ERoomPlayerState.connected;
            else
                throw new NullReferenceException("房间中不存在玩家" + player.name);
            return room;
        }
        private void onConfirmJoinAck(RoomData joinedRoom)
        {
            if (room != null)
                throw new InvalidOperationException("已经在房间" + room.ID + "中");
            localPlayer = joinedRoom.getPlayer(curNetwork.getLocalPlayerData().id);
            room = joinedRoom;
            lobby.updateOrAddRoom(joinedRoom, out _);
        }
        private void onRoomAddPlayerNtf(string roomId, RoomPlayerData playerData)
        {
            Room room = lobby.getRoom(roomId);
            if (room == null)
            {
                logger.logWarn("房间" + roomId + "不存在");
                return;
            }
            if (room.data.playerDataList.Exists(p => p.id == playerData.id))
            {
                logger.logWarn("房间中已经存在玩家" + playerData.name);
                return;
            }
            room.data.playerDataList.Add(playerData);
        }
        private void onRoomRemovePlayerNtf(string roomId, int playerId)
        {
            if (!lobby.tryGetRoom(roomId, out var room))
            {
                logger?.logWarn("房间" + roomId + "不存在");
                return;
            }
            room.removePlayer(playerId);
            if (this.room != null && this.room.ID == roomId && localPlayer.id == playerId)
            {
                logger?.log("客户端被从房间" + roomId + "中移除");
                this.room = null;
            }
        }
        private void onRoomSetPropNtf(string roomId, string propName, object value)
        {
            if (!lobby.tryGetRoom(roomId, out Room room))
            {
                logger.logWarn("房间" + roomId + "不存在");
                return;
            }
            room.setProp(propName, value);
        }
        private void onRoomPlayerSetPropNtf(int playerId, string propName, object value)
        {
            room.setPlayerProp(playerId, propName, value);
        }
        ClientNetworking clientNetwork { get; }
        ILogger logger { get; }
        #endregion
    }
}