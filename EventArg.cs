using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class EventArg : IEventArg
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
        #region 动作定义
        [ActionNodeMethod("GetVariable", "Event")]
        [return: ActionNodeParam("Value")]
        public static object getVariable(EventArg eventArg, [ActionNodeParam("VariableName", true)] string varName)
        {
            return eventArg.getVar(varName);
        }
        [ActionNodeMethod("SetVariable", "Event")]
        public static void setVariable(EventArg eventArg, [ActionNodeParam("VariableName")] string varName, [ActionNodeParam("Value")] object value)
        {
            eventArg.setVar(varName, value);
        }
        #endregion
        public IGame game;
        public string[] beforeNames { get; set; }
        public string[] afterNames { get; set; }
        public object[] args { get; set; }
        public bool isCanceled { get; set; }
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
    public class EventTypeInfo
    {
        #region 属性字段
        public string eventName;
        public string editorName;
        public EventVariableInfo[] variableInfos;
        public string[] obsoleteNames;
        #endregion
    }

}