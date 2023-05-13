using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public interface INetworkingV3Client: IRoomClient
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
        /// <param name="name">房间名称</param>
        /// <param name="password">房间密码</param>
        /// <returns></returns>
        Task<RoomData> CreateRoom(string name = "", string password = "");

        /// <summary>
        /// 关闭当前已经创建的房间（部分情况下用不上）
        /// </summary>
        /// <returns></returns>
        Task DestroyRoom();

        /// <summary>
        /// 获取当前可加入的房间信息。
        /// 调用此方法后会立即返回，在获取到房间信息后多次触发OnRoomListUpdate事件
        /// </summary>
        /// <returns></returns>
        Task RefreshRoomList();

        /// <summary>
        /// 可加入的房间列表更新事件
        /// </summary>
        event Action<LobbyRoomDataList> OnRoomListUpdate;

        /// <summary>
        /// 修改当前房间的信息
        /// </summary>
        /// <param name="changedInfo"></param>
        /// <returns></returns>
        Task AlterRoomInfo(LobbyRoomData newInfo);
        #endregion

        #region Room
        /// <summary>
        /// 使用当前用户加入一个房间
        /// </summary>
        /// <param name="roomID"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<RoomData> JoinRoom(string roomID, string password);

        /// <summary>
        /// 获取房间属性
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        T GetRoomProp<T>(string name);

        /// <summary>
        /// 退出当前加入的房间
        /// </summary>
        /// <returns></returns>
        void QuitRoom();

        /// <summary>
        /// 获取当前网络的延迟
        /// </summary>
        /// <returns></returns>
        int GetLatency();

        /// <summary>
        /// 请求开始游戏！
        /// 注意只有房主能调用。
        /// </summary>
        /// <returns></returns>
        Task GameStart();
        #endregion

        #region Game
        /// <summary>
        /// 发送GameRequest
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task<byte[]> Send(byte[] data);

        /// <summary>
        /// 收到GameResponse
        /// </summary>
        event ResponseHandler onReceive;

        /// <summary>
        /// 游戏开始事件
        /// </summary>
        event Action OnGameStart;
        #endregion
    }


    /// <summary>
    /// 房间相关方法
    /// </summary>
    public interface IRoomClient
    {
        #region Room
        /// <summary>
        /// 房间玩家信息改变事件
        /// </summary>
        /// <returns></returns>
        event Action<RoomPlayerData[]> OnRoomPlayerDataChanged;

        /// <summary>
        /// 修改玩家的属性
        /// </summary>
        /// <returns></returns>
        Task SetPlayerProp(string name, object val);

        /// <summary>
        /// 修改房间的属性
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        Task SetRoomProp(string name, object val);

        /// <summary>
        /// 批量修改房间属性
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        Task SetRoomPropBatch(List<KeyValuePair<string, object>> values);

        /// <summary>
        /// 房间数据被修改后调用此方法
        /// 注意用户数据并不算房间数据，所以有用户加入实际上不会触发这个方法
        /// </summary>
        event Action<RoomData> OnRoomDataChange;
        #endregion

        #region Chat
        /// <summary>
        /// 收到聊天消息
        /// </summary>
        event Action<ChatMsg> OnRecvChat;

        /// <summary>
        /// 发送聊天消息
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendChat(int channel, string message);
        #endregion

        #region Suggest
        /// <summary>
        /// 收到提议请求
        /// </summary>
        event Action<int, CardPoolSuggestion> OnSuggestCardPools;
        /// <summary>
        /// 收到提议回应
        /// </summary>
        event Action<CardPoolSuggestion, bool> OnCardPoolsSuggestionAnwsered;

        /// <summary>
        /// 发起提议
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        Task SuggestCardPools(CardPoolSuggestion suggestion);
        /// <summary>
        /// 回应提议
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="suggestion"></param>
        /// <param name="agree"></param>
        /// <returns></returns>
        Task AnwserCardPoolsSuggestion(int playerId, CardPoolSuggestion suggestion, bool agree);
        #endregion

        #region Resource
        /// <summary>
        /// 获取指定资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<byte[]> GetResourceAsync(ResourceType type, string id);
        /// <summary>
        /// 上传指定资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        Task UploadResourceAsync(ResourceType type, string id, byte[] bytes);
        /// <summary>
        /// 判断指定资源是否存在
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> ResourceExistsAsync(ResourceType type, string id);
        /// <summary>
        /// 批量判断资源是否存在
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        Task<bool[]> ResourceBatchExistsAsync(Tuple<ResourceType, string>[] res);
        #endregion
    }

    /// <summary>
    /// 本地Host扩展的一些方法
    /// </summary>
    public interface INetworkingV3LANHost
    {
        Task AddPlayer(RoomPlayerData player);

        /// <summary>
        /// 当玩家请求加入房间的时候，是否回应？
        /// 检查玩家信息和房间信息，判断是否可以加入
        /// </summary>
        event Func<RoomPlayerData, RoomData> onJoinRoomReq;
    }

    /// <summary>
    /// GameResponse处理器
    /// </summary>
    /// <param name="clientID">发送者ID</param>
    /// <param name="obj">发送的数据</param>
    /// <returns></returns>
    public delegate Task ResponseHandler(int clientID, byte[] obj);
}