using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    /// <summary>
    /// 单个动作的数据结构。
    /// 由于要方便编辑器统一进行操作更改和存储，这个数据结构不允许多态。
    /// 这个数据结构必须同时支持多种类型的语句，比如赋值，分支，循环，返回，方法调用。
    /// </summary>
    public sealed class ActionNode : Node
    {
        #region 公有方法
        #region 构造方法
        public ActionNode(int id, ActionReference defineRef)
        {
            this.id = id;
            this.defineRef = defineRef;
        }
        #endregion
        public override string ToString()
        {
            return $"动作节点{defineRef}";
        }
        public override Task<ControlOutput> run(Flow flow)
        {
            var define = flow.env.game.getActionDefine(defineRef);
            if (define != null)
            {
                return define.run(flow, this);
            }
            return Task.FromResult<ControlOutput>(null);
        }
        public void Define()
        {
            DefinitionInputs(define);
            DefinitionOutputs(define);
        }
        public ControlInput getEnterPort()
        {
            return getInputPort<ControlInput>(enterControlName);
        }
        public ControlOutput getExitPort()
        {
            return getOutputPort<ControlOutput>(exitControlName);
        }
        public override SerializableNode ToSerializableNode()
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
            if (define?.inputDefines != null)
            {
                foreach (var portDefine in define.inputDefines)
                {
                    if (portDefine.GetPortType() == PortType.Control)
                    {
                        inputs.Add(controlInput(portDefine));
                    }
                    else
                    {
                        if (portDefine.isParams && portDefine == define.inputDefines.LastOrDefault())
                        {
                            // 变长参数。
                            var count = 0;
                            var paramInputs = getParamInputPorts(portDefine.name);
                            for (int ci = paramInputs.Length - 1; ci >= 0; ci--)
                            {
                                var paramInput = paramInputs[ci];
                                if (paramInput == null || !(paramInput.connections.Any() || hasInputDefaultValue(portDefine.name, paramInput.paramIndex)))
                                    continue;
                                count = ci + 1;
                                break;
                            }
                            count++;
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
                => outputList.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def);
            ControlOutput controlOutput(PortDefine def)
                => outputList.OfType<ControlOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlOutput(this, def);

            List<IPort> outputs = new List<IPort>();
            if (define?.outputDefines != null)
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

        [Obsolete]
        public string defineName;
        public ActionReference defineRef;
        public ActionDefine define { get; set; }

        public const string enterControlName = "enter";
        public const string exitControlName = "exit";
    }

    [Serializable]
    public sealed class SerializableActionNode : SerializableNode
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionNode(ActionNode actionNode) : base(actionNode)
        {
            defineRef = actionNode.defineRef;
        }
        #endregion

        public ActionNode ToActionNode(ActionGraph graph)
        {
            var node = new ActionNode(id, defineRef)
            {
                posX = posX,
                posY = posY,
            };
            node.defineName = defineName;
            IEnumerable<InputDefaultValue> defaultValues = 
                inputDefaultValues?.Select(dv => dv.deserialize()) 
                ?? constDict?.Select(c => new InputDefaultValue(c.Key, -1, c.Value));
            InitNode(node, defaultValues, graph);
            return node;
        }
        public override Node ToNode(ActionGraph graph) => ToActionNode(graph);
        #endregion
        #region 属性字段
        public ActionReference defineRef;
        [Obsolete]
        public Dictionary<string, object> constDict;

        [Obsolete]
        public string defineName;
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