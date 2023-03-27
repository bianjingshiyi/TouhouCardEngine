using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionEntryNode : IActionNode
    {
        #region 公有方法
        public GeneratedActionEntryNode(int id, GeneratedActionDefine actionDefine)
        {
            this.id = id;
            generatedDefine = actionDefine;
        }
        public GeneratedActionEntryNode()
        {
            id = 0;
        }
        public Task<ControlOutput> run(Flow flow)
        {
            return Task.FromResult(getExitPort());
        }
        public void Define()
        {
            DefinitionOutputs(generatedDefine);
        }
        public void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<IActionNode>();

            var output = this.getOutputPort<ControlOutput>("action");
            if (output != null)
            {
                output.traverse(action, traversedActionNodeSet);
            }
        }
        public ControlOutput getExitPort()
        {
            return this.getOutputPort<ControlOutput>(ActionNode.enterControlName);
        }
        public ISerializableNode ToSerializableNode()
        {
            return new SerializableGeneratedEntryNode(this);
        }

        #endregion

        private void DefinitionOutputs(GeneratedActionDefine actionDefine)
        {
            ValueOutput valueOutput(PortDefine def)
                => outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def, async flow =>
                {
                    var parentFlow = flow.parent;
                    ValueInput input = parentFlow.currentNode.getInputPort<ValueInput>(def.name);
                    return await flow.getValue(input);
                });
            ValueOutput valueConst(PortDefine def)
                => outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def, flow =>
                {
                    var parentFlow = flow.parent;
                    if (parentFlow.currentNode.consts.TryGetValue(def.name, out object value))
                        return Task.FromResult(value);
                    return null;
                });
            ControlOutput controlOutput(PortDefine def)
                => outputs.OfType<ControlOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlOutput(this, def);


            List<IPort> outputList = new List<IPort>();


            foreach (var def in actionDefine.inputDefines)
            {
                if (def.GetPortType() == PortType.Control)
                    outputList.Add(controlOutput(def));
                else
                    outputList.Add(valueOutput(def));
            }
            foreach (var def in actionDefine.constDefines)
            {
                outputList.Add(valueConst(def));
            }


            foreach (var lostPort in outputs.Except(outputList))
            {
                graph.disconnectAll(lostPort);
            }
            outputs.Clear();
            outputs.AddRange(outputList);
        }

        public ActionGraph graph { get; set; }
        public int id { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public GeneratedActionDefine generatedDefine { get; set; }
        public List<IPort> outputs = new List<IPort>();
        private List<IPort> _inputs = new List<IPort>();
        private Dictionary<string, object> _consts = new Dictionary<string, object>();
        IEnumerable<IPort> IActionNode.outputPorts => outputs;
        IEnumerable<IPort> IActionNode.inputPorts => _inputs;
        IDictionary<string, object> IActionNode.consts => _consts;
    }
    [Serializable]
    public class SerializableGeneratedEntryNode : ISerializableNode
    {
        public SerializableGeneratedEntryNode(GeneratedActionEntryNode node)
        {
            id = node.id;
            posX = node.posX;
            posY = node.posY;
        }

        public GeneratedActionEntryNode ToGeneratedEntryNode()
        {
            var node = new GeneratedActionEntryNode()
            {
                id = id,
                posX = posX,
                posY = posY
            };
            return node;
        }
        IActionNode ISerializableNode.ToActionNode() => ToGeneratedEntryNode();
        public int id;
        public float posX;
        public float posY;

    }
}