using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using System.Runtime.InteropServices.ComTypes;

namespace TouhouCardEngine
{
    /// <summary>
    /// 同步触发系统
    /// </summary>
    public partial class SyncTriggerSystem
    {
        #region 公开成员
        public SyncTask doTask(EventContext eventContext, ActionCollection actions)
        {
            SyncTask task = new SyncTask(++_lastTaskId, actions)
            {
                _context = eventContext
            };
            if (currentTask != null && currentTask != task)
                currentTask.addChild(task);
            currentTask = task;
            todoTask(task);
            if (task.state == SyncTaskState.finished)
                currentTask = task.parent;
            else if (task.state == SyncTaskState.paused)
                currentTask = null;
            else
                throw new TaskStillRunningException(task);
            return task;
        }
        /// <summary>
        /// 暂停一个Task
        /// </summary>
        /// <param name="task"></param>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        public void pauseTask(SyncTask task)
        {
            if (task == null)
                throw new ArgumentNullException();
            _pausedTaskList.Add(task);
            for (; task != null; task = task.parent)
                task.state = SyncTaskState.paused;
            currentTask = null;
        }
        public SyncTask[] getPausedTasks()
        {
            return _pausedTaskList.ToArray();
        }
        /// <summary>
        /// 恢复一个被暂停的Task
        /// </summary>
        /// <param name="task">被暂停的Task，不能为空，必须处于被暂停的状态</param>
        /// <returns>被恢复的Task</returns>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当参数不是一个被暂停的任务时抛出</exception>
        public SyncTask resumeTask(SyncTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            if (_pausedTaskList.Remove(task))
            {
                SyncTask sourceTask = currentTask;
                toResumeTask(task);
                if (task.state == SyncTaskState.finished ||
                    task.state == SyncTaskState.paused)
                    currentTask = sourceTask;
                else if (task.state == SyncTaskState.running)
                    throw new TaskStillRunningException(task);
                return task;
            }
            throw new InvalidOperationException(task + "不是一个被暂停的Task");
        }
        public void stopTask(SyncTask task)
        {
            if (task == null)
                throw new ArgumentNullException();
            _pausedTaskList.Remove(task);
            for (; task != null; task = task.parent)
                task.state = SyncTaskState.finished;
            currentTask = null;
        }
        public SyncTask currentTask
        {
            get { return _currentTask; }
            private set { _currentTask = value; }
        }
        public SyncTask _currentTask;
        public List<SyncTask> _pausedTaskList = new List<SyncTask>();
        //public List<SyncTask> _resumeTaskStack = new List<SyncTask>();
        public int _lastTaskId = 0;
        public int maxActionCount
        {
            get => _maxActionCount;
            set => _maxActionCount = value;
        }
        public int _maxActionCount = 1000;
        #endregion
        #region 私有成员
        /// <summary>
        /// 这个方法只负责根据给出的Task的状态继续执行它，不论它是刚刚开始执行，
        /// 还是从什么状态下恢复过来的。所以传入的Task的状态至关重要。
        /// </summary>
        /// <param name="task"></param>
        void todoTask(SyncTask task)
        {
            int count = 0;
            for (; task.curActionIndex < task.actions.length; task.curActionIndex += 1)
            {
                count++;
                if (count > maxActionCount)
                    throw new StackOverflowException("单次todoTask执行动作超过" + maxActionCount);
                if (task.state == SyncTaskState.paused ||
                    task.state == SyncTaskState.finished)
                    return;
                task.actions[task.curActionIndex].execute(game);
            }
            if (task.state == SyncTaskState.running)
                task.state = SyncTaskState.finished;
        }
        void toResumeTask(SyncTask task)
        {
            currentTask = task;
            task.state = SyncTaskState.running;
            todoTask(task);
            if (task.state == SyncTaskState.finished)
            {
                if (task.parent != null && task.parent.state == SyncTaskState.paused)
                    toResumeTask(task.parent);
            }
            else if (task.state == SyncTaskState.running)
                throw new TaskStillRunningException(task);
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
        public override string ToString()
        {
            string s = "Task" + id;
            if (context != null && !string.IsNullOrEmpty(context.name))
                s += "[" + context.name + "]";
            return s;
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
            _actionList.AddRange(actions.Select(sa => new ALambda(sa)));
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
        public ActionCollection append(IEnumerable<SyncAction> actions)
        {
            _actionList.AddRange(actions);
            return this;
        }
        public ActionCollection append(params SyncAction[] actions)
        {
            return append(actions as IEnumerable<SyncAction>);
        }
        public SyncAction this[int index] => _actionList[index];
        public int indexOf(SyncAction action)
        {
            return _actionList.IndexOf(action);
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
    public abstract class SyncAction
    {
        public abstract void execute(CardEngine game);
    }
    public class ALambda : SyncAction
    {
        Action<CardEngine> _action;
        public ALambda(Action<CardEngine> action)
        {
            _action = action;
        }
        public override void execute(CardEngine game)
        {
            _action?.Invoke(game);
        }
    }
    [Serializable]
    public class TaskStillRunningException : Exception
    {
        public TaskStillRunningException() { }
        public TaskStillRunningException(SyncTask task) : base(task + "的状态仍为running") { }
        public TaskStillRunningException(string message) : base(message) { }
        public TaskStillRunningException(string message, Exception inner) : base(message, inner) { }
        protected TaskStillRunningException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}