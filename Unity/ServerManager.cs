using System;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace TouhouCardEngine
{
    public class ServerManager : MonoBehaviour, IServerManager, INetEventListener
    {
        [SerializeField]
        int _port;
        public int port
        {
            get { return _port; }
        }
        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        NetManager net { get; set; }
        protected void Awake()
        {
            net = new NetManager(this)
            {
                AutoRecycle = true,
                DiscoveryEnabled = true
            };

        }
        protected void Start()
        {

            start(port);
        }
        protected void Update()
        {
            net.PollEvents();
        }
        public void start(int port)
        {
            _port = port;
            net.Start(_port);
        }
        public event Action<int, object> onReceive;

        public void broadcast(object obj)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            throw new NotImplementedException();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            throw new NotImplementedException();
        }

        public void send(int id, object obj)
        {
            throw new NotImplementedException();
        }
    }
}
