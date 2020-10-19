using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
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