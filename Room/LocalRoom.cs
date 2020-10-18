using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public class ClientRoom : Room
    {
        ClientNetworking _networking;
        public ClientRoom(ClientNetworking networking, RoomData data) : base(data)
        {
            _networking = networking;
        }
        public override Task<AIRoomPlayer> addAIPlayer()
        {
            throw new NotImplementedException();
        }
    }
    public class LocalRoom : Room
    {
        public LocalRoom() : base()
        {
        }

        public override Task<AIRoomPlayer> addAIPlayer()
        {
            AIRoomPlayer player = new AIRoomPlayer(++lastPlayerId);
            addPlayer(player);
            return Task.FromResult(player);
        }
    }
}