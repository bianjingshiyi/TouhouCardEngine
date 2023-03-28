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
    public sealed class ActionNode : Node, IDefineNode<ActionDefine>
    {
        #region 公有方法
        #region 构造方法
        public ActionNode(int id, string defineName)
        {
            this.id = id;
            this.defineName = defineName;
            inputList = new List<IPort>();
            outputList = new List<IPort>();
            constList = new Dictionary<string, object>();
        }
        #endregion
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var valueConst = getConst("value");
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
                    sb.Append(inputList.Count > 0 && inputList[0] != null ? inputList[0].ToString() : "null");
                    if (getConst("operator") is CompareOperator compareOperator && compareOperator == CompareOperator.equals)
                        sb.Append(" == ");
                    else
                        sb.Append(" != ");
                    sb.Append(inputList.Count > 1 && inputList[1] != null ? inputList[1].ToString() : "null");
                }
                else if (defineName == "LogicOperation")
                {
                    if (getConst("operator") is LogicOperator logicOperator)
                    {
                        if (logicOperator == LogicOperator.not)
                            sb.Append("!" + (inputList.Count > 0 && inputList[0] != null ? inputList[0].ToString() : "null"));
                        else if (logicOperator == LogicOperator.and)
                        {
                            for (int i = 0; i < inputList.Count; i++)
                            {
                                if (i != 0)
                                    sb.Append(" && ");
                                sb.Append(inputList[i].ToString());
                            }
                        }
                        else if (logicOperator == LogicOperator.or)
                        {
                            for (int i = 0; i < inputList.Count; i++)
                            {
                                if (i != 0)
                                    sb.Append(" || ");
                                sb.Append(inputList[i].ToString());
                            }
                        }
                    }
                }
                else
                {
                    sb.Append(defineName);
                    if (constList != null && constList.Count > 0)
                    {
                        sb.Append('<');
                        for (int i = 0; i < constList.Count; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(',');
                            }
                            var cst = constList.ElementAt(i).Value;
                            sb.Append(cst != null ? cst.ToString() : "null");
                        }
                        sb.Append('>');
                    }
                    sb.Append('(');
                    if (inputList != null && inputList.Count > 0)
                    {
                        for (int i = 0; i < inputList.Count; i++)
                        {
                            if (i != 0)
                            {
                                sb.Append(',');
                            }
                            sb.Append(inputList[i] != null ? inputList[i].ToString() : "null");
                        }
                    }
                    sb.Append("); ");
                }
            }
            return string.Intern(sb.ToString());
        }
        public async override Task<ControlOutput> run(Flow flow)
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
            if (!constList.ContainsKey(name))
            {
                constList.Add(name, value);
            }
            else
            {
                constList[name] = value;
            }
        }
        public ControlInput getEnterPort()
        {
            return getInputPort<ControlInput>(enterControlName);
        }
        public ControlOutput getExitPort()
        {
            return getOutputPort<ControlOutput>(exitControlName);
        }
        public ValueInput extendParamsPort(string name)
        {
            var ports = getParamInputPorts(name);
            var portDefine = ports.Select(p => p.define).FirstOrDefault();
            var count = ports.Count();
            count++;
            var valueInput = new ValueInput(this, portDefine, count);
            inputList.Add(valueInput);
            return valueInput;
        }
        public override ISerializableNode ToSerializableNode()
        {
            return new SerializableActionNode(this);
        }
        #endregion
        private void DefinitionInputs(ActionDefine define)
        {
            ValueInput valueInput(PortDefine def, int paramIndex)
                => inputList.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(def) && d.paramIndex == paramIndex) ?? new ValueInput(this, def, paramIndex);
            ControlInput controlInput(PortDefine def)
                => inputList.OfType<ControlInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlInput(this, def);

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
                            var connectedInputs = getParamInputPorts(portDefine.name).Where(p => p.connections.Count() > 0);
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
            foreach (var lostPort in inputList.Except(inputs))
            {
                graph.disconnectAll(lostPort);
            }
            inputList.Clear();
            inputList.AddRange(inputs);
        }
        private void DefinitionOutputs(ActionDefine define)
        {
            ValueOutput valueOutput(PortDefine def)
                => outputList.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def, async flow =>
                {
                    await define.run(flow, this);
                    var port = getOutputPort<ValueOutput>(def.name);
                    return flow.currentScope.getLocalVar(port);
                });
            ControlOutput controlOutput(PortDefine def)
                => outputList.OfType<ControlOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlOutput(this, def);

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
            foreach (var lostPort in outputList.Except(outputs))
            {
                graph.disconnectAll(lostPort);
            }
            outputList.Clear();
            outputList.AddRange(outputs);
        }

        public string defineName;
        public ActionDefine define { get; set; }
        /// <summary>
        /// 该动作的输出端口。
        /// </summary>
        internal List<IPort> outputList;
        /// <summary>
        /// 该动作的输入端口。
        /// </summary>
        internal List<IPort> inputList;
        /// <summary>
        /// 该动作的常量列表。
        /// </summary>
        internal Dictionary<string, object> constList;
        public override IEnumerable<IPort> outputPorts => outputList;
        public override IEnumerable<IPort> inputPorts => inputList;
        public override IDictionary<string, object> consts => constList;

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
            constDict = actionNode.constList;
        }
        #endregion

        public ActionNode ToActionNode()
        {
            var node = new ActionNode(id, defineName)
            {
                posX = posX,
                posY = posY,
            };
            node.constList = constDict;
            return node;
        }
        Node ISerializableNode.ToActionNode() => ToActionNode();
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