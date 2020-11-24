using System;
using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    partial class SyncTriggerSystem
    {
        #region 公共成员
        public SyncTask request(int[] playersId, EventContext requestContext,
            float timeout, SyncAction onTimeout,
            SyncFunc<bool> isValidResponse, bool isAny)
        {
            Timer timer = game.time.startTimer(timeout) as Timer;
            requestContext[nameof(timer)] = timer;
            requestContext[nameof(isValidResponse)] = isValidResponse;
            requestContext[nameof(isAny)] = isAny;
            requestContext[nameof(playersId)] = playersId;
            SyncTask reqTask = doTask(requestContext,
                game => game.trigger.pauseTask(game.trigger.currentTask)
            );
            timer.onExpired += () => {
                onTimeout.execute(game);
                stopTask(reqTask);
            };
            return reqTask;
        }
        public SyncTask request(int playerId, EventContext requestContext,
            float timeout, SyncAction onTimeout)
        {
            return request(new int[] { playerId }, requestContext, timeout, onTimeout, FLambda<bool>.True, false);
        }
        public SyncTask request(int playerId, EventContext requestContext,
            float timeout, SyncAction onTimeout, SyncFunc<bool> isValidResponse)
        {
            return request(new int[] { playerId }, requestContext, timeout, onTimeout, isValidResponse, false);
        }
        public SyncTask requestAny(int[] playersId, EventContext requestContext,
            float timeout, SyncAction onTimeout,
            SyncFunc<bool> isValidResponse = null)
        {
            return request(playersId, requestContext, timeout, onTimeout, isValidResponse, true);
        }
        public SyncTask requestAll(int[] playersId, EventContext requestContext,
            float timeout, SyncAction onTimeout)
        {
            return request(playersId, requestContext, timeout, onTimeout, FLambda<bool>.Default, false);
        }
        public event Action<EventContext> onRequest;
        public SyncTask[] getRequestTasks(int playerId)
        {
            throw new NotImplementedException();
        }
        public SyncTask[] getAllRequestTasks()
        {
            return _pausedTaskList.ToArray();
        }
        public float getRemainedTime(SyncTask requestTask)
        {
            return (requestTask.context["timer"] as Timer).remainedTime;
        }
        public SyncTask response(int playerId, EventContext responseContext)
        {
            SyncTask resTask = _pausedTaskList.FirstOrDefault();
            foreach (SyncTask task in _pausedTaskList) {
                EventContext currContext = task.context;
                if (currContext.getVar<bool>("isAny")&& currContext.getVar<int[]>("playersId").Contains(playerId)
                    || currContext.getVar<int[]>("playersId").FirstOrDefault() == playerId) {
                    foreach (var item in responseContext) {
                        task.context[item.Key] = item.Value;
                    }
                    currentTask = resTask;
                    if (!currContext.getVar<SyncFunc<bool>>("isValidResponse").evaluate(game)) {
                        currentTask = null;
                        pauseTask(resTask);
                    }
                    else{
                        currentTask = null;
                        resTask = resumeTask(resTask);
                    }
                    return resTask;
                }
            }
            return resTask;
        }
        public EventContext getResponse(int playerId, SyncTask requestTask)
        {
            throw new NotImplementedException();
        }
        public event Action<EventContext> onResponse;
        public void cancel(SyncTask requestTask)
        {
            throw new NotImplementedException();
        }
        public void cancel(IEnumerable<SyncTask> requestTasks)
        {
            throw new NotImplementedException();
        }
        public void cancelAll()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}