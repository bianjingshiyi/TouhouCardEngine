using System;
using System.Collections.Generic;

namespace TouhouCardEngine
{
    [Serializable]
    public class ActionValueRef
    {
        #region 公有方法
        #region 构造方法
        /// <summary>
        /// 返回指定索引返回值的构造器
        /// </summary>
        /// <param name="action"></param>
        /// <param name="index"></param>
        public ActionValueRef(ActionNode action, int index)
        {
            this.action = action;
            this.index = index;
        }
        public ActionValueRef(string eventVarName)
        {
            this.eventVarName = eventVarName;
        }
        public ActionValueRef(int argIndex)
        {
            this.argIndex = argIndex;
        }
        /// <summary>
        /// 返回第一个返回值的构造器
        /// </summary>
        /// <param name="action"></param>
        public ActionValueRef(ActionNode action) : this(action, 0)
        {
        }
        /// <summary>
        /// 供序列化使用的默认构造器
        /// </summary>
        public ActionValueRef() : this(null, 0)
        {
        }
        #endregion
        public void traverse(Action<ActionNode> action, HashSet<ActionNode> traversedActionNodeSet = null)
        {
            if (this.action == null)
                return;
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<ActionNode>();
            else if (traversedActionNodeSet.Contains(this.action))
                return;
            this.action.traverse(action, traversedActionNodeSet);
        }
        public override string ToString()
        {
            string s;
            if (action != null)
            {
                s = action.ToString() + "[" + index + "]";
            }
            else if (actionNodeId != 0)
            {
                s = "{" + actionNodeId + "}[" + index + "]";
            }
            else if (!string.IsNullOrEmpty(eventVarName))
            {
                s = "{" + eventVarName + "}";
            }
            else
            {
                s = "null";
            }
            return string.Intern(s);
        }
        #endregion
        /// <summary>
        /// 引用的动作节点
        /// </summary>
        public ActionNode action;
        /// <summary>
        /// 引用的动作节点返回值索引
        /// </summary>
        public int index;
        /// <summary>
        /// 引用的动作节点ID
        /// </summary>
        public int actionNodeId;
        /// <summary>
        /// 事件变量名称
        /// </summary>
        public string eventVarName;
        /// <summary>
        /// 参数索引
        /// </summary>
        public int argIndex;
    }
    public class SerializableActionValueRef
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionValueRef(ActionValueRef actionValueRef, bool isGraph = false)
        {
            if (actionValueRef == null)
                throw new ArgumentNullException(nameof(actionValueRef));
            actionNodeId = actionValueRef.action != null ? actionValueRef.action.id : actionValueRef.actionNodeId;
            index = actionValueRef.index;
            eventVarName = actionValueRef.eventVarName;
            argIndex = actionValueRef.argIndex;
            if (isGraph)
            {
                actionNodeList = new List<SerializableActionNode>();
                if (actionValueRef.action != null)
                {
                    actionValueRef.action.traverse(a =>
                    {
                        if (a != null)
                            actionNodeList.Add(new SerializableActionNode(a));
                    });
                }
            }
        }
        #endregion
        public ActionValueRef toActionValueRef(List<SerializableActionNode> actionNodeList, Dictionary<int, ActionNode> actionNodeDict)
        {
            if (actionNodeId != 0)
            {
                if (actionNodeDict.TryGetValue(actionNodeId, out ActionNode actionNode))
                    return new ActionValueRef(actionNode, index);
                else
                    return new ActionValueRef(SerializableActionNode.toActionNodeGraph(actionNodeId, actionNodeList, actionNodeDict), index);
            }
            else if (!string.IsNullOrEmpty(eventVarName))
                return new ActionValueRef(eventVarName);
            else
                return new ActionValueRef(argIndex);
        }
        public ActionValueRef toActionValueRef()
        {
            return new ActionValueRef(SerializableActionNode.toActionNodeGraph(actionNodeId, actionNodeList), index);
        }
        #endregion
        #region 属性字段
        public int actionNodeId;
        public int index;
        public string eventVarName;
        public int argIndex;
        public List<SerializableActionNode> actionNodeList;
        #endregion
    }
}