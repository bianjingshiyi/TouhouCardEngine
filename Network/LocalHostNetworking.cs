using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;
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
        public override Task<T> invoke<T>(string mehtod, params object[] args)
        {
            throw new NotImplementedException();
        }
        protected Dictionary<int, NetPeer> peerDict { get; } = new Dictionary<int, NetPeer>();

        protected override IEnumerable<NetPeer> GetRoomPeers(NetPeer peer)
        {
            return peerDict.Values;
        }

        protected override void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType, PacketType packetType)
        {
            switch (messageType)
            {
                // 处理房间发现请求或主机信息更新请求
                case UnconnectedMessageType.Broadcast:
                case UnconnectedMessageType.BasicMessage:
                    if (packetType == PacketType.discoveryRequest)
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
