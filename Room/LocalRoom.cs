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
        public async Task createRoom(RoomPlayerData playerData)
        {
            logger?.log("客户端创建房间");
            await networking.invoke<RoomData>(nameof(createRoom), playerData);
        }
        public void Dispose()
        {

        }
        public ClientRoom room { get; private set; } = null;
        public ClientNetworking networking { get; }
        #endregion
        #region 私有成员
        ILogger logger { get; }
        #endregion
    }
    public class ClientRoom : TypedRoom
    {
        ClientNetworking _networking;
        public ClientRoom(ClientNetworking networking, RoomData data) : base(data)
        {
            _networking = networking;
        }
        public override Task<RoomPlayerData> addAIPlayer()
        {
            return _networking.invoke<RoomPlayerData>(nameof(Room.addAIPlayer));
        }
    }
    public class LocalRoom : TypedRoom
    {
        public LocalRoom() : base()
        {
        }
        public override RoomData data => throw new NotImplementedException();
        protected override void setData(RoomData data)
        {
            throw new NotImplementedException();
        }
    }
}