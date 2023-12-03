using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Histories;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EventArg : IEventArg
    {
        public EventArg(CardEngine game, EventDefine define)
        {
            this.game = game;
            this.define = define;
        }
        public IEventArg[] getAllChildEvents()
        {
            return beforeChildrenEvents.Concat(logicChildrenEvents).Concat(afterChildrenEvents).ToArray();
        }
        public IEventArg[] getChildEvents(EventState state)
        {
            switch (state)
            {
                case EventState.Before:
                    return beforeChildrenEvents.ToArray();
                case EventState.Logic:
                    return logicChildrenEvents.ToArray();
                case EventState.After:
                    return afterChildrenEvents.ToArray();
            }
            return null;
        }
        public object getVar(string varName)
        {
            if (varDict.TryGetValue(varName, out object value))
                return value;
            else
                return null;
        }
        public T getVar<T>(string varName)
        {
            if (getVar(varName) is T t)
                return t;
            else
                return default;
        }
        public string[] getVarNames()
        {
            return varDict.Keys.ToArray();
        }
        public void setVar(string varName, object value)
        {
            if (isCompleted)
                return;
            varDict[varName] = value;
        }
        /// <summary>
        /// 获取事件发生时的参数信息。
        /// </summary>
        /// <returns></returns>
        public EventVariableInfo[] getBeforeEventVarInfos()
        {
            return define.beforeVariableInfos;
        }
        /// <summary>
        /// 获取事件发生后的参数信息。
        /// </summary>
        /// <returns></returns>
        public EventVariableInfo[] getAfterEventVarInfos()
        {
            return define.afterVariableInfos;
        }
        public Task execute()
        {
            return define.execute(this);
        }
        public void setParent(IEventArg parent)
        {
            this.parent = parent;
            if (parent is EventArg par)
            {
                switch (par.state)
                {
                    case EventState.Before:
                        par.beforeChildrenEvents.Add(this);
                        break;
                    case EventState.Logic:
                        par.logicChildrenEvents.Add(this);
                        break;
                    case EventState.After:
                        par.afterChildrenEvents.Add(this);
                        break;
                    default:
                        throw new InvalidOperationException($"无法为状态为{par.state}的事件添加子事件。");
                }
            }
        }
        [Obsolete]
        public void Record(CardEngine game, EventRecord record)
        {
            define.Record(game, this, record);
        }
        public void addChange(Change change)
        {
            _changes.Add(change);
        }
        public Change[] getChanges()
        {
            return _changes.ToArray();
        }
        public override string ToString()
        {
            if (define == null)
                return "未知事件";
            return define.toString(this);
        }
        public string[] beforeNames { get; set; }
        public string[] afterNames { get; set; }
        public object[] args { get; set; }
        public int flowNodeId { get; set; }
        public bool isCanceled { get; set; }
        public bool isCompleted { get; set; }
        [Obsolete]
        public EventRecord record { get; set; }
        public int repeatTime { get; set; }
        public Func<IEventArg, Task> action { get; set; }
        public List<IEventArg> beforeChildrenEvents { get; } = new List<IEventArg>();
        public List<IEventArg> logicChildrenEvents { get; } = new List<IEventArg>();
        public List<IEventArg> afterChildrenEvents { get; } = new List<IEventArg>();
        public IEventArg parent { get; private set; }
        public EventState state { get; set; }
        private Dictionary<string, object> varDict { get; } = new Dictionary<string, object>();
        private List<Change> _changes = new List<Change>();
        public EventDefine define { get; set; }
        public CardEngine game { get; set; }
    }
    public class EventVariableInfo
    {
        public string name;
        public Type type;
    }
    public class EventChildrenAttribute : Attribute
    {
        public EventChildrenAttribute(params Type[] types)
        {
            childrenTypes = types;
        }
        public Type[] childrenTypes;
    }
    public enum EventState
    {
        None,
        Before,
        Logic,
        After,
        Completed,
        Canceled
    }
}