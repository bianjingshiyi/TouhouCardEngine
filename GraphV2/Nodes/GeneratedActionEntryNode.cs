using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionEntryNode : Node, IDefineNode<GeneratedActionDefine>
    {
        #region 公有方法
        public GeneratedActionEntryNode(int id, GeneratedActionDefine actionDefine)
        {
            this.id = id;
            define = actionDefine;
        }
        public GeneratedActionEntryNode()
        {
            id = 0;
        }
        public override async Task<ControlOutput> run(Flow flow)
        {
            var parentFlow = flow.parent;
            var outerNode = parentFlow.currentNode;
            foreach (var input in outputPorts.OfType<ValueOutput>())
            {
                var outerInput = outerNode.getInputPort<ValueInput>(input.name);
                if (outerInput != null)
                {
                    var value = await parentFlow.getValue(outerInput);
                    flow.setValue(input, value);
                }
                else
                {
                    if (outerNode.consts.TryGetValue(input.name, out object value))
                        flow.setValue(input, value);
                }
            }
            return getExitPort();
        }
        public void Define()
        {
            DefinitionOutputs(define);
        }
        public ControlOutput getExitPort()
        {
            return getOutputPort<ControlOutput>(ActionNode.enterControlName);
        }
        public override ISerializableNode ToSerializableNode()
        {
            return new SerializableGeneratedEntryNode(this);
        }

        #endregion

        private void DefinitionOutputs(GeneratedActionDefine actionDefine)
        {
            ValueOutput valueOutput(PortDefine def)
                => _outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def);
            ValueOutput valueConst(PortDefine def)
                => _outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def);
            ControlOutput controlOutput(PortDefine def)
                => _outputs.OfType<ControlOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlOutput(this, def);


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


            foreach (var lostPort in _outputs.Except(outputList))
            {
                graph.disconnectAll(lostPort);
            }
            _outputs.Clear();
            _outputs.AddRange(outputList);
        }

        public GeneratedActionDefine define { get; set; }
        private List<IPort> _outputs = new List<IPort>();
        private List<IPort> _inputs = new List<IPort>();
        private Dictionary<string, object> _consts = new Dictionary<string, object>();
        public override IEnumerable<IPort> outputPorts => _outputs;
        public override IEnumerable<IPort> inputPorts => _inputs;
        public override IDictionary<string, object> consts => _consts;
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

        public GeneratedActionEntryNode ToGeneratedEntryNode(ActionGraph graph)
        {
            var node = new GeneratedActionEntryNode()
            {
                id = id,
                posX = posX,
                posY = posY
            };
            node.graph = graph;
            return node;
        }
        Node ISerializableNode.ToActionNode(ActionGraph graph) => ToGeneratedEntryNode(graph);
        public int id;
        public float posX;
        public float posY;

    }
}