using NitoriNetwork.Common;
using System;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TouhouCardEngine
{
    public static class CommonHostMethod
    {
        /// <summary>
        /// request的转发器，将这个Request转发给指定的Peer
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="reader"></param>
        public static void SendRequestForwarder(this Networking nw, NetPeer[] peers, NetPacketReader reader)
        {
            try
            {
                int rid = reader.GetInt();
                int id = reader.GetInt();
                string typeName = reader.GetString();
                string json = reader.GetString();
                NetDataWriter writer = new NetDataWriter();
                writer.Put((int)PacketType.sendResponse);
                writer.Put(rid);
                writer.Put(id);
                writer.Put(typeName);
                writer.Put(json);
                nw.log?.logTrace($"{nw.name} 转发来自客户端 {id} 的数据：{typeName}({json})");
                foreach (var client in peers)
                {
                    client.Send(writer, DeliveryMethod.ReliableOrdered);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}