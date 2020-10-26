using NitoriNetwork.Common;
using System;
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
            networking = new ClientNetworking(logger);
        }
        public void createLocalRoom()
        {
            logger?.log("客户端创建本地房间");
            room = new LocalRoom();
            localPlayer = room.addLocalPlayer().Result;
        }
        public async Task createOnlineRoom(RoomPlayerData playerData)
        {
            logger?.log("客户端创建房间");
            await networking.invoke<RoomData>("createRoom", playerData);
        }
        public void Dispose()
        {
            if (networking != null)
                networking.Dispose();
        }
        public RoomPlayer localPlayer { get; private set; } = null;
        public ClientRoom room { get; private set; } = null;
        public ClientNetworking networking { get; }
        #endregion
        #region 私有成员
        ILogger logger { get; }
        #endregion
    }
    public class ClientRoom : Room
    {
        /// <summary>
        /// 添加并返回本地玩家。
        /// </summary>
        /// <returns></returns>
        public Task<RoomPlayer> addLocalPlayer()
        {
            RoomPlayer player = new LocalRoomPlayer(++lastPlayerId);
            addPlayer(new RoomPlayerData("本地玩家", RoomPlayerType.human), player);
            return Task.FromResult(player);
        }
    }
    public class OnlineRoom : Room
    {
        ClientNetworking _networking;
        public OnlineRoom(ClientNetworking networking, RoomData data) : base(data)
        {
            _networking = networking;
        }
    }
    public class LocalRoom : ClientRoom
    {
        public LocalRoom() : base()
        {
        }
    }
}