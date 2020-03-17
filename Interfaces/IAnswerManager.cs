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
        IResponse getDefaultResponse(IGame game);
    }
    public interface IResponse
    {
        int playerId { get; set; }
    }
    public interface IAnswerManager
    {
        Task<IResponse> ask(int playerId, IRequest request, float timeout);
        Task<IResponse[]> askAll(int[] playersId, IRequest request, float timeout);
        Task<IResponse> askAny(int[] playersId, IRequest request, float timeout, Func<IResponse, bool> responseFilter);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="response"></param>
        /// <returns>返回值表示这次回应是否有相应的请求。</returns>
        bool answer(int playerId, IResponse response);
        IRequest getLastRequest(int playerId);
        IRequest[] getRequests(int playerId);
        IRequest[] getAllRequests();
        /// <summary>
        /// 获取请求剩余的时间（毫秒）
        /// </summary>
        /// <param name="request"></param>
        /// <returns>单位为毫秒</returns>
        float getRemainedTime(IRequest request);
    }
}
