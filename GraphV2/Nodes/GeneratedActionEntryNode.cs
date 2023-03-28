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
        public override Task<ControlOutput> run(Flow flow)
        {
            return Task.FromResult(getExitPort());
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
                => _outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def, async flow =>
                {
                    var parentFlow = flow.parent;
                    ValueInput input = parentFlow.currentNode.getInputPort<ValueInput>(def.name);
                    return await flow.getValue(input);
                });
            ValueOutput valueConst(PortDefine def)
                => _outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueOutput(this, def, flow =>
                {
                    var parentFlow = flow.parent;
                    if (parentFlow.currentNode.consts.TryGetValue(def.name, out object value))
                        return Task.FromResult(value);
                    return null;
                });
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
        Node ISerializableNode.ToActionNode() => ToGeneratedEntryNode();
        public int id;
        public float posX;
        public float posY;

    }
}