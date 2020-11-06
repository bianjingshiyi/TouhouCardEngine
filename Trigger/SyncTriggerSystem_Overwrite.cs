using System;

namespace TouhouCardEngine
{
    partial class SyncTriggerSystem
    {
        /// <summary>
        /// 执行一项任务，返回执行任务对象。
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public SyncTask doTask(ActionCollection actions)
        {
            return doTask(null, actions);
        }
        /// <summary>
        /// doTask的简化重载版本，会为传入的Action集合生成ActionCollection，
        /// 然后调用真正的doTask。
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public SyncTask doTask(EventContext eventContext, params Action<CardEngine>[] actions)
        {
            return doTask(eventContext, new ActionCollection(actions));
        }
        public SyncTask doTask(params Action<CardEngine>[] actions)
        {
            return doTask(null, new ActionCollection(actions));
        }
        public SyncTask doTask(string name, params Action<CardEngine>[] actions)
        {
            return doTask(new EventContext(name), new ActionCollection(actions));
        }
    }
}