using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public class AnswerManager : MonoBehaviour, IAnswerManager
    {
        IClientManager _client = null;
        public IClientManager client
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
            public override string ToString()
            {
                return request.ToString();
            }
        }
        [SerializeField]
        List<RequestItem> _requestList = new List<RequestItem>();
        public IGame game { get; set; } = null;
        protected void Update()
        {
            float deltaTime = Time.deltaTime;
            while (deltaTime > 0 && _requestList.Count > 0)
            {
                var item = _requestList[_requestList.Count - 1];
                if (item.remainedTime >= deltaTime)
                {
                    item.remainedTime -= deltaTime;
                    deltaTime = 0;
                }
                else
                {
                    deltaTime -= item.remainedTime;
                    item.remainedTime = 0;
                }
                if (item.remainedTime <= 0)
                {
                    if (item.request.isAny)
                    {
                        try
                        {
                            game?.logger?.log("Answer", "玩家" + string.Join("，", item.request.playersId) + "的询问" + item.request + "超时自动回应");
                            if (item.request.playersId.Length == 1)
                                item.tcs.SetResult(new Dictionary<int, IResponse>()
                                {
                                    { item.request.playersId[0], item.request.getDefaultResponse(game, item.request.playersId[0]) }
                                });
                            else
                                item.tcs.SetResult(new Dictionary<int, IResponse>());
                        }
                        catch (Exception e)
                        {
                            game?.logger?.log("Error", item.request + "超时引发异常：" + e);
                        }
                    }
                    else
                    {
                        try
                        {
                            game?.logger?.log("Answer", item.request + "超时自动回应");
                            item.tcs.SetResult(item.request.playersId.Select(p =>
                            {
                                if (item.responseDic.FirstOrDefault(r => r.Key == p).Value is IResponse response)
                                    return response;
                                else
                                {
                                    response = item.request.getDefaultResponse(game, p);
                                    response.playerId = p;
                                    return response;
                                }
                            }).ToDictionary(r => r.playerId));
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(item.request + "超时引发异常：" + e);
                        }
                    }
                    _requestList.Remove(item);
                    game?.logger?.log("当前询问：\n" + string.Join("\n", _requestList));
                }
            }
        }
        /// <summary>
        /// 询问指定的玩家一个请求，如果在指定时间内回应则返回回应，超时则返回默认回应，如果调用了Cancel则任务被取消。
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<IResponse> ask(int playerId, IRequest request, float timeout = float.MaxValue)
        {
            game?.logger?.log("Answer", "询问玩家" + playerId + "：" + request + "，超时时间：" + timeout);
            request.playersId = new int[] { playerId };
            request.isAny = true;
            if (timeout < 0)
                timeout = 0;
            request.timeout = timeout;
            TaskCompletionSource<Dictionary<int, IResponse>> tcs = new TaskCompletionSource<Dictionary<int, IResponse>>();
            _requestList.Add(new RequestItem(request, tcs, null)
            {
                remainedTime = timeout
            });
            onRequest?.Invoke(request);
            var responses = await tcs.Task;
            if (responses != null && responses.Count > 0)
                return responses[playerId];
            else
                return null;
        }
        public Task<Dictionary<int, IResponse>> askAll(int[] playersId, IRequest request, float timeout)
        {
            game?.logger?.log("Answer", "询问所有玩家（" + string.Join("，", playersId) + "）：" + request);
            request.playersId = playersId;
            request.isAny = false;
            if (timeout < 0)
                timeout = 0;
            request.timeout = timeout;
            TaskCompletionSource<Dictionary<int, IResponse>> tcs = new TaskCompletionSource<Dictionary<int, IResponse>>();
            _requestList.Add(new RequestItem(request, tcs, null)
            {
                remainedTime = timeout
            });
            onRequest?.Invoke(request);
            return tcs.Task;
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
            game?.logger?.log("Answer", "询问任意玩家（" + string.Join("，", playersId) + "）：" + request);
            request.playersId = playersId;
            request.isAny = true;
            if (timeout < 0)
                timeout = 0;
            request.timeout = timeout;
            TaskCompletionSource<Dictionary<int, IResponse>> tcs = new TaskCompletionSource<Dictionary<int, IResponse>>();
            _requestList.Add(new RequestItem(request, tcs, responseFilter)
            {
                remainedTime = timeout
            });
            onRequest?.Invoke(request);
            var responses = await tcs.Task;
            if (responses != null && responses.Count > 0)
                return responses.First().Value;
            else
                return null;
        }
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
        private async Task<bool> answer(int playerId, IResponse response, IClientManager client)
        {
            response.playerId = playerId;
            response.isUnasked = false;
            if (client != null)
                response = await client.send(response);
            for (int i = _requestList.Count - 1; i >= 0; i--)
            {
                var item = _requestList[i];
                var request = item.request;
                if (!request.playersId.Contains(playerId))//问了这个玩家
                    continue;
                if (request.isAny && item.responseDic.Any(r => r.Key == playerId))//如果是面对所有玩家的请求，那么玩家不能回应过
                    continue;
                if (item.responseFilter != null && !item.responseFilter(response))//如果有条件，那么要满足条件
                    continue;
                if (!request.isValidResponse(response))//是合法的回应
                    continue;

                game?.logger?.log("Answer", "玩家" + playerId + "回应请求" + request);
                response.remainedTime = item.remainedTime;
                if (request.isAny)
                {
                    _requestList.Remove(item);
                    try
                    {
                        item.tcs.SetResult(new Dictionary<int, IResponse>()
                            {
                                { playerId, response }
                            });
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(response + "回应" + request + "发生异常：" + e);
                        return false;
                    }
                    onResponse?.Invoke(response);
                    return true;
                }
                else
                {
                    item.responseDic.Add(playerId, response);
                    if (item.request.playersId.All(p => item.responseDic.Any(r => r.Key == p)))
                    {
                        _requestList.Remove(item);
                        try
                        {
                            item.tcs.SetResult(item.responseDic);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(response + "回应" + request + "发生异常：" + e);
                            return false;
                        }
                        onResponse?.Invoke(response);
                        return true;
                    }
                    else
                    {
                        onResponse?.Invoke(response);
                        return true;
                    }
                }
            }
            return false;
        }

        public void unaskedAnswer(int playerId, IResponse response)
        {
            unaskedAnswer(playerId, response, client);
        }
        private void unaskedAnswer(int playerId, IResponse response, IClientManager client)
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
        Task onReceive(int id, object obj)
        {
            if (obj is IResponse response)
            {
                if (id != response.playerId)
                    throw new InvalidOperationException("收到来自客户端" + id + "的玩家" + response.playerId + "的指令" + response);
                if (response.isUnasked)
                    unaskedAnswer(response.playerId, response, null);
                else if (id != client.id)//数据可能来自自己，这种情况已经在await send中处理了，就不需要再调用一遍了。
                    _ = answer(response.playerId, response, null);
            }
            return Task.CompletedTask;
        }
        public event Action<IRequest> onRequest;
        public event Action<IResponse> onResponse;
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
            return getRequests(playerId).OfType<T>().First();
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
        public void cancel(IRequest request)
        {
            game?.logger?.log("Answer", "取消询问" + request);
            RequestItem item = _requestList.FirstOrDefault(i => i.request == request);
            if (item != null)
            {
                if (item.request.isAny)
                {
                    try
                    {
                        game?.logger?.log("Answer", item.request + "取消自动回应");
                        if (item.request.playersId.Length == 1)
                            item.tcs.SetResult(new Dictionary<int, IResponse>()
                                {
                                    { item.request.playersId[0], item.request.getDefaultResponse(game, item.request.playersId[0]) }
                                });
                        else
                            item.tcs.SetResult(new Dictionary<int, IResponse>());
                    }
                    catch (Exception e)
                    {
                        game?.logger?.log("Error", item.request + "取消引发异常：" + e);
                    }
                }
                else
                {
                    try
                    {
                        game?.logger?.log("Answer", item.request + "取消自动回应");
                        item.tcs.SetResult(item.request.playersId.Select(p =>
                        {
                            if (item.responseDic.FirstOrDefault(r => r.Key == p).Value is IResponse response)
                                return response;
                            else
                            {
                                response = item.request.getDefaultResponse(game, p);
                                response.playerId = p;
                                return response;
                            }
                        }).ToDictionary(r => r.playerId));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(item.request + "取消引发异常：" + e);
                    }
                }
                _requestList.Remove(item);
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
            game?.logger?.log("Answer", "取消所有询问");
            foreach (var item in _requestList)
            {
                if (item.request.isAny)
                {
                    try
                    {
                        game?.logger?.log("Answer", item.request + "取消自动回应");
                        if (item.request.playersId.Length == 1)
                            item.tcs.SetResult(new Dictionary<int, IResponse>()
                                {
                                    { item.request.playersId[0], item.request.getDefaultResponse(game, item.request.playersId[0]) }
                                });
                        else
                            item.tcs.SetResult(new Dictionary<int, IResponse>());
                    }
                    catch (Exception e)
                    {
                        game?.logger?.log("Error", item.request + "取消引发异常：" + e);
                    }
                }
                else
                {
                    try
                    {
                        game?.logger?.log("Answer", item.request + "取消自动回应");
                        item.tcs.SetResult(item.request.playersId.Select(p =>
                        {
                            if (item.responseDic.FirstOrDefault(r => r.Key == p).Value is IResponse response)
                                return response;
                            else
                            {
                                response = item.request.getDefaultResponse(game, p);
                                response.playerId = p;
                                return response;
                            }
                        }).ToDictionary(r => r.playerId));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(item.request + "取消引发异常：" + e);
                    }
                }
            }
            _requestList.Clear();
        }
    }
}
