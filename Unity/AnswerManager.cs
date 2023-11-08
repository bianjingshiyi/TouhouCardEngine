using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public class AnswerManager : MonoBehaviour, IAnswerManager, IDisposable
    {
        #region 公有方法

        #region 请求
        /// <summary>
        /// 询问指定的玩家一个请求，如果在指定时间内回应则返回回应，超时则返回默认回应，如果调用了Cancel则任务被取消。
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<IResponse> ask(int playerId, IRequest request, float timeout = float.MaxValue)
        {
            game?.logger?.logTrace("Answer", $"询问玩家{playerId}：{request}，超时时间：{timeout}");
            var item = startRequest(request, new int[] { playerId }, true, timeout);
            var responses = await item.tcs.Task;
            if (responses != null && responses.Count > 0)
                return responses[playerId];
            else
                return null;
        }
        /// <summary>
        /// 询问指定的多个玩家一个请求，如果在指定时间内所有玩家都回应则返回回应，超时则返回默认回应，如果调用了Cancel则任务被取消。
        /// </summary>
        /// <param name="playersId"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<Dictionary<int, IResponse>> askAll(int[] playersId, IRequest request, float timeout)
        {
            game?.logger?.logTrace("Answer", $"询问所有玩家（{string.Join("，", playersId)}）：{request}");
            var item = startRequest(request, playersId, false, timeout);
            return item.tcs.Task;
        }
        /// <summary>
        /// 询问所有给出的玩家一个request，如果任意玩家回复了满足条件的response，则返回它，否则返回null
        /// </summary>
        /// <param name="playersId"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <param name="responseFilter"></param>
        /// <returns></returns>
        public async Task<IResponse> askAny(int[] playersId, IRequest request, float timeout, Func<IResponse, bool> responseFilter = null)
        {
            game?.logger?.logTrace("Answer", $"询问任意玩家（{string.Join("，", playersId)}）：{request}");
            var item = startRequest(request, playersId, true, timeout);
            var responses = await item.tcs.Task;
            if (responses != null && responses.Count > 0)
                return responses.First().Value;
            else
                return null;
        }
        #endregion

        #region 回应
        /// <summary>
        /// 玩家对当前的请求作出回应，返回这个回应是否生效了。
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public Task<bool> answer(int playerId, IResponse response)
        {
            return answer(playerId, response, client);
        }
        public void unaskedAnswer(int playerId, IResponse response)
        {
            unaskedAnswer(playerId, response, client);
        }
        #endregion

        #region 取消
        public void cancel(IRequest request)
        {
            game?.logger?.logTrace("Answer", $"取消询问{request}");
            RequestItem item = _requestList.FirstOrDefault(i => i.request == request);
            if (item != null)
            {
                game?.logger?.logTrace("Answer", $"{item.request}取消自动回应");
                try
                {
                    cancelRequest(item);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{item.request}取消引发异常：{e}");
                }
            }
        }
        public void cancel(IRequest[] requests)
        {
            foreach (IRequest request in requests)
            {
                cancel(request);
            }
        }
        public void cancelAll()
        {
            game?.logger?.logTrace("Answer", "取消所有询问");
            while (_requestList.Count > 0)
            {
                var item = _requestList[0];
                game?.logger?.logTrace("Answer", $"{item.request}取消自动回应");
                try
                {
                    cancelRequest(item);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{item.request}取消引发异常：{e}");
                }
            }
        }
        #endregion

        #region 获取请求
        public IRequest getLastRequest(int playerId)
        {
            if (_requestList.LastOrDefault(i => i.request.playersId.Contains(playerId)) is RequestItem item)
                return item.request;
            else
                return null;
        }
        public IRequest[] getRequests(int playerId)
        {
            return _requestList.Where(item => item.request.playersId.Contains(playerId)).Select(item => item.request).ToArray();
        }
        public T getRequest<T>(int playerId) where T : IRequest
        {
            return getRequests(playerId).OfType<T>().FirstOrDefault();
        }
        public IRequest[] getAllRequests()
        {
            return _requestList.Select(item => item.request).ToArray();
        }
        public float getRemainedTime(IRequest request)
        {
            var item = _requestList.FirstOrDefault(i => i.request == request);
            if (item == null)
                return 0;
            else
                return item.remainedTime;
        }

        #endregion

        #region 获取回应
        public IResponse getResponse(int playerId, IRequest request)
        {
            var item = _requestList.FirstOrDefault(i => i.request == request);
            if (item == null)
                return null;
            if (item.responseDic.ContainsKey(playerId))
                return item.responseDic[playerId];
            else
                return null;
        }
        #endregion

        public void Dispose()
        {
            Destroy(gameObject);
        }
        #endregion

        #region 私有方法

        #region 生命周期
        protected void Update()
        {
            float deltaTime = Time.deltaTime;
            while (deltaTime > 0 && _requestList.Count > 0) // 还有剩余的处理时间，并且有待处理的请求
            {
                // 获取最后一位的请求。
                var item = _requestList[_requestList.Count - 1];

                // 处理请求的剩余时间。
                if (item.remainedTime >= deltaTime) // 剩余时间充足。
                {
                    item.remainedTime -= deltaTime;
                    deltaTime = 0;
                }
                else // 剩余时间不足。
                {
                    deltaTime -= item.remainedTime;
                    item.remainedTime = 0;
                }

                // 超时。
                if (item.remainedTime <= 0)
                {
                    game?.logger?.logTrace("Answer", $"玩家{string.Join("，", item.request.playersId)}的询问{item.request}超时自动回应");
                    try
                    {
                        cancelRequest(item);
                    }
                    catch (Exception e)
                    {
                        game?.logger?.logTrace("Error", $"{item.request}超时引发异常：{e}");
                    }
                    game?.logger?.logTrace($"当前询问：\n{string.Join("\n", _requestList)}");
                }
            }
        }
        #endregion

        #region 事件回调
        Task onReceive(int id, object obj)
        {
            if (obj is IResponse response)
            {
                if (id != response.playerId)
                    throw new InvalidOperationException($"收到来自客户端{id}的玩家{response.playerId}的指令{response}");
                if (response.isUnasked)
                    unaskedAnswer(response.playerId, response, null);
                else if (id != client.id)//数据可能来自自己，这种情况已经在await send中处理了，就不需要再调用一遍了。
                    _ = answer(response.playerId, response, null);
            }
            return Task.CompletedTask;
        }
        #endregion
        private RequestItem startRequest(IRequest request, int[] playersId, bool any, float timeout)
        {
            if (timeout < 0)
                timeout = 0;
            request.playersId = playersId;
            request.isAny = any;
            request.timeout = timeout;
            TaskCompletionSource<Dictionary<int, IResponse>> tcs = new TaskCompletionSource<Dictionary<int, IResponse>>();
            var item = new RequestItem(request, tcs, null)
            {
                remainedTime = timeout
            };
            _requestList.Add(item);
            onRequest?.Invoke(request);
            return item;
        }
        private async Task<bool> answer(int playerId, IResponse response, IClient client)
        {
            response.playerId = playerId;
            response.isUnasked = false;
            if (client != null)
                response = await client.send(response);
            for (int i = _requestList.Count - 1; i >= 0; i--)
            {
                var item = _requestList[i];
                // 请求必须正确
                if (!item.isCorrectResponse(response))
                    continue;

                var request = item.request;
                game?.logger?.logTrace("Answer", $"玩家{playerId}回应请求{request}");
                response.remainedTime = item.remainedTime;
                try
                {
                    item.responseDic.Add(playerId, response);
                    if (request.isAny || item.isAllResponsed()) // 所有玩家都回答请求完毕，或者任意玩家回答请求都可以。
                    {
                        completeRequest(item);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"{response}回应{request}发生异常：{e}");
                    return false;
                }
                onResponse?.Invoke(response);
                return true;
            }
            return false;
        }
        private void unaskedAnswer(int playerId, IResponse response, IClient client)
        {
            response.playerId = playerId;
            response.isUnasked = true;
            if (client != null)
            {
                client.send(response);
                return;
            }
            onResponse?.Invoke(response);
        }
        private void completeRequest(RequestItem item)
        {
            _requestList.Remove(item);
            item.tcs.SetResult(item.responseDic);
        }
        private void cancelRequest(RequestItem item)
        {
            _requestList.Remove(item);
            if (item.request.isAny) // 任意一个玩家回应都行
            {
                if (item.request.playersId.Length == 1)
                {
                    // 强制请求的唯一一个玩家进行回应。
                    var playerId = item.request.playersId[0];
                    IResponse response = item.request.getDefaultResponse(game, playerId);
                    item.tcs.SetResult(new Dictionary<int, IResponse>()
                            {
                                { playerId, response }
                            });
                    onResponse?.Invoke(response);
                }
                else
                {
                    // 没有人回应。
                    item.tcs.SetResult(new Dictionary<int, IResponse>());
                }
            }
            else // 需要双方玩家都回应。
            {
                // 强制双方玩家使用已经回应的response，或者默认的reponse进行回应。
                Dictionary<int, IResponse> responses = item.request.playersId.Select(p =>
                {
                    IResponse response = null;
                    // 该玩家已经回应过该请求，直接获取
                    if (item.responseDic.FirstOrDefault(r => r.Key == p).Value is IResponse res)
                    {
                        response = res;
                    }
                    else // 没有回应过，创建一个默认的
                    {
                        response = item.request.getDefaultResponse(game, p);
                        response.playerId = p;
                    }
                    return response;
                }).ToDictionary(r => r.playerId);
                item.tcs.SetResult(responses);
                foreach (var response in responses.Values)
                {
                    onResponse?.Invoke(response);
                }
            }
        }
        #endregion

        #region 事件
        public event Action<IRequest> onRequest;
        public event Action<IResponse> onResponse;
        #endregion

        #region 属性字段
        public IGame game { get; set; } = null;
        public IClient client
        {
            get { return _client; }
            set
            {
                if (_client != null)
                {
                    _client.onReceive -= onReceive;
                }
                _client = value;
                if (_client != null)
                {
                    _client.onReceive += onReceive;
                }
            }
        }
        IClient _client = null;
        [SerializeField]
        List<RequestItem> _requestList = new List<RequestItem>();
        #endregion

        #region 内嵌类
        [Serializable]
        public class RequestItem
        {
            public IRequest request { get; }
            public TaskCompletionSource<Dictionary<int, IResponse>> tcs { get; }
            [SerializeField]
            string _name;
            [SerializeField]
            int[] _players;
            [SerializeField]
            float _remainedTime;
            public float remainedTime
            {
                get { return _remainedTime; }
                set { _remainedTime = value; }
            }
            public Dictionary<int, IResponse> responseDic { get; } = new Dictionary<int, IResponse>();
            public Func<IResponse, bool> responseFilter { get; }
            public RequestItem(IRequest request, TaskCompletionSource<Dictionary<int, IResponse>> tcs, Func<IResponse, bool> responseFilter)
            {
                this.request = request;
                _name = request.GetType().Name;
                _players = request.playersId;
                this.tcs = tcs;
                this.responseFilter = responseFilter;
            }
            public bool isCorrectResponse(IResponse response)
            {
                var playerId = response.playerId;
                if (!request.playersId.Contains(playerId))//问了这个玩家
                    return false;
                if (request.isAny && responseDic.Any(r => r.Key == playerId))//如果是任意玩家回应就会结束的请求，那么玩家不能回应过
                    return false;
                if (responseFilter != null && !responseFilter(response))//如果有条件，那么要满足条件
                    return false;
                if (!request.isValidResponse(response))//是合法的回应
                    return false;
                return true;
            }
            /// <summary>
            /// 是否所有玩家的请求都回应完毕。
            /// </summary>
            /// <returns>回应完毕。</returns>
            public bool isAllResponsed()
            {
                return request.playersId.All(p => responseDic.Any(r => r.Key == p));
            }
            public override string ToString()
            {
                return request.ToString();
            }
        }
        #endregion
    }
}
