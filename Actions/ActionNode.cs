using System;
using System.Collections.Generic;
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
        public ActionNode(int id, string defineName, ActionValueRef[] inputs = null, object[] consts = null, bool[] regVar = null, ActionNode[] branches = null)
        {
            this.id = id;
            this.defineName = defineName;
            this.branches = branches != null ? branches : new ActionNode[0];
            this.inputs = inputs != null ? inputs : new ActionValueRef[0];
            this.consts = consts != null ? consts : new object[0];
            this.regVar = regVar != null ? regVar : new bool[0];
        }
        public ActionNode(string defineName, ActionValueRef[] inputs, object[] consts) : this(0, defineName, inputs, consts, new bool[0], new ActionNode[0])
        {
        }
        public ActionNode(string defineName, ActionValueRef[] inputs, ActionNode next) : this(0, defineName, inputs, new object[0], new bool[0], new ActionNode[] { next })
        {
        }
        public ActionNode(string defineName, ActionValueRef[] inputs) : this(0, defineName, inputs, new object[0], new bool[0], new ActionNode[0])
        {
        }
        public ActionNode(string defineName, object[] consts) : this(0, defineName, new ActionValueRef[0], consts, new bool[0], new ActionNode[0])
        {
        }
        public ActionNode(string defineName) : this(0, defineName, new ActionValueRef[0], new object[0], new bool[0], new ActionNode[0])
        {
        }
        public ActionNode(int id) : this(id, null, new ActionValueRef[0], new object[0], new bool[0], new ActionNode[0])
        {
        }
        public ActionNode() : this(0, string.Empty, new ActionValueRef[0], new object[0], new bool[0], new ActionNode[0])
        {
        }
        #endregion
        public void traverse(Action<ActionNode> action, HashSet<ActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<ActionNode>();
            else if (traversedActionNodeSet.Contains(this))
                return;
            traversedActionNodeSet.Add(this);
            action(this);
            //遍历输入
            if (inputs != null && inputs.Length > 0)
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i] == null)
                        continue;
                    inputs[i].traverse(action, traversedActionNodeSet);
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
                        childActionNode.traverse(action, traversedActionNodeSet);
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
            if (branches != null && branches.Length > 0)
            {
                for (int i = 0; i < branches.Length; i++)
                {
                    if (branches[i] == null)
                        continue;
                    branches[i].traverse(action, traversedActionNodeSet);
                }
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (defineName == "BooleanConst")
                sb.Append(consts != null && consts.Length > 0 && consts[0] is bool b ? b : false);
            else if (defineName == "IntegerConst")
                sb.Append(consts != null && consts.Length > 0 && consts[0] is int i ? i : 0);
            else if (defineName == "StringConst")
                sb.Append(consts != null && consts.Length > 0 && consts[0] is string s ? s : string.Empty);
            else
            {
                if (defineName == "Compare")
                {
                    sb.Append(inputs.Length > 0 && inputs[0] != null ? inputs[0].ToString() : "null");
                    if (consts != null && consts.Length > 0 && consts[0] is CompareOperator compareOperator && compareOperator == CompareOperator.equals)
                        sb.Append(" == ");
                    else
                        sb.Append(" != ");
                    sb.Append(inputs.Length > 1 && inputs[1] != null ? inputs[1].ToString() : "null");
                }
                else if (defineName == "LogicOperation")
                {
                    if (consts != null && consts.Length > 0 && consts[0] is LogicOperator logicOperator)
                    {
                        if (logicOperator == LogicOperator.not)
                            sb.Append("!" + (inputs.Length > 0 && inputs[0] != null ? inputs[0].ToString() : "null"));
                        else if (logicOperator == LogicOperator.and)
                        {
                            for (int i = 0; i < inputs.Length; i++)
                            {
                                if (i != 0)
                                    sb.Append(" && ");
                                sb.Append(inputs[i].ToString());
                            }
                        }
                        else if (logicOperator == LogicOperator.or)
                        {
                            for (int i = 0; i < inputs.Length; i++)
                            {
                                if (i != 0)
                                    sb.Append(" || ");
                                sb.Append(inputs[i].ToString());
                            }
                        }
                    }
                }
                else
                {
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
                }
            }
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
    [Serializable]
    public sealed class SerializableActionNode
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionNode(ActionNode actionNode)
        {
            if (actionNode == null)
                throw new ArgumentNullException(nameof(actionNode));
            id = actionNode.id;
            defineName = actionNode.defineName;
            branches = actionNode.branches != null ?
                Array.ConvertAll(actionNode.branches, a => a != null ? a.id : 0) :
                new int[0];
            inputs = actionNode.inputs != null ?
                Array.ConvertAll(actionNode.inputs, i => i != null ? new SerializableActionValueRef(i) : null) :
                new SerializableActionValueRef[0];
            consts = actionNode.consts != null ? actionNode.consts : new object[0];
            regVar = actionNode.regVar != null ? actionNode.regVar : new bool[0];
        }
        #endregion
        public static ActionNode toActionNodeGraph(int actionNodeId, List<SerializableActionNode> actionNodeList, Dictionary<int, ActionNode> actionNodeDict = null)
        {
            if (actionNodeId == 0)
                return null;
            if (actionNodeList == null)
                return null;
            if (actionNodeDict == null)
                actionNodeDict = new Dictionary<int, ActionNode>();
            SerializableActionNode seriActionNode = actionNodeList.Find(s => s.id == actionNodeId);
            if (seriActionNode == null)
                throw new KeyNotFoundException("不存在id为" + actionNodeId + "的动作节点");
            ActionNode actionNode = new ActionNode(actionNodeId);
            actionNodeDict.Add(actionNodeId, actionNode);
            actionNode.defineName = seriActionNode.defineName;
            actionNode.consts = seriActionNode.consts;
            actionNode.regVar = seriActionNode.regVar;
            //inputs
            actionNode.inputs = new ActionValueRef[seriActionNode.inputs.Length];
            for (int i = 0; i < actionNode.inputs.Length; i++)
            {
                if (seriActionNode.inputs[i] != null)
                    actionNode.inputs[i] = seriActionNode.inputs[i].toActionValueRef(actionNodeList, actionNodeDict);
                else
                    actionNode.inputs[i] = null;
            }
            //branches
            actionNode.branches = new ActionNode[seriActionNode.branches.Length];
            for (int i = 0; i < actionNode.branches.Length; i++)
            {
                if (seriActionNode.branches[i] == 0)
                    actionNode.branches[i] = null;
                else if (actionNodeDict.TryGetValue(seriActionNode.branches[i], out ActionNode childNode))
                    actionNode.branches[i] = childNode;
                else
                    actionNode.branches[i] = toActionNodeGraph(seriActionNode.branches[i], actionNodeList, actionNodeDict);
            }
            return actionNode;
        }
        #endregion
        #region 属性字段
        public int id;
        public string defineName;
        public int[] branches;
        public SerializableActionValueRef[] inputs;
        public object[] consts;
        public bool[] regVar;
        #endregion
    }
    [Serializable]
    public sealed class SerializableActionNodeGraph
    {
        #region 公有方法
        public SerializableActionNodeGraph(ActionNode actionNode)
        {
            if (actionNode == null)
                throw new ArgumentNullException(nameof(actionNode));
            rootActionId = actionNode.id;
            actionNode.traverse(a =>
            {
                if (a != null)
                    actionNodeList.Add(new SerializableActionNode(a));
            });
        }
        public ActionNode toActionNodeGraph(Dictionary<int, ActionNode> actionNodeDict = null)
        {
            return SerializableActionNode.toActionNodeGraph(rootActionId, actionNodeList, actionNodeDict);
        }
        #endregion
        #region 属性字段
        public int rootActionId;
        public List<SerializableActionNode> actionNodeList = new List<SerializableActionNode>();
        #endregion
    }
}