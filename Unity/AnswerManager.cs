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
        [Serializable]
        public class RequestItem
        {
            public IRequest request { get; }
            public TaskCompletionSource<IResponse[]> tcs { get; }
            [SerializeField]
            float _remainedTime;
            public float remainedTime
            {
                get { return _remainedTime; }
                set { _remainedTime = value; }
            }
            public List<IResponse> responseList { get; } = new List<IResponse>();
            public Func<IResponse, bool> responseFilter { get; }
            public RequestItem(IRequest request, TaskCompletionSource<IResponse[]> tcs, Func<IResponse, bool> responseFilter)
            {
                this.request = request;
                this.tcs = tcs;
                this.responseFilter = responseFilter;
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
                            item.tcs.SetResult(new IResponse[] { item.request.getDefaultResponse(game) });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(item.request + "超时引发异常：" + e);
                        }
                    }
                    else
                    {
                        try
                        {
                            item.tcs.SetResult(item.request.playersId.Select(p =>
                            {
                                if (item.responseList.FirstOrDefault(r => r.playerId == p) is IResponse response)
                                    return response;
                                else
                                {
                                    response = item.request.getDefaultResponse(game);
                                    response.playerId = p;
                                    return response;
                                }
                            }).ToArray());
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(item.request + "超时引发异常：" + e);
                        }
                    }
                    _requestList.RemoveAt(_requestList.Count - 1);
                }
            }
        }
        public async Task<IResponse> ask(int playerId, IRequest request, float timeout)
        {
            request.playersId = new int[] { playerId };
            request.isAny = true;
            if (timeout < 0)
                timeout = 0;
            request.timeout = timeout;
            TaskCompletionSource<IResponse[]> tcs = new TaskCompletionSource<IResponse[]>();
            _requestList.Add(new RequestItem(request, tcs, null)
            {
                remainedTime = timeout
            });
            var responses = await tcs.Task;
            if (responses != null && responses.Length > 0)
                return responses[0];
            else
                return null;
        }
        public Task<IResponse[]> askAll(int[] playersId, IRequest request, float timeout)
        {
            request.playersId = playersId;
            request.isAny = false;
            if (timeout < 0)
                timeout = 0;
            request.timeout = timeout;
            TaskCompletionSource<IResponse[]> tcs = new TaskCompletionSource<IResponse[]>();
            _requestList.Add(new RequestItem(request, tcs, null)
            {
                remainedTime = timeout
            });
            return tcs.Task;
        }
        public async Task<IResponse> askAny(int[] playersId, IRequest request, float timeout, Func<IResponse, bool> responseFilter = null)
        {
            request.playersId = playersId;
            request.isAny = true;
            if (timeout < 0)
                timeout = 0;
            request.timeout = timeout;
            TaskCompletionSource<IResponse[]> tcs = new TaskCompletionSource<IResponse[]>();
            _requestList.Add(new RequestItem(request, tcs, responseFilter)
            {
                remainedTime = timeout
            });
            var responses = await tcs.Task;
            if (responses != null && responses.Length > 0)
                return responses[0];
            else
                return null;
        }
        public bool answer(int playerId, IResponse response)
        {
            response.playerId = playerId;
            for (int i = _requestList.Count - 1; i >= 0; i--)
            {
                var item = _requestList[i];
                var request = item.request;
                if (request.playersId.Contains(playerId) &&//问了这个玩家
                    (request.isAny || !item.responseList.Any(r => r.playerId == playerId)) &&//如果是面对所有玩家的请求，那么玩家不能回应过
                    (item.responseFilter == null || item.responseFilter(response)) &&//如果有条件，那么要满足条件
                    request.isValidResponse(response))//是合法的回应
                {
                    if (request.isAny)
                    {
                        try
                        {
                            item.tcs.SetResult(new IResponse[] { response });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(response + "回应" + request + "发生异常：" + e);
                            return false;
                        }
                        _requestList.RemoveAt(i);
                        return true;
                    }
                    else
                    {
                        item.responseList.Add(response);
                        if (item.request.playersId.All(p => item.responseList.Any(r => r.playerId == p)))
                        {
                            try
                            {
                                item.tcs.SetResult(item.responseList.ToArray());
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(response + "回应" + request + "发生异常：" + e);
                                return false;
                            }
                            _requestList.RemoveAt(i);
                            return true;
                        }
                        else
                            return true;
                    }
                }
            }
            return false;
        }
        public IRequest getLastRequest(int playerId)
        {
            if (_requestList.LastOrDefault(i => i.request.playersId.Contains(playerId)) is var item)
                return item.request;
            else
                return null;
        }
        public IRequest[] getRequests(int playerId)
        {
            return _requestList.Where(item => item.request.playersId.Contains(playerId)).Select(item => item.request).ToArray();
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
    }
}
