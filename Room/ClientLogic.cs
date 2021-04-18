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
        public ClientLogic(string name, ServerClient sClient = null, ILogger logger = null)
        {
            this.logger = logger;
            if (sClient != null)
                clientNetwork = new LobbyClientNetworking(sClient, logger: logger);
            LANNetwork = new LANNetworking(name, logger);
        }
        public void update()
        {
            curNetwork.update();
            //curNetwork.net.PollEvents();
        }
        public void Dispose()
        {
            if (clientNetwork != null)
                clientNetwork.Dispose();
            if (LANNetwork != null)
                LANNetwork.Dispose();
        }
        public async Task createLocalRoom()
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
                // 切换网络注销事件。
                curNetwork.OnRoomListUpdate -= roomListChangeEvtHandler;
                if (curNetwork == LANNetwork)
                {
                    LANNetwork.onJoinRoomReq -= onJoinRoomReq;
                    LANNetwork.onConfirmJoinReq -= onConfirmJoinReq;
                    LANNetwork.onConfirmJoinAck -= onConfirmJoinAck;
                }
            }
            if (!LANNetwork.isRunning)
                LANNetwork.start();
            curNetwork = LANNetwork;
            curNetwork.OnRoomListUpdate += roomListChangeEvtHandler;
            if (curNetwork == LANNetwork)
            {
                LANNetwork.onJoinRoomReq += onJoinRoomReq;
                LANNetwork.onConfirmJoinReq += onConfirmJoinReq;
                LANNetwork.onConfirmJoinAck += onConfirmJoinAck;
            }
        }

        public RoomPlayerData getLocalPlayerData()
        {
            return curNetwork.GetSelfPlayerData();
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
            localPlayer = curNetwork.GetSelfPlayerData();
            room = await curNetwork.CreateRoom();

            // lobby.addRoom(room); // 不要在自己的房间列表里面显示自己的房间。
            //this.room.maxPlayerCount = MAX_PLAYER_COUNT;
        }
        //public void refreshRooms()
        //{
        //    logger?.log("客户端刷新房间列表");
        //    curNetwork.refreshRooms();
        //}
        [Obsolete("无用方法，等会删")]
        public Task<RoomData[]> getRooms()
        {
            logger?.log("客户端请求房间列表");
            curNetwork.RefreshRoomList();
            return null; // todo: 这个房间是异步的……
        }
        public async Task<bool> joinRoom(string roomId)
        {
            logger?.log("客户端请求加入房间" + roomId);
            room = await curNetwork.JoinRoom(roomId);
            return room != null;
        }
        public Task addAIPlayer()
        {
            logger?.log("主机添加AI玩家");
            RoomPlayerData playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai);

            var host = curNetwork as INetworkingV3LANHost;
            if (host != null)
            {
                return host.AddPlayer(playerData);
            }
            else
            {
                // 本地玩家。
                room.playerDataList.Add(playerData);
            }

            return Task.CompletedTask;
        }
        public Task setRoomProp(string propName, object value)
        {
            logger?.log("主机更改房间属性" + propName + "为" + value);
            room.setProp(propName, value);
            if (curNetwork != null)
                return curNetwork.SetRoomProp(propName, value);
            return Task.CompletedTask;
        }
        public async Task setPlayerProp(string propName, object value)
        {
            logger?.log("玩家更改房间属性" + propName + "为" + value);
            room.setPlayerProp(localPlayer.id, propName, value);
            if (curNetwork != null)
                await curNetwork.SetPlayerProp(propName, value);
        }
        public Task quitRoom()
        {
            logger?.log("玩家退出房间" + room.ID);
            room = null;
            if (curNetwork != null)
                curNetwork.QuitRoom();
            return Task.CompletedTask;
        }
        [Obsolete("Use onRoomListChangeInstead", true)]
        public event Action<RoomData> onNewRoom;
        [Obsolete("Use onRoomListChangeInstead", true)]
        public event Action<RoomData> onUpdateRoom;
        public event Action<LobbyRoomDataList> onRoomListChange;
        public RoomPlayerData localPlayer { get; private set; } = null;
        public RoomData room { get; private set; } = null;

        public LobbyRoomDataList roomList { get; protected set; } = new LobbyRoomDataList();

        /// <summary>
        /// 网络端口
        /// </summary>
        public int port => curNetwork != null ? curNetwork.Port : -1;
        public LANNetworking LANNetwork { get; }
        LobbyClientNetworking clientNetwork { get; }

        public CommonClientNetwokingV3 curNetwork { get; set; } = null;
        #endregion
        #region 私有成员
        void roomListChangeEvtHandler(LobbyRoomDataList list)
        {
            roomList = list;
            onRoomListChange?.Invoke(list);
        }
        /// <summary>
        /// 处理玩家连接请求，判断是否可以连接
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 将玩家加入房间，然后返回一个房间信息
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 房间加入完成
        /// </summary>
        /// <param name="joinedRoom"></param>
        private void onConfirmJoinAck(RoomData joinedRoom)
        {
            if (room != null)
                throw new InvalidOperationException("已经在房间" + room.ID + "中");
            localPlayer = joinedRoom.getPlayer(curNetwork.GetSelfPlayerData().id);
            room = joinedRoom;
        }

        ILogger logger { get; }
        #endregion
    }
}