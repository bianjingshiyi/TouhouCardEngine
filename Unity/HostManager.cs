using LiteNetLib;
using NitoriNetwork.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
using UnityEngine;

namespace TouhouCardEngine
{
    public class HostManager : MonoBehaviour, IHostManager
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

        public string[] ips => Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).Select(i => i.ToString()).ToArray();

        public string[] addresses => ips.Select(i => i + ":" + port).ToArray();

        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }

        LocalHostNetworking host = new LocalHostNetworking();

        NetManager net { get => host.net; set => host.net = value; }
        public bool isRunning
        {
            get { return net != null ? net.IsRunning : false; }
        }

        Interfaces.ILogger _logger = null;

        public Interfaces.ILogger logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
                host.logger = new NetworkingLoggerAdapter(value);
            }
        }

        [SerializeField]
        float _timeout = 3;
        /// <summary>
        /// 超时时间，以毫秒计
        /// </summary>
        public float timeout
        {
            get { return host.timeout; }
            set { host.timeout = value; _timeout = value; }
        }
        protected void Awake()
        {
            net = new NetManager(host)
            {
                AutoRecycle = true,
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true,
                IPv6Enabled = true
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
            host.timeout = _timeout;
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

        public void stop()
        {
            net.Stop();
        }

        public RoomInfo room => host.room;

        public bool RoomIsValid => host.RoomValid;

        public RoomInfo openRoom(RoomInfo info)
        {
            host.SetRoomInfo(info);

            if (!net.IsRunning)
            {
                start(info.port);
            }
            info.ip = ip;
            info.port = port;
            return info;
        }

        public async Task<Dictionary<int, T>> invokeAll<T>(int[] IdArray, RPCRequest request)
        {
            return await host.invokeAll<T>(IdArray, request);
        }
        public Task<T> invoke<T>(int id, RPCRequest request)
        {
            return host.invoke<T>(id, request);
        }

        public event Action<RoomPlayerInfo> onPlayerJoin 
        {
            add
            {
                host.onPlayerJoin += value;
            }
            remove
            {
                host.onPlayerQuit -= value;
            }
        }

        public event Action<RoomPlayerInfo> onPlayerQuit
        {
            add
            {
                host.onPlayerQuit += value;
            }
            remove
            {
                host.onPlayerQuit -= value;
            }
        }

        public void closeRoom()
        {
            net.DisconnectAll();
            host.room = null;
        }
    }

    public class NetworkingLoggerAdapter : INetworkingLogger
    {
        Interfaces.ILogger logger;

        public const string channel = "Network";

        public NetworkingLoggerAdapter(Interfaces.ILogger logger)
        {
            this.logger = logger;
        }
        public void Error(string log)
        {
            logger.logError(channel, log);
        }

        public void Log(string log)
        {
            logger.log(channel, log);
        }

        public void Warning(string log)
        {
            logger.logWarn(channel, log);
        }
    }
}
