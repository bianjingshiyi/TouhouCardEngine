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
        public GeneratedActionDefine(ActionGraph graph, int id, string category, string defineName, PortDefine[] inputs, PortDefine[] consts, PortDefine[] outputs) : base(defineName, null)
        {
            this.id = id;
            this.category = category;
            _inputs.Add(enterPortDefine);
            _outputs.Add(exitPortDefine);
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
            entry.generatedDefine = this;

            var exit = graph.createActionDefineExitNode(this, 300, 0);
            exit.generatedDefine = this;
        }
        public override async Task<ControlOutput> run(Flow flow, Node node)
        {
            Flow childFlow = new Flow(flow);

            var entryNode = getEntryNode();

            var port = entryNode.getExitPort();

            await childFlow.Run(port);

            var exitNode = getReturnNode();

            foreach (var outputDef in outputDefines)
            {
                var input = exitNode.getInputPort<ValueInput>(outputDef.name);
                var output = node.getOutputPort<ValueOutput>(outputDef.name);
                flow.setValue(output, await childFlow.getValue(input));
            }
            return node.getOutputPort<ControlOutput>("exit");
        }

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
        #region 属性字段
        public int id { get; }
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
            category = generatedActionDefine.category;
            name = generatedActionDefine.defineName;
            graph = new SerializableActionNodeGraph(generatedActionDefine.graph);
            // 不包括动作入口端点
            inputs.AddRange(generatedActionDefine.inputDefines.Where(d => d.name != ActionDefine.enterPortName).Select(d => new SerializablePortDefine(d)));
            consts.AddRange(generatedActionDefine.constDefines.Select(d => new SerializablePortDefine(d)));
            // 不包括动作出口端点
            outputs.AddRange(generatedActionDefine.outputDefines.Where(d => d.name != ActionDefine.exitPortName).Select(d => new SerializablePortDefine(d)));
        }
        #endregion
        public static GeneratedActionDefine[] toGeneratedActionDefines(IEnumerable<SerializableActionDefine> defines, ActionDefineFinder defineFinder, EventTypeInfoFinder eventFinder, TypeFinder typeFinder)
        {
            Dictionary<SerializableActionDefine, GeneratedActionDefine> dict = new Dictionary<SerializableActionDefine, GeneratedActionDefine>();
            // 铺设节点。
            foreach (var seri in defines)
            {
                var graph = new ActionGraph();
                var nodes = seri.graph.GetNodes();
                graph.AddNodes(nodes);

                var define = new GeneratedActionDefine(graph, seri.id, seri.category, seri.name,
                    seri.inputs.Select(s => s.ToPortDefine(typeFinder)).ToArray(),
                    seri.consts.Select(s => s.ToPortDefine(typeFinder)).ToArray(),
                    seri.outputs.Select(s => s.ToPortDefine(typeFinder)).ToArray());

                dict.Add(seri, define);
            }
            // 定义所有节点。
            foreach (var define in dict.Values)
            {
                define.graph.DefineNodes(getActionDefine, eventFinder);
                define.graph.DefineGeneratedActionDefine(define);
            }
            // 连接所有节点。
            foreach (var pair in dict)
            {
                var graph = pair.Value.graph;
                var nodes = pair.Key.graph.GetConnections(graph);
                graph.AddConnections(nodes);
            }
            return dict.Values.ToArray();

            ActionDefine getActionDefine(string name)
            {
                return defineFinder?.Invoke(name) ?? dict.Values.FirstOrDefault(a => a.defineName == name || (a.obsoleteNames != null && a.obsoleteNames.Contains(name)));
            }
        }
        #endregion
        #region 属性字段
        public int id;
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