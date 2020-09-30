using System;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Threading;
using System.Linq;
using NitoriNetwork.Common;
using UnityEditor;

namespace TouhouCardEngine
{
    public class ClientManager : MonoBehaviour, IClientManager
    {
        [SerializeField]
        int _port = 9050;
        public int port
        {
            get { return _port; }
        }
        [SerializeField]
        float _timeout = 30;
        public float timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                if (net != null)
                    net.DisconnectTimeout = (int)(value * 1000);
            }
        }
        [SerializeField]
        bool _autoStart = false;
        public bool autoStart
        {
            get { return _autoStart; }
            set { _autoStart = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Host和Client都有NetManager是因为在同一台电脑上如果要开Host和Client进行网络对战的话，就必须得开两个端口进行通信，出于这样的理由
        /// Host和Client都必须拥有一个NetManager实例并使用不同的端口。
        /// </remarks>
        NetManager net { get => client.net; set => client.net = value; }

        ClientNetworking client = new ClientNetworking();

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
                client.logger = new NetworkingLoggerAdapter(_logger);
            }
        }
        /// <summary>
        /// 客户端ID
        /// </summary>
        public int id => client.id;

        public int uid => _serverClient?.UID ?? -1;

        protected void Awake()
        {
            net = new NetManager(client)
            {
                AutoRecycle = true,
                UnconnectedMessagesEnabled = true,
                DisconnectTimeout = (int)(timeout * 1000),
                IPv6Enabled = true,
            };
            client.addSingleton(this);
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
        #region Network
        public void start()
        {
            if (!net.IsRunning)
            {
                net.Start();
                _port = net.LocalPort;
                logger?.log("客户端初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "客户端已经初始化，本地端口：" + net.LocalPort);
        }
        public void start(int port)
        {
            if (!net.IsRunning)
            {
                net.Start(port);
                _port = net.LocalPort;
                logger?.log("客户端初始化，本地端口：" + net.LocalPort);
            }
            else
                logger?.log("Warning", "客户端已经初始化，本地端口：" + net.LocalPort);
        }
        public Task<int> join(string ip, int port)
        {
            return client.join(ip, port);
        }
        public event Func<Task> onConnected
        {
            add
            {
                client.onConnected += value;
            }
            remove
            {
                client.onConnected -= value;
            }
        }
        public Task send(object obj)
        {
            return client.send(obj);
        }
        public async Task<T> send<T>(T obj)
        {
            return await client.send<T>(obj);
        }
        public Task<T> invokeHost<T>(RPCRequest request)
        {
            return client.invokeHost<T>(request);
        }
        /// <summary>
        /// 返回值是void的Request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<object> invokeHost(RPCRequest request)
        {
            return invokeHost<object>(request);
        }
        public Task<T> invokeAll<T>(IEnumerable<int> id, RPCRequest request)
        {
            throw new NotImplementedException();
        }
        public void addInvokeTarget(object obj)
        {
            client.addInvokeTarget(obj);
        }
        public event Func<int, object, Task> onReceive
        {
            add
            {
                client.onReceive += value;
            }
            remove
            {
                client.onReceive -= value;
            }
        }
        public void disconnect()
        {
            client.disconnect();
        }
        public event Action<DisconnectType> onDisconnect
        {
            add
            {
                client.onDisconnect += value;
            }
            remove
            {
                client.onDisconnect -= value;
            }
        }
        public void stop()
        {
            net.Stop();
        }
        #endregion
        #region Room
        /// <summary>
        /// 打开一个房间，如果是在服务器模式下会在收到服务器返回的房间信息后返回。
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public async Task<RoomInfo> openRoom(RoomInfo room, RoomPlayerInfo ownerInfo)
        {
            if (account != null)
            {
                var serverRoom = await _serverClient.CreateRoomAsync();
                room.id = new Guid(serverRoom.id);
                room.ip = serverRoom.ip;
                room.port = serverRoom.port;
                var newRoom = await joinRoom(room, ownerInfo);
                foreach (var pair in room.runtimeDic)
                {
                    newRoom.setProp(pair.Key, pair.Value);
                    await invokeHost(RPCHelper.RoomPropSet(pair.Key, pair.Value));
                }
                return newRoom;
            }
            throw new NotImplementedException();
        }
        /// <summary>
        /// 获取大厅中的房间列表，在服务器返回房间列表之后返回。
        /// </summary>
        /// <returns></returns>
        public async Task<RoomInfo[]> getRooms()
        {
            if (account != null)
            {
                var serverRooms = await _serverClient.GetRoomInfosAsync();
                return serverRooms.Select(sr =>
                {
                    return new RoomInfo(new Guid(sr.id), sr.ownerID, sr.players.Select(sp => new RoomPlayerInfo()
                    { 
                        name = _serverClient.GetUserInfo(sp).Name,
                        PlayerID = sp
                    }).ToArray())
                    {
                        ip = sr.ip,
                        port = sr.port
                    };
                }).ToArray();
            }
            throw new NotImplementedException();
        }
        /// <summary>
        /// 局域网发现是Host收到了给回应，你不可能知道Host什么时候回应，也不知道局域网里有多少个可能会回应的Host，所以这里不返回任何东西。
        /// </summary>
        /// <param name="port">搜索端口。默认9050</param>
        public void findRoom(int port = 9050)
        {
            client.findRoom(port);
        }

        public event Action<RoomInfo> onRoomFound
        {
            add => client.onRoomFound += value;
            remove => client.onRoomFound -= value;
        }

        /// <summary>
        /// 向目标房间请求新的房间信息，如果目标房间已经不存在了，那么会返回空，否则返回更新的房间信息。
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        public Task<RoomInfo> checkRoomInfo(RoomInfo roomInfo)
        {
            return client.checkRoomInfo(roomInfo);
        }
        public event Action onQuitRoom
        {
            add => client.onQuitRoom += value;
            remove => client.onQuitRoom -= value;
        }
        public event Action<RoomInfo> onJoinRoom
        {
            add => client.onJoinRoom += value;
            remove => client.onJoinRoom -= value;
        }
        /// <summary>
        /// 使用指定的玩家信息加入指定的房间，在收到主机或服务器返回的房间信息之后返回。
        /// </summary>
        /// <param name="room"></param>
        /// <param name="playerInfo"></param>
        /// <returns></returns>
        public async Task<RoomInfo> joinRoom(RoomInfo room, RoomPlayerInfo playerInfo)
        {
            if (account != null)
            {
                room = await client.joinRoom(room, playerInfo, _serverClient.UserSession);
                return room;
            }
            else
                return await client.joinRoom(room, playerInfo);
        }

        /// <summary>
        /// 请求更新
        /// </summary>
        /// <param name="playerInfo"></param>
        /// <returns></returns>
        public async Task updatePlayerInfo(RoomPlayerInfo playerInfo)
        {
            await client.updatePlayerInfo(playerInfo);
        }

        /// <summary>
        /// 当前所在房间信息，如果不在任何房间中则为空。
        /// </summary>
        public RoomInfo roomInfo => client.roomInfo;

        public event ClientNetworking.RoomInfoUpdateDelegate onRoomInfoUpdate
        {
            add => client.onRoomInfoUpdate += value;
            remove => client.onRoomInfoUpdate -= value;
        }

        public void quitRoom()
        {
            if (account != null)
            {
                client.quitRoom();
            }
            else
                client.quitRoom();
        }
        #endregion
        #region Server
        public Task<byte[]> getCaptchaImage()
        {
            return _serverClient.GetCaptchaImageAsync();
        }
        public Task<PublicBasicUserInfo> getUserInfo()
        {
            return _serverClient.GetUserInfoAsync();
        }
        /// <summary>
        /// 注册账号，在服务器回应之后返回。
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public Task register(AccountInfo account, string captcha)
        {
            return _serverClient.RegisterAsync(account.userName, account.mail, account.password, account.nickName, account.invite, captcha);
        }
        /// <summary>
        /// 登录指定服务器，在收到服务器的回应之后返回。
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task login(string account, string password, string captcha)
        {
            await _serverClient.LoginAsync(account, password, captcha);
            var userInfo = await _serverClient.GetUserInfoAsync();
            this.account = new AccountInfo(account, password, userInfo.Name, _serverClient.UID);
        }
        /// <summary>
        /// 从服务器登出，在服务器返回消息之后返回。
        /// </summary>
        /// <returns></returns>
        public async Task logout()
        {
            if (_serverClient == null)
                return;
            await _serverClient.LogoutAsync();
            account = null;
        }
        /// <summary>
        /// 加入服务器房间
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="session"></param>
        /// <param name="roomID"></param>
        /// <returns></returns>
        [Obsolete("使用joinRoom替代")]
        public Task<int> JoinServer(string ip, int port, string session, string roomID)
        {
            return client.join(ip, port, session, roomID);
        }
        /// <summary>
        /// 初始化服务器的客户端
        /// </summary>
        /// <param name="uri"></param>
        public void InitServerClient(string uri)
        {
            if (_serverClient == null)
            {
                _serverClient = new ServerClient(uri);
                account = null;
            }
        }
        public AccountInfo account
        {
            get { return _account; }
            private set { _account = value; }
        }

        [Header("Server")]
        [SerializeField]
        AccountInfo _account;
        ServerClient _serverClient;
        #endregion
    }
    [Serializable]
    public class AccountInfo
    {
        public string userName;
        public string password;
        public string mail;
        public string nickName;
        public int uid;
        public string invite;
        /// <summary>
        /// 登录账号构造器
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="nickName"></param>
        /// <param name="uid"></param>
        public AccountInfo(string userName, string password, string nickName, int uid) : this(userName, password, null, nickName, uid, null)
        {
        }
        /// <summary>
        /// 注册账号构造器
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="mail"></param>
        /// <param name="nickName"></param>
        public AccountInfo(string userName, string password, string mail, string nickName, string key) : this(userName, password, mail, nickName, 0, key)
        {
        }
        public AccountInfo(string userName, string password, string mail, string nickName, int uid, string key)
        {
            this.userName = userName;
            this.password = password;
            this.mail = mail;
            this.nickName = nickName;
            this.uid = uid;
            this.invite = key;
        }
    }
    public class RPCHelper
    {
        public static RPCRequest GameStart()
        {
            return new RPCRequest(typeof(void), "gameStart");
        }

        public static RPCRequest RoomPropSet(string name, object value)
        {
            return new RPCRequest(typeof(void), "setRoomProp", name, value is BsonDocument ? value : value.ToBsonDocument());
        }

        public static RPCRequest RemovePlayer(int playerID)
        {
            return new RPCRequest(typeof(void), "removePlayer", playerID);
        }
    }
}
