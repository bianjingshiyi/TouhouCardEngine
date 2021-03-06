﻿using NitoriNetwork.Common;
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

        public int[] LANPorts { get; } = { 32900, 32901 };

        #region 公共成员
        public ClientLogic(string name, int[] ports = null, ServerClient sClient = null, ILogger logger = null)
        {
            this.logger = logger;
            if (sClient != null)
                LobbyNetwork = new LobbyClientNetworking(sClient, logger: logger);
            LANNetwork = new LANNetworking(name, logger);

            if (ports != null)
                LANPorts = ports;
            
            LANNetwork.broadcastPorts = LANPorts;
        }
        public void update()
        {
            if (curNetwork != null)
            {
                curNetwork.update();
            }
            //curNetwork.net.PollEvents();
        }
        public void Dispose()
        {
            if (clientNetwork != null)
                clientNetwork.Dispose();
            if (LANNetwork != null)
                LANNetwork.Dispose();
        }
        bool isLocalRoom = false;
        public async Task createLocalRoom()
        {
            logger?.log("客户端创建本地房间");
            room = new RoomData(string.Empty);
            localPlayer = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "本地玩家", RoomPlayerType.human);
            room.playerDataList.Add(localPlayer);
            room.ownerId = localPlayer.id;

            isLocalRoom = true;
        }
        public void switchNetToLAN()
        {
            SwitchMode(true);
        }

        public void switchNetToLobby()
        {
            SwitchMode(false);
        }

        public void SwitchMode(bool isLAN)
        {
            if (curNetwork != null)
            {
                // 切换网络注销事件。
                curNetwork.OnRoomListUpdate -= roomListChangeEvtHandler;
                curNetwork.OnGameStart -= roomGameStartEvtHandler;
                curNetwork.onReceive -= roomReceiveEvtHandler;
                curNetwork.OnRoomDataChange -= roomDataChangeEvtHandler;
                curNetwork.OnRoomPlayerDataChanged -= roomPlayerDataChangeEvtHandler;

                if (curNetwork == LANNetwork)
                {
                    LANNetwork.onJoinRoomReq -= onJoinRoomReq;
                    LANNetwork.onConfirmJoinReq -= onConfirmJoinReq;
                    LANNetwork.onConfirmJoinAck -= onConfirmJoinAck;
                }
            }

            if (isLAN)
            {
                logger.log("切换到局域网网络");
                curNetwork = LANNetwork;
            } 
            else
            {
                logger.log("切换到服务器网络");
                curNetwork = LobbyNetwork;
            }

            if (!curNetwork.isRunning)
            {
                if (curNetwork == LANNetwork)
                {
                    // 以指定的端口启动
                    for (int i = 0; i < LANPorts.Length; i++)
                    {
                        if (curNetwork.start(LANPorts[i]))
                            break;
                    }
                    if (!curNetwork.isRunning)
                    {
                        curNetwork.start();
                    }
                }
                else
                {
                    curNetwork.start();
                }
            }

            curNetwork.OnRoomListUpdate += roomListChangeEvtHandler;
            curNetwork.OnGameStart += roomGameStartEvtHandler;
            curNetwork.onReceive += roomReceiveEvtHandler;
            curNetwork.OnRoomDataChange += roomDataChangeEvtHandler;
            curNetwork.OnRoomPlayerDataChanged += roomPlayerDataChangeEvtHandler;

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

        public event Action<RoomPlayerData[]> OnPlayerDataChange;
        private void roomPlayerDataChangeEvtHandler(RoomPlayerData[] obj)
        {
            OnPlayerDataChange?.Invoke(obj);
        }

        public event Action<RoomData> OnRoomDataChange;
        private void roomDataChangeEvtHandler(RoomData obj)
        {
            OnRoomDataChange?.Invoke(obj);
        }

        public event ResponseHandler OnReceiveData;
        private Task roomReceiveEvtHandler(int clientID, object obj)
        {
            return OnReceiveData?.Invoke(clientID, obj);
        }

        /// <summary>
        /// 根据所处的模式，创建局域网房间或服务器房间
        /// </summary>
        /// <param name="port">发送或广播创建房间信息的端口</param>
        /// <returns></returns>
        /// <remarks>port主要是在局域网测试下有用</remarks>
        public async Task createOnlineRoom()
        {
            logger?.log("客户端创建在线房间");
            localPlayer = curNetwork.GetSelfPlayerData();
            room = await curNetwork.CreateRoom();
            room.maxPlayerCount = MAX_PLAYER_COUNT;
            isLocalRoom = false;

            // lobby.addRoom(room); // 不要在自己的房间列表里面显示自己的房间。
            //this.room.maxPlayerCount = MAX_PLAYER_COUNT;
        }

        /// <summary>
        /// 请求房间列表
        /// </summary>
        public void refreshRoomList()
        {
            logger?.log("客户端请求房间列表");
            curNetwork?.RefreshRoomList();
        }

        public async Task<bool> joinRoom(string roomId)
        {
            logger?.log("客户端请求加入房间" + roomId);
            room = await curNetwork.JoinRoom(roomId);
            return room != null;
        }

        public async Task<bool> joinRoom(string addr, int port)
        {
            if (curNetwork != LANNetwork) return false; 
            logger?.log("客户端请求加入房间" + addr + ":" + port);
            room = await LANNetwork.JoinRoom(addr, port);
            return room != null;
        }

        public Task addAIPlayer()
        {
            logger?.log("主机添加AI玩家");
            RoomPlayerData playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai);
            var host = curNetwork as INetworkingV3LANHost;
            if (!isLocalRoom && host != null)
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
            if (curNetwork != null && !isLocalRoom)
                curNetwork.QuitRoom();
            return Task.CompletedTask;
        }
        public event Action<LobbyRoomDataList> onRoomListChange;
        public event Action onGameStart;
        public RoomPlayerData localPlayer { get; private set; } = null;
        public RoomData room { get; private set; } = null;

        public LobbyRoomDataList roomList { get; protected set; } = new LobbyRoomDataList();

        /// <summary>
        /// 网络端口
        /// </summary>
        public int port => curNetwork != null ? curNetwork.Port : -1;
        public LANNetworking LANNetwork { get; }
        public LobbyClientNetworking LobbyNetwork { get; }

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

        private void roomGameStartEvtHandler()
        {
            onGameStart?.Invoke();
        }
        ILogger logger { get; }
        #endregion
    }
}