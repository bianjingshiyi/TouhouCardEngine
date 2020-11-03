using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;

namespace TouhouCardEngine
{
    /// <summary>
    /// 同步触发系统
    /// </summary>
    public partial class SyncTriggerSystem
    {
        #region 公开成员
        public SyncTriggerSystem(CardEngine game)
        {
            this.game = game;
        }
        /// <summary>
        /// 执行一项任务，返回执行任务对象。
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public SyncTask doTask(ActionCollection actions)
        {
            SyncTask task = new SyncTask(++_lastTaskId, actions);
            return doOrResumeTask(task);
        }
        /// <summary>
        /// doTask的简化重载版本，会为传入的Action集合生成ActionCollection，
        /// 然后调用真正的doTask。
        /// </summary>
        /// <param name="actions"></param>
        /// <returns></returns>
        public SyncTask doTask(EventContext eventContext,params Action<CardEngine>[] actions)
        {
            SyncTask task = new SyncTask(++_lastTaskId, new ActionCollection(actions))
            {
                _context = eventContext
            };
            return doOrResumeTask(task);
        }
        public SyncTask doTask(EventContext eventContext, ActionCollection actions)
        {
            SyncTask task = new SyncTask(++_lastTaskId, actions)
            {
                _context = eventContext
            };
            return doOrResumeTask(task);
        }
        public SyncTask doTask(params Action<CardEngine>[] actions)
        {
            SyncTask task = new SyncTask(++_lastTaskId, new ActionCollection(actions));
            return doOrResumeTask(task);
        }
        public void pauseTask(SyncTask task)
        {
            task.state = SyncTaskState.paused;
            _pausedTaskList.Add(task);
        }
        public SyncTask[] getPausedTasks()
        {
            return _pausedTaskList.ToArray();
        }
        public SyncTask resumeTask(SyncTask task)
        {
            if (_pausedTaskList.Remove(task))
            {
                return doOrResumeTask(task);
            }
            return null;
        }
        public SyncTask[] getResumeTaskStack()
        {
            return _resumeTaskStack.ToArray();
        }
        public void stopTask(SyncTask task)
        {
            task.state = SyncTaskState.finished;
            _pausedTaskList.Remove(task);
        }
        public SyncTask currentTask
        {
            get { return _currentTask; }
            private set { _currentTask = value; }
        }
        public SyncTask _currentTask;
        public List<SyncTask> _pausedTaskList = new List<SyncTask>();
        public List<SyncTask> _resumeTaskStack = new List<SyncTask>();
        public int _lastTaskId = 0;
        #endregion
        #region 私有成员
        SyncTask doOrResumeTask(SyncTask task)
        {
            bool isResumed = task.state == SyncTaskState.paused;
            if (currentTask != null)
            {
                if (isResumed)
                {
                    _resumeTaskStack.Add(currentTask);
                }
                else
                    currentTask.addChild(task);
            }
            task.state = SyncTaskState.running;
            currentTask = task;
            for (int i = task.curActionIndex; i < task.actions.length; i++)
            {
                task.curActionIndex = i;
                if (task.state == SyncTaskState.paused ||
                    task.state == SyncTaskState.finished)
                {
                    currentTask = null;
                    return task;
                }
                task.actions[i].execute(game);
            }
            task.state = SyncTaskState.finished;
            if (isResumed && _resumeTaskStack.Count > 0)
            {
                currentTask = _resumeTaskStack[_resumeTaskStack.Count - 1];
                _resumeTaskStack.RemoveAt(_resumeTaskStack.Count - 1);
            }
            else
                currentTask = task.parent;
            return task;
        }
        CardEngine game { get; }
        #endregion
    }
    public class SyncTask
    {
        /// <summary>
        /// ID
        /// </summary>
        public int id => _id;
        public int _id = 0;

        /// <summary>
        /// 状态
        /// </summary>
        public SyncTaskState state
        {
            get { return _state; }
            set { _state = value; }
        }
        public SyncTaskState _state = SyncTaskState.running;
        /// <summary>
        /// 父任务
        /// </summary>
        public SyncTask parent => _parent;
        public SyncTask _parent = null;
        /// <summary>
        /// 子任务列表
        /// </summary>
        public List<SyncTask> _childList = new List<SyncTask>();
        public int curActionIndex
        {
            get { return _curActionIndex; }
            set { _curActionIndex = value; }
        }
        public int _curActionIndex = 0;
        public ActionCollection actions => _actions;
        public ActionCollection _actions;
        public EventContext context => _context;
        public EventContext _context = null;
        public SyncTask(int id, EventContext context, ActionCollection actions)
        {
            _id = id;
            _context = context;
            _actions = actions;
        }
        public SyncTask(int id, ActionCollection actions) : this(id, null, actions)
        {
        }
        public void addChild(SyncTask task)
        {
            _childList.Add(task);
            task._parent = this;
        }
        public SyncTask[] getChildren()
        {
            return _childList.ToArray();
        }
    }
    public enum SyncTaskState
    {
        running,
        paused,
        finished
    }
    public class ActionCollection : IEnumerable<SyncAction>
    {
        public List<SyncAction> _actionList = new List<SyncAction>();
        public int length => _actionList.Count;
        public ActionCollection()
        {
        }
        public ActionCollection(IEnumerable<Action<CardEngine>> actions)
        {
            _actionList.AddRange(actions.Select(sa => new CAction(sa)));
        }
        [SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
        public void Add(SyncAction action)
        {
            _actionList.Add(action);
        }
        public void Add(ActionCollection actions)
        {
            _actionList.AddRange(actions);
        }
        public SyncAction this[int index] => _actionList[index];
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
        public virtual void execute(CardEngine game)
        {
        }
    }
    public class CAction : SyncAction
    {
        Action<CardEngine> _action;
        public CAction(Action<CardEngine> action)
        {
            _action = action;
        }
        public override void execute(CardEngine game)
        {
            _action?.Invoke(game);
        }
    }
}