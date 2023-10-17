using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public class GeneratedActionDefine : ActionDefine
    {
        #region 公有方法
        #region 构造方法
        public GeneratedActionDefine(ActionGraph graph, int id, NodeDefineType type, string category, string editorName, PortDefine[] inputs, PortDefine[] consts, PortDefine[] outputs) : base(id, editorName)
        {
            this.category = category;
            if (type != NodeDefineType.Function)
            {
                _inputs.Add(enterPortDefine);
                _outputs.Add(exitPortDefine);
            }
            this.type = type;
            if (inputs != null)
                _inputs.AddRange(inputs);
            if (consts != null)
                _consts.AddRange(consts);
            if (outputs != null)
                _outputs.AddRange(outputs);

            this.graph = graph;
        }
        #endregion
        public void InitNodes()
        {
            var entry = graph.createActionDefineEntryNode(this, 0, 0);
            entry.define = this;

            var exit = graph.createActionDefineExitNode(this, 300, 0);
            exit.define = this;
        }
        public override async Task<ControlOutput> run(Flow flow, Node node)
        {
            if (type == NodeDefineType.Event)
            {
                var env = flow.env;
                var game = env.game;
                var eventArg = new GeneratedEventArg(this);

                // 为事件设置输入变量。
                await setEventVariablesByInputValues(flow, node, eventArg);
                await game.triggers.doEvent(eventArg, async arg =>
                {
                    var flowEnv = new FlowEnv(game, env.card, env.buff, eventArg, env.effect);
                    Flow childFlow = new Flow(flow, flowEnv);

                    setEntryNodeOutputValuesByEventArg(arg, childFlow);
                    await execute(flow, childFlow, node);

                    // 为事件设置输出变量。
                    var exitNode = getReturnNode();
                    foreach (var portDefine in getValueOutputs())
                    {
                        var varName = portDefine.name;
                        var inputPort = exitNode.getInputPort<ValueInput>(varName);
                        var value = await childFlow.getValue(inputPort);
                        arg.setVar(varName, value);
                    }
                });
            }
            else
            {
                Flow childFlow = new Flow(flow);
                await setEntryNodeOutputValues(flow, childFlow, node);
                await execute(flow, childFlow, node);
            }
            return node.getOutputPort<ControlOutput>(exitPortName);
        }
        #region 事件
        public string[] getAllEventArgVarNames()
        {
            return getAfterEventArgVarNames();
        }
        public string[] getBeforeEventArgVarNames()
        {
            var inputs = getValueInputs().Select(v => v.name);
            var consts = _consts.Select(v => v.name);
            return inputs.Concat(consts).ToArray();
        }
        public string[] getAfterEventArgVarNames()
        {
            var inputs = getValueInputs().Select(v => v.name);
            var consts = _consts.Select(v => v.name);
            var outputs = getValueOutputs().Select(v => v.name);
            return inputs.Concat(consts).Concat(outputs).ToArray();
        }
        public EventVariableInfo[] getBeforeEventArgVarInfos()
        {
            var inputs = getValueInputs().Select(v => portDefineToEventVarInfo(v));
            var consts = _consts.Select(v => portDefineToEventVarInfo(v));
            return inputs.Concat(consts).ToArray();
        }
        public EventVariableInfo[] getAfterEventArgVarInfos()
        {
            var inputs = getValueInputs().Select(v => portDefineToEventVarInfo(v));
            var consts = _consts.Select(v => portDefineToEventVarInfo(v));
            var outputs = getValueOutputs().Select(v => portDefineToEventVarInfo(v));
            return inputs.Concat(consts).Concat(outputs).ToArray();
        }
        public string getEventName()
        {
            return $"Event({defineId})From({cardPoolId})";
        }
        public string getBeforeEventName()
        {
            return EventHelper.getNameBefore(getEventName());
        }
        public string getAfterEventName()
        {
            return EventHelper.getNameAfter(getEventName());
        }
        #endregion

        public void traverse(Action<Node> act, HashSet<Node> traversedActionNodeSet = null)
        {
            if (act == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<Node>();
            Node entryNode = getEntryNode();
            entryNode.traverse(act, traversedActionNodeSet);

            Node exitNode = getReturnNode();
            exitNode.traverse(act, traversedActionNodeSet);
            foreach (var portDef in outputDefines)
            {
                if (portDef.GetPortType() == PortType.Value)
                {
                    var port = exitNode.getInputPort<ValueInput>(portDef.name);
                    if (port == null || port.getConnectedOutputPort() == null)
                        continue;
                    port.traverse(act, traversedActionNodeSet);
                }
            }
        }

        public GeneratedActionEntryNode getEntryNode()
        {
            return graph.nodes.OfType<GeneratedActionEntryNode>().FirstOrDefault();
        }
        public GeneratedActionReturnNode getReturnNode()
        {
            return graph.nodes.OfType<GeneratedActionReturnNode>().FirstOrDefault();
        }
        public void addInput(PortDefine define) => _inputs.Add(define);
        public bool removeInput(PortDefine define)
        {
            if (_inputs.Remove(define))
            {
                var entryNode = getEntryNode();
                var port = entryNode.getOutputPort(define.name);
                graph.disconnectAll(port);
                return true;
            }
            return false;
        }
        public void addConst(PortDefine define) => _consts.Add(define);
        public bool removeConst(PortDefine define)
        {
            if (_consts.Remove(define))
            {
                var entryNode = getEntryNode();
                var port = entryNode.getOutputPort(define.name);
                graph.disconnectAll(port);
                return true;
            }
            return false;
        }
        public void addOutput(PortDefine define) => _outputs.Add(define);
        public bool removeOutput(PortDefine define)
        {
            if (_outputs.Remove(define))
            {
                var returnNode = getReturnNode();
                var port = returnNode.getInputPort(define.name);
                graph.disconnectAll(port);
                return true;
            }
            return false;
        }
        public void setCategory(string category)
        {
            this.category = category;
        }
        #endregion
        #region 私有方法
        private static EventVariableInfo portDefineToEventVarInfo(PortDefine portDefine)
        {
            return new EventVariableInfo()
            {
                name = portDefine.name,
                type = portDefine.isParams ? portDefine.type.MakeArrayType() : portDefine.type
            };
        }
        /// <summary>
        /// 获取自定义动作的所有输入值和常量，并且将变长参数变为数组。
        /// </summary>
        /// <param name="flow">自定义动作的执行流。</param>
        /// <param name="node">自定义动作节点。</param>
        /// <returns>变量字典。</returns>
        private async Task<Dictionary<string, object>> getInputValues(Flow flow, Node node)
        {
            Dictionary<string, object> variables = new Dictionary<string, object>();
            foreach (var inputDef in inputDefines)
            {
                bool isParams = inputDef != null && inputDef.isParams;
                var inputName = inputDef.name;
                // 变长参数。
                if (isParams)
                {
                    var paramInputs = node.getParamInputPorts(inputName);
                    object[] array = new object[paramInputs.Length - 1];
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i] = await flow.getValue(paramInputs[i]);
                    }
                    variables.Add(inputName, array);
                    continue;
                }
                // 输入变量。
                var outerInput = node.getInputPort<ValueInput>(inputName);
                if (outerInput != null)
                {
                    var value = await flow.getValue(outerInput);
                    variables.Add(inputName, value);
                }
            }
            foreach (var constDef in constDefines)
            {
                // 输入常量
                var inputName = constDef.name;
                if (node.consts.TryGetValue(inputName, out object value))
                    variables.Add(inputName, value);
            }
            return variables;
        }
        /// <summary>
        /// 传递变量值：自定义动作的输入值-->入口节点的输出值
        /// </summary>
        /// <param name="flow">自定义动作的执行流。</param>
        /// <param name="childFlow">自定义动作内部的执行流。</param>
        /// <param name="node">自定义动作节点。</param>
        /// <returns></returns>
        private async Task setEntryNodeOutputValues(Flow flow, Flow childFlow, Node node)
        {
            var variables = await getInputValues(flow, node);
            var entryNode = getEntryNode();
            foreach (var input in entryNode.getOutputPorts<ValueOutput>())
            {
                if (variables.TryGetValue(input.name, out var value))
                {
                    childFlow.setValue(input, value);
                }
            }
        }
        /// <summary>
        /// 传递变量值：自定义动作的输入值-->事件
        /// </summary>
        /// <param name="flow">自定义动作的执行流。</param>
        /// <param name="node">自定义动作节点。</param>
        /// <param name="arg">事件。</param>
        /// <returns></returns>
        private async Task setEventVariablesByInputValues(Flow flow, Node node, EventArg arg)
        {
            var variables = await getInputValues(flow, node);
            foreach (var pair in variables)
            {
                arg.setVar(pair.Key, pair.Value);
            }
        }
        /// <summary>
        /// 传递变量值：事件-->入口节点的输出值
        /// </summary>
        /// <param name="arg">事件。</param>
        /// <param name="childFlow">自定义动作内部的执行流。</param>
        private void setEntryNodeOutputValuesByEventArg(EventArg arg, Flow childFlow)
        {
            var entryNode = getEntryNode();
            foreach (var input in entryNode.getOutputPorts<ValueOutput>())
            {
                childFlow.setValue(input, arg.getVar(input.name));
            }
        }
        private async Task execute(Flow flow, Flow childFlow, Node node)
        {
            var entryNode = getEntryNode();
            var port = entryNode.getExitPort();
            if (port != null)
            {
                await childFlow.Run(port);
            }

            var exitNode = getReturnNode();
            foreach (var outputDef in getValueOutputs())
            {
                var input = exitNode.getInputPort<ValueInput>(outputDef.name);
                var output = node.getOutputPort<ValueOutput>(outputDef.name);
                flow.setValue(output, await childFlow.getValue(input));
            }
        }
        #endregion
        #region 属性字段
        [Obsolete("Use defineId instead.")]
        public int id 
        {
            get => defineId;
            set => defineId = value;
        }
        public ActionGraph graph;

        private List<PortDefine> _inputs = new List<PortDefine>();
        private List<PortDefine> _consts = new List<PortDefine>();
        private List<PortDefine> _outputs = new List<PortDefine>();

        public override IEnumerable<PortDefine> inputDefines => _inputs;
        public override IEnumerable<PortDefine> constDefines => _consts;
        public override IEnumerable<PortDefine> outputDefines => _outputs;
        #endregion
    }
    [Serializable]
    public class SerializableActionDefine
    {
        #region 公有方法
        #region 构造函数
        public SerializableActionDefine(GeneratedActionDefine generatedActionDefine)
        {
            if (generatedActionDefine == null)
                throw new ArgumentNullException(nameof(generatedActionDefine));
            id = generatedActionDefine.id;
            type = (int)generatedActionDefine.type;
            category = generatedActionDefine.category;
            name = generatedActionDefine.editorName;
            graph = new SerializableActionNodeGraph(generatedActionDefine.graph);
            // 不包括动作入口端点
            inputs.AddRange(generatedActionDefine.inputDefines.Where(d => d.name != ActionDefine.enterPortName).Select(d => new SerializablePortDefine(d)));
            consts.AddRange(generatedActionDefine.constDefines.Select(d => new SerializablePortDefine(d)));
            // 不包括动作出口端点
            outputs.AddRange(generatedActionDefine.outputDefines.Where(d => d.name != ActionDefine.exitPortName).Select(d => new SerializablePortDefine(d)));
        }
        #endregion
        public GeneratedActionDefine toGeneratedActionDefine(TypeFinder typeFinder)
        {
            // 铺设节点。
            var graph = new ActionGraph();
            var nodes = this.graph.GetNodes(graph);
            graph.AddNodes(nodes);

            var define = new GeneratedActionDefine(graph, id, (NodeDefineType)type, category, name,
                inputs.Select(s => s.ToPortDefine(typeFinder)).ToArray(),
                consts.Select(s => s.ToPortDefine(typeFinder)).ToArray(),
                outputs.Select(s => s.ToPortDefine(typeFinder)).ToArray());
            return define;
        }
        #endregion
        #region 属性字段
        public int id;
        public int type;
        public string name;
        public string category;
        public SerializableActionNodeGraph graph;
        public List<SerializablePortDefine> inputs = new List<SerializablePortDefine>();
        public List<SerializablePortDefine> consts = new List<SerializablePortDefine>();
        public List<SerializablePortDefine> outputs = new List<SerializablePortDefine>();

        [Obsolete]
        public List<SerializableValueDefine> inputList = new List<SerializableValueDefine>();
        [Obsolete]
        public List<SerializableValueDefine> constList = new List<SerializableValueDefine>();
        [Obsolete]
        public List<SerializableValueDefine> outputList = new List<SerializableValueDefine>();
        [Obsolete]
        public List<ReturnValueRef> returnList = null;
        [Obsolete]
        public List<SerializableReturnValueRef> seriReturnList = new List<SerializableReturnValueRef>();
        [Obsolete]
        public ActionNode action;
        [Obsolete]
        public int rootActionId;
        [Obsolete]
        public List<SerializableActionNode> actionNodeList = new List<SerializableActionNode>();
        #endregion
    }
}