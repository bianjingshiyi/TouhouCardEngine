using System;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace TouhouCardEngine
{
    public class HostManager : MonoBehaviour, IHostManager, INetEventListener
    {
        [SerializeField]
        int _port = 9050;
        public int port
        {
            get { return _port; }
        }
        public string address
        {
            get { return ip + ":" + port; }
        }
        public string ip => Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        NetManager net { get; set; }
        public bool isRunning
        {
            get { return net != null ? net.IsRunning : false; }
        }
        Dictionary<int, NetPeer> clientDic { get; } = new Dictionary<int, NetPeer>();
        public Interfaces.ILogger logger { get; set; } = null;
        protected void Awake()
        {
            net = new NetManager(this)
            {
                AutoRecycle = true,
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true
            };

        }
        protected void Start()
        {
            if (autoStart)
            {
                if (port > 0)
                    start(port);
                else
                    start();
            }
        }
        protected void Update()
        {
            net.PollEvents();
        }
        public void start()
        {
            if (!net.IsRunning)
            {
                net.Start();
                _port = net.LocalPort;
                logger?.log("主机初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "主机已经初始化，本地端口：" + net.LocalPort);
        }
        public void start(int port)
        {
            if (!net.IsRunning)
            {
                net.Start(port);
                _port = net.LocalPort;
                logger?.log("主机初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "主机已经初始化，本地端口：" + net.LocalPort);
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            NetPeer peer = request.Accept();
            clientDic.Add(peer.Id, peer);
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.connectResponse);
            writer.Put(peer.Id);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            logger?.log("主机同意" + request.RemoteEndPoint + "的连接请求");
        }
        public void OnPeerConnected(NetPeer peer)
        {
            logger?.log("主机被客户端" + peer.Id + "连接");
            onClientConnected?.Invoke(peer.Id);
        }
        public event Action<int> onClientConnected;
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            PacketType type = (PacketType)reader.GetInt();
            switch (type)
            {
                case PacketType.sendRequest:
                    int id = reader.GetInt();
                    string typeName = reader.GetString();
                    string json = reader.GetString();
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put((int)PacketType.sendResponse);
                    writer.Put(id);
                    writer.Put(typeName);
                    writer.Put(json);
                    logger?.log("主机收到来自客户端" + id + "的数据：（" + typeName + "）" + json);
                    foreach (var client in clientDic.Values)
                    {
                        client.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                    break;
                case PacketType.joinRequest:
                    if (currentRoom == null)
                        break;
                    id = reader.GetInt();
                    typeName = reader.GetString();
                    json = reader.GetString();
                    if (typeName != typeof(RoomPlayerInfo).FullName)
                    {
                        logger?.log($"主机房间信息类型错误，收到了 {typeName}");
                        break;
                    }
                    var info = BsonSerializer.Deserialize<RoomPlayerInfo>(json);
                    currentRoom.playerList.Add(info);
                    onPlayerJoin?.Invoke(info);
                    logger?.log($"主机房间收到了客户端 {info.name} 的加入请求，当前人数 {currentRoom.playerList.Count}");

                    writer = RoomInfoUpdateWriter();

                    foreach (var client in clientDic.Values)
                    {
                        if (client.Id == peer.Id)
                        {
                            // 接受加入，返回房间信息
                            NetDataWriter writer2 = RoomInfoResponseWriter();
                            client.Send(writer2, DeliveryMethod.ReliableOrdered);
                        }
                        else
                        {
                            // 其他的更新房间信息
                            client.Send(writer, DeliveryMethod.ReliableOrdered);
                        }
                    }
                    break;
                default:
                    logger?.log("Warning", "服务端未处理的数据包类型：" + type);
                    break;
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            logger?.log("客主机与客户端" + peer.Id + "断开连接，原因：" + disconnectInfo.Reason + "，SocketErrorCode：" + disconnectInfo.SocketErrorCode);
            // 处理房间问题
            var infos = currentRoom?.playerList.Where(c => c.id == peer.Id);
            if (infos != null && infos.Count() > 0)
            {
                var info = infos.First();
                currentRoom.playerList.Remove(info);
                onPlayerQuit?.Invoke(info);

                var writer = RoomInfoUpdateWriter();
                foreach (var client in clientDic.Values)
                {
                    if (client.Id != peer.Id)
                    {
                        // 其他的更新房间信息
                        client.Send(writer, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            logger?.log("Error", "主机与" + endPoint + "发生网络异常：" + socketError);
        }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            switch (messageType)
            {
                case UnconnectedMessageType.Broadcast:
                case UnconnectedMessageType.BasicMessage:
                    if (currentRoom != null && reader.GetInt() == (int)PacketType.discoveryRequest)
                    {
                        logger?.log($"主机房间收到了局域网发现请求或主机信息更新请求");

                        NetDataWriter writer = RoomInfoDiscoveryWriter(reader.GetUInt());
                        net.SendUnconnectedMessage(writer, remoteEndPoint);
                    }
                    break;
                default:
                    break;
            }
        }

        public void stop()
        {
            net.Stop();
        }
        #region Room

        RoomInfo currentRoom = null;
        public RoomInfo openRoom(RoomInfo roomInfo)
        {
            currentRoom = roomInfo;
            if (!net.IsRunning)
            {
                start(roomInfo.port);
            }
            roomInfo.ip = ip;
            return roomInfo;
        }
        private NetDataWriter RoomInfoUpdateWriter()
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.roomInfoUpdate);
            RoomInfoWriter(writer);
            return writer;
        }

        private NetDataWriter RoomInfoDiscoveryWriter(uint requestID)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((int)PacketType.discoveryResponse);
            writer.Put(requestID);
            RoomInfoWriter(writer);
            return writer;
        }

        private void RoomInfoWriter(NetDataWriter writer)
        {
            writer.Put(currentRoom.GetType().FullName);
            writer.Put(currentRoom.ToJson());
        }
        private NetDataWriter RoomInfoResponseWriter()
        {
            var writer = new NetDataWriter();
            writer.Put((int)PacketType.joinResponse);
            RoomInfoWriter(writer);
            return writer;
        }


        public event Action<RoomPlayerInfo> onPlayerJoin;
        /// <summary>
        /// 当前房间信息，在没有打开房间的情况下为空。
        /// </summary>
        public RoomInfo roomInfo => currentRoom;

        /// <summary>
        /// 更新房间信息，会在Host保存最新的房间信息和将更新的房间信息发送给所有的Client
        /// </summary>
        /// <param name="roomInfo"></param>
        public void updateRoomInfo(RoomInfo roomInfo)
        {
            currentRoom = roomInfo;
            var writer = RoomInfoUpdateWriter();

            foreach (var client in clientDic.Values)
            {
                client.Send(writer, DeliveryMethod.ReliableOrdered);
            }
        }
        public event Action<RoomPlayerInfo> onPlayerQuit;
        public void closeRoom()
        {
            net.DisconnectAll();
            currentRoom = null;
        }
        #endregion
    }
    [Serializable]
    public class RoomInfo
    {
        public string ip;
        public int port;
        public List<RoomPlayerInfo> playerList = new List<RoomPlayerInfo>();
    }
    [Serializable]
    public class RoomPlayerInfo
    {
        public int id = 0;
        public string name = null;
    }
    enum PacketType
    {
        connectResponse,
        sendRequest,
        sendResponse,
        discoveryRequest,
        discoveryResponse,
        joinRequest,
        joinResponse,
        roomInfoUpdate,
    }
}
