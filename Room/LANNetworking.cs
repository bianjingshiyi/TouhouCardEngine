using NitoriNetwork.Common;
using System;
using System.Threading.Tasks;
using TouhouCardEngine.Shared;

namespace TouhouCardEngine
{
    /// <summary>
    /// 网络的局域网实现
    /// </summary>
    public class LANNetworking : ClientNetworking
    {
        #region 公共成员
        /// <summary>
        /// 局域网络构造器，包括RPC方法注册。
        /// </summary>
        /// <param name="logger"></param>
        public LANNetworking(ILogger logger = null) : base("LAN", logger)
        {
            addRPCMethod(this, GetType().GetMethod(nameof(ackCreateRoom)));
            addRPCMethod(this, GetType().GetMethod(nameof(reqGetRoom)));
        }
        /// <summary>
        /// 局域网默认玩家使用随机Guid，没有玩家名字
        /// </summary>
        /// <returns></returns>
        public override RoomPlayerData getLocalPlayerData()
        {
            if (_playerData == null)
                _playerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "玩家1", RoomPlayerType.human);
            return _playerData;
        }
        /// <summary>
        /// 局域网创建房间直接返回构造好的房间供ClientLogic持有。
        /// </summary>
        /// <param name="hostPlayerData"></param>
        /// <returns></returns>
        /// <remarks>游戏大厅的话，就应该是返回游戏大厅构造并且保存在列表里的房间了吧</remarks>
        public override Task<RoomData> createRoom(RoomPlayerData hostPlayerData, int port = -1)
        {
            RoomData data = new RoomData();
            data.playerDataList.Add(hostPlayerData);
            data.ownerId = hostPlayerData.id;
            invokeBroadcast(nameof(ackCreateRoom), port, data);
            return Task.FromResult(data);
        }
        /// <summary>
        /// 远程调用方法，当收到创建房间消息时被调用
        /// </summary>
        /// <param name="data"></param>
        public void ackCreateRoom(RoomData data)
        {
            onNewRoomAck?.Invoke(data);
        }
        public event Action<RoomData> onNewRoomAck;
        /// <summary>
        /// 广播一个刷新房间列表的消息。
        /// </summary>
        /// <param name="port"></param>
        public override void refreshRooms(int port = -1)
        {
            invokeBroadcast(nameof(reqGetRoom), port);
        }
        public void reqGetRoom()
        {
            RoomData roomData = onGetRoomReq?.Invoke();
            invoke(unconnectedInvokeIP, nameof(ackGetRoom), roomData);
        }
        /// <summary>
        /// 当局域网收到发现房间的请求的时候被调用，需要返回当前ClientLogic的房间信息。
        /// </summary>
        public event Func<RoomData> onGetRoomReq;
        public void ackGetRoom(RoomData roomData)
        {
            onNewRoomAck?.Invoke(roomData);
        }
        /// <summary>
        /// 获取房间列表，在局域网实现下实际上是返回发现的第一个房间。
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override async Task<RoomData[]> getRooms(int port = -1)
        {
            RoomData data = await invokeBroadcastAny<RoomData>("discoverRoom", port);
            return new RoomData[] { data };
        }
        /// <summary>
        /// 被远程调用的发现房间方法，提供事件接口给ClientLogic用于回复存在的房间。
        /// </summary>
        /// <returns></returns>
        public RoomData discoverRoom()
        {
            return onGetRoomReq?.Invoke();
        }
        /// <summary>
        /// 添加AI玩家，实际上就是直接构造玩家数据然后返回给ClientLogic，在此之前通知其他玩家。
        /// </summary>
        /// <returns></returns>
        public override Task<RoomPlayerData> addAIPlayer()
        {
            RoomPlayerData aiPlayerData = new RoomPlayerData(Guid.NewGuid().GetHashCode(), "AI", RoomPlayerType.ai);
            //通知其他玩家添加AI玩家
            return Task.FromResult(aiPlayerData);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomData"></param>
        /// <param name="joinPlayerData"></param>
        /// <returns></returns>
        public override Task<RoomData> joinRoom(RoomData roomData, RoomPlayerData joinPlayerData)
        {
            return base.joinRoom(roomData, joinPlayerData);
        }
        #endregion
        #region 私有成员
        RoomPlayerData _playerData;
        #endregion
    }
}