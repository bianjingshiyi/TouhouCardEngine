using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionDefine : ActionDefine
    {
        #region 公有方法
        #region 构造方法
        public GeneratedActionDefine(ActionGraph graph, int id, NodeDefineType type, string category, string editorName, PortDefine[] inputs, PortDefine[] outputs) : base(id, editorName)
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
            var env = flow.env;
            if (type == NodeDefineType.Event)
            {
                var game = env.game;
                var eventDefine = game.getGeneratedEventDefine(new ActionReference(cardPoolId, defineId));
                var eventArg = new EventArg(game, eventDefine);
                eventArg.setVar(GeneratedEventDefine.VAR_CARD, env.card);
                eventArg.setVar(GeneratedEventDefine.VAR_BUFF, env.buff);
                eventArg.setVar(GeneratedEventDefine.VAR_EFFECT, env.effect); 

                await setEventVariablesByInputValues(flow, node, eventArg);
                await game.triggers.doEvent(eventArg);
                sendOuterNodeOutputValuesByEventArg(flow, node, eventArg);
            }
            else
            {
                Flow childFlow = new Flow(env);
                await setEntryNodeOutputValues(flow, childFlow, node);
                await executeGraph(childFlow);
                await sendOuterNodeOutputValues(flow, childFlow, node);
            }
            return node.getOutputPort<ControlOutput>(exitPortName);
        }
        public Task executeGraph(Flow childFlow)
        {
            var entryNode = getEntryNode();
            var port = entryNode.getExitPort();
            if (port != null)
            {
                return childFlow.Run(port);
            }
            return Task.CompletedTask;
        }
        #region 事件
        public string[] getAllEventArgVarNames()
        {
            return getAfterEventArgVarNames();
        }
        public string[] getBeforeEventArgVarNames()
        {
            var inputs = getValueInputs().Select(v => v.name);
            return inputs.ToArray();
        }
        public string[] getAfterEventArgVarNames()
        {
            var inputs = getValueInputs().Select(v => v.name);
            var outputs = getValueOutputs().Select(v => v.name);
            return inputs.Concat(outputs).ToArray();
        }
        public EventVariableInfo[] getBeforeEventArgVarInfos()
        {
            var inputs = getValueInputs().Select(v => portDefineToEventVarInfo(v));
            return inputs.ToArray();
        }
        public EventVariableInfo[] getAfterEventArgVarInfos()
        {
            var inputs = getValueInputs().Select(v => portDefineToEventVarInfo(v));
            var outputs = getValueOutputs().Select(v => portDefineToEventVarInfo(v));
            return inputs.Concat(outputs).ToArray();
        }
        public string getEventName()
        {
            return getEventName(cardPoolId, defineId);
        }
        public string getBeforeEventName()
        {
            return EventHelper.getNameBefore(getEventName());
        }
        public string getAfterEventName()
        {
            return EventHelper.getNameAfter(getEventName());
        }
        public static string getEventName(long cardPoolId, int defineId)
        {
            return $"Event({defineId})From({cardPoolId})";
        }
        public static string getEventName(ActionReference actionRef)
        {
            if (actionRef == null)
                return null;
            return getEventName(actionRef.cardPoolId, actionRef.defineId);
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
        public ActionReference getReference()
        {
            return new ActionReference(cardPoolId, defineId);
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
        /// 传递变量值：出口节点的输出值-->自定义动作的输出值
        /// </summary>
        /// <param name="flow">自定义动作的执行流。</param>
        /// <param name="node">自定义动作节点。</param>
        /// <param name="arg">事件。</param>
        /// <returns></returns>
        private async Task sendOuterNodeOutputValues(Flow flow, Flow childFlow, Node node)
        {
            var exitNode = getReturnNode();
            foreach (var outputDef in getValueOutputs())
            {
                var input = exitNode.getInputPort<ValueInput>(outputDef.name);
                var output = node.getOutputPort<ValueOutput>(outputDef.name);
                flow.setValue(output, await childFlow.getValue(input));
            }
        }
        /// <summary>
        /// 传递变量值：事件-->自定义动作的输出值
        /// </summary>
        /// <param name="flow">自定义动作的执行流。</param>
        /// <param name="node">自定义动作节点。</param>
        /// <param name="arg">事件。</param>
        /// <returns></returns>
        private void sendOuterNodeOutputValuesByEventArg(Flow flow, Node node, IEventArg arg)
        {
            var exitNode = getReturnNode();
            foreach (var outputDef in getValueOutputs())
            {
                var input = exitNode.getInputPort<ValueInput>(outputDef.name);
                var output = node.getOutputPort<ValueOutput>(outputDef.name);
                flow.setValue(output, arg.getVar(input.name));
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
        private List<PortDefine> _outputs = new List<PortDefine>();

        public override IEnumerable<PortDefine> inputDefines => _inputs;
        public override IEnumerable<PortDefine> outputDefines => _outputs;
        #endregion
    }
}