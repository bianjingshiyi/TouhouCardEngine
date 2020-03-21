﻿using System;
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
                        onAnswer?.Invoke(response);
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
                            onAnswer?.Invoke(response);
                            _requestList.RemoveAt(i);
                            return true;
                        }
                        else
                        {
                            onAnswer?.Invoke(response);
                            return true;
                        }
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
            onAnswer?.Invoke(response);
        }
        void onReceive(int id, object obj)
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
        }
        public event Action<IResponse> onAnswer;
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
        public void cancel(IRequest request)
        {
            RequestItem item = _requestList.FirstOrDefault(i => i.request == request);
            if (item != null)
            {
                item.tcs.SetCanceled();
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
            foreach (var item in _requestList)
            {
                item.tcs.SetCanceled();
            }
            _requestList.Clear();
        }
    }
}
