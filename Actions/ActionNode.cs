using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    /// <summary>
    /// 单个动作的数据结构。
    /// 由于要方便编辑器统一进行操作更改和存储，这个数据结构不允许多态。
    /// 这个数据结构必须同时支持多种类型的语句，比如赋值，分支，循环，返回，方法调用。
    /// </summary>
    public sealed class ActionNode : IActionNode
    {
        #region 公有方法
        #region 构造方法
        public ActionNode(int id, string defineName)
        {
            this.id = id;
            this.defineName = defineName;
            inputPorts = new List<IPort>();
            outputPorts = new List<IPort>();
            consts = new Dictionary<string, object>();
        }
        #endregion
        public void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<IActionNode>();
            else if (traversedActionNodeSet.Contains(this))
                return;
            traversedActionNodeSet.Add(this);
            action(this);
            //遍历输入
            if (inputPorts != null)
            {
                foreach (var port in inputPorts)
                {
                    if (port == null)
                        continue;
                    port.traverse(action, traversedActionNodeSet);
                }
            }
            //遍历常量
            if (consts != null)
            {
                foreach (var cst in consts.Values)
                {
                    if (cst == null)
                        continue;
                    if (cst is ITraversable traversable)
                    {
                        traversable.traverse(action, traversedActionNodeSet);
                    }
                }
            }
            //遍历后续
            if (outputPorts != null)
            {
                foreach (var port in outputPorts)
                {
                    if (port == null)
                        continue;
                    port.traverse(action, traversedActionNodeSet);
                }
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var valueConst = this.getConst("value");
            if (defineName == "BooleanConst")
                sb.Append(valueConst is bool b ? b : false);
            else if (defineName == "IntegerConst")
                sb.Append(valueConst is int i ? i : 0);
            else if (defineName == "StringConst")
                sb.Append(valueConst is string s ? s : string.Empty);
            else
            {
                if (defineName == "Compare")
                {
                    sb.Append(inputPorts.Count > 0 && inputPorts[0] != null ? inputPorts[0].ToString() : "null");
                    if (this.getConst("operator") is CompareOperator compareOperator && compareOperator == CompareOperator.equals)
                        sb.Append(" == ");
                    else
                        sb.Append(" != ");
                    sb.Append(inputPorts.Count > 1 && inputPorts[1] != null ? inputPorts[1].ToString() : "null");
                }
                else if (defineName == "LogicOperation")
                {
                    if (this.getConst("operator") is LogicOperator logicOperator)
                    {
                        if (logicOperator == LogicOperator.not)
                            sb.Append("!" + (inputPorts.Count > 0 && inputPorts[0] != null ? inputPorts[0].ToString() : "null"));
                        else if (logicOperator == LogicOperator.and)
                        {
                            for (int i = 0; i < inputPorts.Count; i++)
                            {
                                if (i != 0)
                                    sb.Append(" && ");
                                sb.Append(inputPorts[i].ToString());
                            }
                        }
                        else if (logicOperator == LogicOperator.or)
                        {
                            for (int i = 0; i < inputPorts.Count; i++)
                            {
                                if (i != 0)
                                    sb.Append(" || ");
                                sb.Append(inputPorts[i].ToString());
                            }
                        }
                    }
                }
                else
                {
                    sb.Append(defineName);
                    if (consts != null && consts.Count > 0)
                    {
                        sb.Append('<');
                        for (int i = 0; i < consts.Count; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(',');
                            }
                            var cst = consts.ElementAt(i).Value;
                            sb.Append(cst != null ? cst.ToString() : "null");
                        }
                        sb.Append('>');
                    }
                    sb.Append('(');
                    if (inputPorts != null && inputPorts.Count > 0)
                    {
                        for (int i = 0; i < inputPorts.Count; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(',');
                            }
                            sb.Append(inputPorts[i] != null ? inputPorts[i].ToString() : "null");
                        }
                    }
                    sb.Append("); ");
                }
            }
            return string.Intern(sb.ToString());
        }
        public async Task<ControlOutput> run(Flow flow)
        {
            var define = flow.env.game.getActionDefine(defineName);
            if (define != null)
            {
                return await define.run(flow, this);
            }
            return null;
        }
        public void Define()
        {
            DefinitionInputs(define);
            DefinitionOutputs(define);
        }
        public void setConst(string name, object value)
        {
            if (!consts.ContainsKey(name))
            {
                consts.Add(name, value);
            }
            else
            {
                consts[name] = value;
            }
        }
        public ControlInput getEnterPort()
        {
            return this.getInputPort<ControlInput>(enterControlName);
        }
        public ControlOutput getExitPort()
        {
            return this.getOutputPort<ControlOutput>(exitControlName);
        }
        public ValueInput extendParamsPort(string name)
        {
            var ports = this.getParamInputPorts(name);
            var portDefine = ports.Select(p => p.define).FirstOrDefault();
            var count = ports.Count();
            count++;
            var valueInput = new ValueInput(this, portDefine, count);
            inputPorts.Add(valueInput);
            return valueInput;
        }
        ISerializableNode IActionNode.ToSerializableNode()
        {
            return new SerializableActionNode(this);
        }
        #endregion
        private void DefinitionInputs(ActionDefine define)
        {
            ValueInput valueInput(PortDefine def, int paramIndex)
                => inputPorts.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(def) && d.paramIndex == paramIndex) ?? new ValueInput(this, def, paramIndex);
            ControlInput controlInput(PortDefine def)
                => inputPorts.OfType<ControlInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlInput(this, def);

            List<IPort> inputs = new List<IPort>();
            if (define.inputDefines != null)
            {
                foreach (var portDefine in define.inputDefines)
                {
                    if (portDefine.GetPortType() == PortType.Control)
                    {
                        inputs.Add(controlInput(portDefine));
                    }
                    else
                    {
                        if (define.isParams && portDefine == define.inputDefines.Last())
                        {
                            // 变长参数。
                            var count = 0;
                            var connectedInputs = this.getParamInputPorts(portDefine.name).Where(p => p.connections.Count() > 0);
                            if (connectedInputs.Count() > 0)
                            {
                                count = connectedInputs.Max(p => p.paramIndex);
                            }
                            count = Math.Max(count, -1) + 2;
                            for (int i = 0; i < count; i++)
                            {
                                inputs.Add(valueInput(portDefine, i));
                            }
                        }
                        else
                        {
                            inputs.Add(valueInput(portDefine, -1));
                        }
                    }
                }
            }
            foreach (var lostPort in inputPorts.Except(inputs))
            {
                graph.disconnectAll(lostPort);
            }
            inputPorts.Clear();
            inputPorts.AddRange(inputs);
        }
        private void DefinitionOutputs(ActionDefine define)
        {
            ValueOutput valueOutput(PortDefine def)
                => outputPorts.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def, async flow =>
                {
                    await define.run(flow, this);
                    var port = this.getOutputPort<ValueOutput>(def.name);
                    return flow.currentScope.getLocalVar(port);
                });
            ControlOutput controlOutput(PortDefine def)
                => outputPorts.OfType<ControlOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlOutput(this, def);

            List<IPort> outputs = new List<IPort>();
            if (define.outputDefines != null)
            {
                foreach (var portDefine in define.outputDefines)
                {
                    if (portDefine.GetPortType() == PortType.Control)
                        outputs.Add(controlOutput(portDefine));
                    else
                        outputs.Add(valueOutput(portDefine));
                }
            }
            foreach (var lostPort in outputPorts.Except(outputs))
            {
                graph.disconnectAll(lostPort);
            }
            outputPorts.Clear();
            outputPorts.AddRange(outputs);
        }

        /// <summary>
        /// 用来区分不同动作节点的ID。
        /// </summary>
        /// <remarks>其实这个ID在逻辑上并没有什么特殊的作用，但是编辑器需要一个ID来保存对应的视图信息。</remarks>
        public int id;
        public string defineName;
        int IActionNode.id => id;
        IEnumerable<IPort> IActionNode.outputPorts => outputPorts;
        IEnumerable<IPort> IActionNode.inputPorts => inputPorts;
        IDictionary<string, object> IActionNode.consts => consts;

        public float posX { get; set; }
        public float posY { get; set; }
        /// <summary>
        /// 该动作的输出端口。
        /// </summary>
        public List<IPort> outputPorts;
        /// <summary>
        /// 该动作的输入端口。
        /// </summary>
        public List<IPort> inputPorts;
        /// <summary>
        /// 该动作的常量列表。
        /// </summary>
        public Dictionary<string, object> consts;
        public ActionDefine define { get; set; }
        public ActionGraph graph { get; set; }

        public const string enterControlName = "enter";
        public const string exitControlName = "exit";
    }

    [Serializable]
    public sealed class SerializableActionNode : ISerializableNode
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionNode(ActionNode actionNode)
        {
            if (actionNode == null)
                throw new ArgumentNullException(nameof(actionNode));
            id = actionNode.id;
            defineName = actionNode.defineName;
            posX = actionNode.posX;
            posY = actionNode.posY;
            constDict = actionNode.consts;
        }
        #endregion

        public ActionNode ToActionNode()
        {
            var node = new ActionNode(id, defineName)
            {
                posX = posX,
                posY = posY,
            };
            node.consts = constDict;
            return node;
        }
        IActionNode ISerializableNode.ToActionNode() => ToActionNode();
        #endregion
        #region 属性字段
        public int id;
        public string defineName;
        public float posX;
        public float posY;
        public Dictionary<string, object> constDict;

        [Obsolete]
        public int[] branches;
        [Obsolete]
        public SerializableActionValueRef[] inputs;
        [Obsolete]
        public bool[] regVar;
        [Obsolete]
        public object[] consts;
        #endregion
    }
}