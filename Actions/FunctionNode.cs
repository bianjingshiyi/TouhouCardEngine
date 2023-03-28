using System;
using System.Collections.Generic;
using System.Text;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Obsolete]
    public sealed class FunctionNode
    {
        #region 公有方法
        #region 构造方法
        public FunctionNode(string functionName, ActionNode action = null, ReturnValueRef[] returns = null, object[] consts = null)
        {
            this.functionName = functionName;
            this.action = action;
            this.returns = returns ?? new ReturnValueRef[0];
            this.consts = consts ?? new object[0];
        }
        public FunctionNode() : this(null, null, null, null)
        {
        }
        #endregion
        public void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<Node>();
            //遍历输入
            if (returns != null && returns.Length > 0)
            {
                for (int i = 0; i < returns.Length; i++)
                {
                    if (returns[i] == null)
                        continue;
                    returns[i].valueRef.traverse(action, traversedActionNodeSet);
                }
            }
            //遍历常量
            if (consts != null && consts.Length > 0)
            {
                for (int i = 0; i < consts.Length; i++)
                {
                    if (consts[i] == null)
                        continue;
                    if (consts[i] is FunctionNode childActionConditionNode)
                    {
                        childActionConditionNode.traverse(action, traversedActionNodeSet);
                    }
                    else if (consts[i] is ActionValueRef valueRef)
                    {
                        valueRef.traverse(action, traversedActionNodeSet);
                    }
                    else if (consts[i] is TargetChecker targetChecker)
                    {
                        targetChecker.traverse(action, traversedActionNodeSet);
                    }
                    else if (consts[i] is TriggerGraph trigger)
                    {
                        trigger.traverse(action, traversedActionNodeSet);
                    }
                }
            }
            //遍历后续
            if (this.action != null)
                this.action.traverse(action, traversedActionNodeSet);
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (consts != null && consts.Length > 0)
            {
                sb.Append('<');
                for (int i = 0; i < consts.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(consts[i] != null ? consts[i].ToString() : "null");
                }
                sb.Append('>');
            }
            sb.Append('(');
            if (returns != null && returns.Length > 0)
            {
                for (int i = 0; i < returns.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(returns[i] != null ? returns[i].ToString() : "null");
                }
            }
            sb.Append("); ");
            return string.Intern(sb.ToString());
        }
        #endregion
        public string functionName;
        public ActionNode action;
        /// <summary>
        /// 该动作引用的返回值
        /// </summary>
        public ReturnValueRef[] returns;
        public object[] consts;
    }
    [Serializable]
    public sealed class SerializableFunctionNode
    {
        #region 属性字段
        public int actionNodeId;
        public SerializableReturnValueRef[] returns;
        public string functionName;
        public object[] consts;
        public List<SerializableActionNode> actionNodeList;
        #endregion
    }

    public class FunctionDefine
    {
        public ValueDefine[] parameters;
        public ValueDefine[] consts;
        public ValueDefine[] returns;
    }
}