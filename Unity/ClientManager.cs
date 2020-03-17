using System;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace TouhouCardEngine
{
    public class ClientManager : MonoBehaviour, IClientManager, INetEventListener
    {
        [SerializeField]
        bool _autoStart = true;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        NetManager net { get; set; } = null;
        NetPeer server { get; set; } = null;
        public int id => throw new NotImplementedException();
        protected void Awake()
        {
            net = new NetManager(this)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true
            };
        }
        protected void Start()
        {
            if (autoStart)
                start();
        }
        protected void Update()
        {
            net.PollEvents();
        }
        public void start()
        {
            net.Start();
        }
        public void connect(string ip, int port)
        {
            NetDataWriter writer = new NetDataWriter();
            server = net.Connect(new IPEndPoint(IPAddress.Parse(ip), port), writer);
        }
        public void send(object obj)
        {
            throw new NotImplementedException();
        }
        public event Action onConnected;
        public event Action<int, object> onReceive;
        public event Action onDisconnect;

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

        public void disconnect()
        {
            throw new NotImplementedException();
        }
    }
}
