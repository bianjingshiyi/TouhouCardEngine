using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace TouhouCardEngine
{
    public class LobbyClientNetworking : CommonClientNetwokingV3, IRoomRPCMethodClient
    {
        /// <summary>
        /// 服务器通信客户端
        /// </summary>
        ServerClient serverClient { get; }

        /// <summary>
        /// 本地玩家
        /// </summary>
        RoomPlayerData localPlayer { get; set; } = null;

        /// <summary>
        /// 主机对端
        /// </summary>
        public NetPeer hostPeer { get; set; } = null;

        public LobbyClientNetworking(ServerClient servClient, Shared.ILogger logger) : base("lobbyClient", logger)
        {
            serverClient = servClient;
            initRpcMethods();
        }

        #region 外部方法实现

        /// <summary>
        /// 获取当前用户的数据
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData GetSelfPlayerData()
        {
            // 仅在更换了用户后更新这个PlayerData
            var info = serverClient.GetUserInfoCached();
            if (localPlayer?.id != info.UID)
                localPlayer = new RoomPlayerData(info.UID, info.Name, RoomPlayerType.human);

            return localPlayer;
        }

        /// <summary>
        /// 创建一个空房间，房主为自己
        /// </summary>
        /// <returns></returns>
        public async override Task<RoomData> CreateRoom()
        {
            // todo: 这里需要与其他地方配合，得到真正的房间信息。
            // 在没有连接到房间之前，房间内玩家是0，所以不设置房间。
            var roomInfo = await serverClient.CreateRoomAsync();
            var roomData = new RoomData(roomInfo.id);
            roomData.ownerId = localPlayer.id;
            return roomData;
        }

        /// <summary>
        /// 缓存的服务器上房间列表
        /// </summary>
        Dictionary<string, BriefRoomInfo> cachedRoomsInfo = new Dictionary<string, BriefRoomInfo>();

        /// <summary>
        /// 获取当前服务器的房间信息
        /// </summary>
        /// <returns></returns>
        public async override Task<RoomData[]> GetRooms()
        {
            var roomInfos = await serverClient.GetRoomInfosAsync();
            List<RoomData> rooms = new List<RoomData>();

            cachedRoomsInfo.Clear();
            foreach (var item in roomInfos)
            {
                cachedRoomsInfo.Add(item.id, item);

                var room = new RoomData(item.id);
                room.ownerId = item.ownerID;
                foreach (var p in item.players)
                {
                    var userInfo = await serverClient.GetUserInfoAsync(p, false);
                    room.playerDataList.Add(new RoomPlayerData(p, userInfo.Name, RoomPlayerType.human));
                }
                foreach (var propKV in item.properties)
                {
                    // todo: object convert
                    room.propDict[propKV.Key] = propKV.Value;
                }
            }

            return rooms.ToArray();
        }

        public override Task<RoomData> JoinRoom(string roomId)
        {
            if (!cachedRoomsInfo.ContainsKey(roomId))
                throw new ArgumentOutOfRangeException("roomID", "指定ID的房间不存在");

            var roomInfo = cachedRoomsInfo[roomId];
            var writer = new NetDataWriter();
            // todo: 规定加入的数据格式。
            writer.Put(roomId);
            writer.Put(localPlayer.id);

            hostPeer = net.Connect(roomInfo.ip, roomInfo.port, writer);
            JoinRoomOperation op = new JoinRoomOperation();
            startOperation(op, () =>
            {
                log?.logWarn($"连接到 {roomInfo} 响应超时。");
            });
            return op.task;
        }

        /// <summary>
        /// 销毁房间。
        /// 这个方法不要使用，请使用QuitRoom退出当前房间，服务器会在没有更多玩家的情况下销毁房间。
        /// </summary>
        /// <returns></returns>
        public override Task DestroyRoom()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 修改房间信息
        /// 暂时不知道能修改什么房间信息，先不实现
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        public override Task AlterRoomInfo(RoomInfo changedInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 退出房间
        /// </summary>
        /// <returns></returns>
        public override void QuitRoom()
        {
            // 直接断开就好了吧
            net.DisconnectPeer(hostPeer);
        }

        /// <summary>
        /// 获取所有用户信息
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData[] GetPlayerData()
        {
            // todo: API 接口设计有点问题
            throw new NotImplementedException();
        }

        public override object GetRoomProp(string name)
        {
            // todo: API 接口设计有点问题
            throw new NotImplementedException();
        }

        public override Task SetRoomProp(string key, object value)
        {
            return invoke<object>(nameof(IRoomRPCMethodHost.setRoomProp), key, value);
        }

        public override Task SetPlayerProp(string name, object val)
        {
            return invoke<object>(nameof(IRoomRPCMethodHost.setPlayerProp), name, val);
        }

        public override Task GameStart()
        {
            return invoke<object>(nameof(IRoomRPCMethodHost.gameStart));
        }
        #endregion

        #region RPC接口
        private void initRpcMethods()
        {
            addRPCMethod(this, typeof(IRoomRPCMethodClient));
        }

        /// <summary>
        /// 缓存的房间数据
        /// </summary>
        RoomData cachedRoomData;

        void IRoomRPCMethodClient.updateRoomData(RoomData data)
        {
            cachedRoomData = data;
            // todo: invoke change
        }

        void IRoomRPCMethodClient.updatePlayerData(RoomPlayerData data)
        {
            // todo: invoke change
        }
        #endregion
        #region 交互逻辑
        async Task requestJoinRoom()
        {
            var op = getOperation(typeof(JoinRoomOperation));
            if (op == null)
            {
                log?.logWarn($"{name} 当前没有加入房间的操作，但是却想要发出加入房间的请求。");
                return;
            }

            var roomInfo = await invoke<RoomData>(nameof(IRoomRPCMethodHost.requestJoinRoom), GetSelfPlayerData());
            completeOperation(op, roomInfo);
        }
        #endregion
        #region 底层实现
        public override Task<T> invoke<T>(string method, params object[] args)
        {
            return invoke<T>(hostPeer, method, args);
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
            log?.logWarn($"另一个客户端({request.RemoteEndPoint})尝试连接到本机({name})，由于当前网络是客户端网络故拒绝。");
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            if (peer == hostPeer)
            {
                _ = requestJoinRoom();
            }
        }

        SlidingAverage latencyAvg = new SlidingAverage(10);
        protected override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            if (peer == hostPeer)
            {
                latencyAvg.Push(latency);
            }
        }

        public override int GetLatency()
        {
            return (int)latencyAvg.GetAvg();
        }
        #endregion
    }

    public abstract class CommonClientNetwokingV3 : Networking, INetworkingV3Client
    {
        public CommonClientNetwokingV3(string name, Shared.ILogger logger) : base(name, logger)
        {
        }

        #region 待实现的接口
        public abstract Task AlterRoomInfo(RoomInfo changedInfo);
        public abstract Task<RoomData> CreateRoom();
        public abstract Task DestroyRoom();
        public abstract Task GameStart();
        public abstract object GetRoomProp(string name);
        public abstract Task<RoomData[]> GetRooms();
        public abstract RoomPlayerData GetSelfPlayerData();
        public abstract Task<RoomData> JoinRoom(string roomID);
        public abstract RoomPlayerData[] GetPlayerData();
        public abstract void QuitRoom();
        public abstract Task SetPlayerProp(string name, object val);
        public abstract Task SetRoomProp(string name, object val);
        public abstract int GetLatency();
        #endregion

        #region 网络底层处理
        protected class JoinRoomOperation : Operation<RoomData>
        {
            public JoinRoomOperation() : base(nameof(CommonClientNetwokingV3.JoinRoom))
            {
            }
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            log?.log($"网络({name})与端点({peer.EndPoint})断开连接，原因：{disconnectInfo.Reason}，SocketErrorCode：{disconnectInfo.SocketErrorCode}");

            switch (disconnectInfo.Reason)
            {
                case DisconnectReason.ConnectionFailed: // 连接失败
                case DisconnectReason.HostUnreachable: // 无法到达主机
                case DisconnectReason.NetworkUnreachable: // 无法到达网络
                case DisconnectReason.UnknownHost: // 未知主机
                case DisconnectReason.InvalidProtocol: // 错误协议
                case DisconnectReason.ConnectionRejected: // 拒绝连接
                    // 基本上只有正在加入的时候才会触发这些Error。将这些错误转换为Exception交给上层处理，用于显示无法加入的信息。
                    var exception = new NtrNetworkException(disconnectInfo.Reason);
                    var op = getOperation(typeof(JoinRoomOperation));
                    if (op != null)
                        op.setException(exception);
                    break;
                case DisconnectReason.RemoteConnectionClose: // 远程关闭
                    break;
                case DisconnectReason.Reconnect:
                    break;
                case DisconnectReason.Timeout:
                    break;
                case DisconnectReason.DisconnectPeerCalled:
                    break;
                default:
                    break;
            }
        }

        public void pollEvents()
        {
            net.PollEvents();
        }

        protected override Type getType(string typeName)
        {
            return TypeHelper.getType(typeName);
        }

        protected override void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            log?.logError($"{name} 在与 {endPoint} 通信时发生异常，{socketError}");
        }
        #endregion
    }

    interface INetworkingV3Client
    {
        #region Player
        /// <summary>
        /// 获取当前玩家（自己）的玩家信息
        /// </summary>
        /// <returns></returns>
        RoomPlayerData GetSelfPlayerData();
        #endregion

        #region Lobby
        /// <summary>
        /// 以当前玩家为房主创建一个房间
        /// </summary>
        /// <returns></returns>
        Task<RoomData> CreateRoom();

        /// <summary>
        /// 关闭当前已经创建的房间（部分情况下用不上）
        /// </summary>
        /// <returns></returns>
        Task DestroyRoom();

        /// <summary>
        /// 获取当前课加入的房间信息
        /// </summary>
        /// <remarks>
        /// 对开发者的提示：
        /// 请在实现时缓存详细的IP和端口等信息，方便后面JoinRoom时连接。
        /// </remarks>
        /// <returns></returns>
        Task<RoomData[]> GetRooms();

        /// <summary>
        /// 修改当前房间的信息
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        Task AlterRoomInfo(RoomInfo changedInfo);
        #endregion

        #region Room
        /// <summary>
        /// 使用当前用户加入一个房间
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        Task<RoomData> JoinRoom(string roomID);

        /// <summary>
        /// 退出当前加入的房间
        /// </summary>
        /// <returns></returns>
        void QuitRoom();

        /// <summary>
        /// 获取房间内所有玩家的数据
        /// //? 为啥会有这个API？
        /// </summary>
        /// <returns></returns>
        RoomPlayerData[] GetPlayerData();

        /// <summary>
        /// 修改玩家的属性
        /// </summary>
        /// <returns></returns>
        Task SetPlayerProp(string name, object val);

        /// <summary>
        /// 获取房间属性
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object GetRoomProp(string name);

        /// <summary>
        /// 修改房间的属性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        Task SetRoomProp(string name, object val);

        /// <summary>
        /// 开始游戏！
        /// </summary>
        /// <returns></returns>
        Task GameStart();

        /// <summary>
        /// 获取当前网络的延迟
        /// </summary>
        /// <returns></returns>
        int GetLatency();
        #endregion

        #region Game
        // todo
        #endregion
    }

    /// <summary>
    /// 滑动窗口平均
    /// </summary>
    class SlidingAverage
    {
        public int Size { get; }
        int[] buffer;

        int index = 0, count = 0;
        long sum = 0;

        /// <summary>
        /// 窗口大小
        /// </summary>
        /// <param name="size"></param>
        public SlidingAverage(int size)
        {
            Size = size;
            buffer = new int[Size];
        }

        /// <summary>
        /// 插入一个值
        /// </summary>
        /// <param name="num"></param>
        public void Push(int num)
        {
            if (count == Size)
            {
                sum -= buffer[index];
                sum += num;
            }

            buffer[index++] = num;
            index = index % Size;

            if (count < Size) count++;
        }

        /// <summary>
        /// 获取平均值
        /// </summary>
        /// <returns></returns>
        public float GetAvg()
        {
            return (float)sum / count; 
        }
    }

    [Serializable]
    public class NtrNetworkException : Exception
    {
        public NtrNetworkException() { }
        public NtrNetworkException(DisconnectReason reason) : base(reason.ToString()) { }
        protected NtrNetworkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}