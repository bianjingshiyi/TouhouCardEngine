using System;
using System.Linq;
using System.Text;
namespace TouhouCardEngine
{
    /// <summary>
    /// 单个动作的数据结构。
    /// 由于要方便编辑器统一进行操作更改和存储，这个数据结构不允许多态。
    /// 这个数据结构必须同时支持多种类型的语句，比如赋值，分支，循环，返回，方法调用。
    /// </summary>
    [Serializable]
    public sealed class ActionNode
    {
        #region 公有方法
        #region 构造方法
        public ActionNode(string defineName, ActionValueRef[] inputs, object[] consts, ActionNode[] branches)
        {
            this.defineName = defineName;
            this.branches = branches;
            this.inputs = inputs;
            this.consts = consts;
        }
        public ActionNode(string defineName, ActionValueRef[] inputs, object[] consts) : this(defineName, inputs, consts, new ActionNode[0])
        {
        }
        public ActionNode(string defineName, ActionValueRef[] inputs, ActionNode next) : this(defineName, inputs, new object[0], new ActionNode[] { next })
        {
        }
        public ActionNode(string defineName, ActionValueRef[] inputs) : this(defineName, inputs, new object[0], new ActionNode[0])
        {
        }
        public ActionNode(string defineName, params object[] consts) : this(defineName, new ActionValueRef[0], consts, new ActionNode[0])
        {
        }
        public ActionNode(string defineName) : this(defineName, new ActionValueRef[0], new object[0], new ActionNode[0])
        {
        }
        public ActionNode() : this(string.Empty, new ActionValueRef[0], new object[0], new ActionNode[0])
        {
        }
        #endregion
        public void traverse(Action<ActionNode> action)
        {
            if (action == null)
                return;
            action(this);
            //遍历输入
            if (inputs != null && inputs.Length > 0)
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i] == null)
                        continue;
                    inputs[i].traverse(action);
                }
            }
            //遍历常量
            if (consts != null && consts.Length > 0)
            {
                for (int i = 0; i < consts.Length; i++)
                {
                    if (consts[i] == null)
                        continue;
                    if (consts[i] is ActionNode childActionNode)
                    {
                        childActionNode.traverse(action);
                    }
                    else if (consts[i] is ActionValueRef valueRef)
                    {
                        valueRef.traverse(action);
                    }
                    else if (consts[i] is TargetChecker targetChecker)
                    {
                        targetChecker.traverse(action);
                    }
                    else if (consts[i] is TriggerGraph trigger)
                    {
                        trigger.traverse(action);
                    }
                }
            }
            //遍历后续
            if (branches != null && branches.Length > 0)
            {
                for (int i = 0; i < branches.Length; i++)
                {
                    if (branches[i] == null)
                        continue;
                    branches[i].traverse(action);
                }
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(defineName);
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
            if (inputs != null && inputs.Length > 0)
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(inputs[i] != null ? inputs[i].ToString() : "null");
                }
            }
            sb.Append("); ");
            return string.Intern(sb.ToString());
        }
        #endregion
        /// <summary>
        /// 用来区分不同动作节点的ID。
        /// </summary>
        /// <remarks>其实这个ID在逻辑上并没有什么特殊的作用，但是编辑器需要一个ID来保存对应的视图信息。</remarks>
        public int id;
        public string defineName;
        /// <summary>
        /// 该动作的后续动作，根据动作的类型不同可能有多个动作分支，比如条件结构，或者循环结构
        /// </summary>
        public ActionNode[] branches;
        /// <summary>
        /// 该动作引用的输入值
        /// </summary>
        public ActionValueRef[] inputs;
        public object[] consts;
        /// <summary>
        /// 用于标识返回值是否需要保存为局部变量
        /// </summary>
        public bool[] regVar;
    }
}