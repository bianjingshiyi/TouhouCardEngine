using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Net;

namespace NitoriNetwork.Common
{
    class LocalHostNetworking : HostNetworking
    {
        public RoomInfo room { get; set; } = null;

        public bool RoomValid => RoomIsValid(null);

        public void SetRoomInfo(RoomInfo room)
        {
            this.room = room;
        }

        public override RoomInfo GetRoom(NetPeer peer)
        {
            return room;
        }

        protected Dictionary<int, NetPeer> peerDict { get; } = new Dictionary<int, NetPeer>();

        protected override IEnumerable<NetPeer> GetRoomPeers(NetPeer peer)
        {
            return peerDict.Values;
        }

        public override void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            switch (messageType)
            {
                // 处理房间发现请求或主机信息更新请求
                case UnconnectedMessageType.Broadcast:
                case UnconnectedMessageType.BasicMessage:
                    if (RoomValid && reader.GetInt() == (int)PacketType.discoveryRequest)
                    {
                        logger?.Log($"主机房间收到了局域网发现请求或主机信息更新请求");
                        NetDataWriter writer = room.Write(PacketType.discoveryResponse, reader.GetUInt());
                        net.SendUnconnectedMessage(writer, remoteEndPoint);
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void onPeerAccept(NetPeer peer)
        {
            peerDict.Add(peer.Id, peer);
        }

        protected override NetPeer getPeerByPeerID(int id)
        {
            return peerDict[id];
        }

        protected override void onPeerDisconnect(NetPeer peer)
        {
            peerDict.Remove(peer.Id);
        }
    }
}
