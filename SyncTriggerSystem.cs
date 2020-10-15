using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
namespace TouhouCardEngine
{
    /// <summary>
    /// 同步触发系统
    /// </summary>
    public class SyncTriggerSystem
    {
        public SyncTask currentTask => _currentTask;
        public SyncTask _currentTask;
        /// <summary>
        /// 执行一项任务，返回执行任务对象。
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public SyncTask doTask(ActionCollection actions)
        {
            throw new NotImplementedException();
        }
    }
    public class SyncTask
    {
        public SyncTaskState state => _state;
        public SyncTaskState _state;
        public void pause()
        {
            throw new NotImplementedException();
        }
        public void resume()
        {
            throw new NotImplementedException();
        }
    }
    public enum SyncTaskState
    {
        running,
        paused,
        completed
    }
    public class ActionCollection : IEnumerable<SyncAction>
    {
        public List<SyncAction> _actionList = new List<SyncAction>();
        [SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
        public void Add(SyncAction action)
        {
            _actionList.Add(action);
        }
        public IEnumerator<SyncAction> GetEnumerator()
        {
            return ((IEnumerable<SyncAction>)_actionList).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _actionList.GetEnumerator();
        }
    }
    public class SyncAction
    {
        public virtual void execute()
        {
        }
    }
}