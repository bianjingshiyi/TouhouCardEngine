using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouhouCardEngine.Interfaces
{
    public interface IRequest
    {
        int[] playersId { get; set; }
        bool isAny { get; set; }
        float timeout { get; set; }
        bool isValidResponse(IResponse response);
        IResponse getDefaultResponse(IGame game, int playerId);
    }
    public interface IResponse
    {
        int playerId { get; set; }
        bool isUnasked { get; set; }
        float remainedTime { get; set; }
    }
    public interface IAnswerManager
    {
        Task<IResponse> ask(int playerId, IRequest request, float timeout);
        /// <summary>
        /// 对给出的所有玩家进行询问，直到所有人都进行回应或超时后返回询问结果。
        /// </summary>
        /// <param name="playersId"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task<Dictionary<int, IResponse>> askAll(int[] playersId, IRequest request, float timeout);
        Task<IResponse> askAny(int[] playersId, IRequest request, float timeout, Func<IResponse, bool> responseFilter);
        /// <summary>
        /// 某玩家回应一次请求。
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="response"></param>
        /// <returns>返回值表示这次回应是否有相应的请求。</returns>
        Task<bool> answer(int playerId, IResponse response);
        /// <summary>
        /// 单纯的做一次回应，不响应任何请求。
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        void unaskedAnswer(int playerId, IResponse response);
        IRequest getLastRequest(int playerId);
        IRequest[] getRequests(int playerId);
        IRequest[] getAllRequests();
        /// <summary>
        /// 获取请求剩余的时间（毫秒）
        /// </summary>
        /// <param name="request"></param>
        /// <returns>单位为毫秒</returns>
        float getRemainedTime(IRequest request);
        event Action<IRequest> onRequest;
        event Action<IResponse> onResponse;
        void cancel(IRequest request);
        void cancel(IRequest[] requests);
        void cancelAll();
    }
    public interface ITimeManager
    {

    }
    public interface ITimer
    {
        float time { get; }
        event Action onExpired;
    }
}
