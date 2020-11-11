using System;
using System.Collections.Generic;

namespace TouhouCardEngine
{
    partial class SyncTriggerSystem
    {
        #region 公共成员
        public SyncTask request(int[] playersId, EventContext requestContext,
            float timeout, SyncAction onTimeout,
            SyncFunc<bool> isValidResponse, bool isAny)
        {
            throw new NotImplementedException();
        }
        public SyncTask request(int playerId, EventContext requestContext,
            float timeout, SyncAction onTimeout)
        {
            return request(new int[] { playerId }, requestContext, timeout, onTimeout, FLambda<bool>.Default, false);
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
            throw new NotImplementedException();
        }
        public float getRemainedTime(SyncTask requestTask)
        {
            throw new NotImplementedException();
        }
        public SyncTask response(int playerId, EventContext responseContext)
        {
            throw new NotImplementedException();
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