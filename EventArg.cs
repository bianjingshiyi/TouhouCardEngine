using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public abstract class EventArg : IEventArg
    {
        public IEventArg[] getChildEvents()
        {
            return childEventList.ToArray();
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
        public void setVar(string varName, object value)
        {
            varDict[varName] = value;
        }
        /// <summary>
        /// 获取事件发生时的参数信息。
        /// </summary>
        /// <returns></returns>
        public virtual EventVariableInfo[] getBeforeEventVarInfos()
        {
            return null;
        }
        /// <summary>
        /// 获取事件发生后的参数信息。
        /// </summary>
        /// <returns></returns>
        public virtual EventVariableInfo[] getAfterEventVarInfos()
        {
            return null;
        }
        public abstract void Record(IGame game, EventRecord record);
        public string[] beforeNames { get; set; }
        public string[] afterNames { get; set; }
        public object[] args { get; set; }
        public int flowNodeId { get; set; }
        public bool isCanceled { get; set; }
        public bool isCompleted { get; set; }
        public EventRecord record { get; set; }
        public int repeatTime { get; set; }
        public Func<IEventArg, Task> action { get; set; }
        public List<IEventArg> childEventList { get; } = new List<IEventArg>();
        public IEventArg parent
        {
            get => _parent;
            set
            {
                _parent = value;
                if (value is EventArg ea)
                    ea.childEventList.Add(this);
            }
        }
        IEventArg _parent;
        Dictionary<string, object> varDict { get; } = new Dictionary<string, object>();
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
            this.childrenTypes = types;
        }
        public Type[] childrenTypes;
    }
}