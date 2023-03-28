using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionReturnNode : Node, IDefineNode<GeneratedActionDefine>
    {
        #region 公有方法
        public GeneratedActionReturnNode(int id, GeneratedActionDefine actionDefine)
        {
            this.id = id;
            define = actionDefine;
        }
        public GeneratedActionReturnNode()
        {
            id = 0;
        }
        public override Task<ControlOutput> run(Flow flow)
        {
            ActionNode node = flow.parent.currentNode as ActionNode;
            return Task.FromResult(node?.getExitPort());
        }
        public void Define()
        {
            DefinitionInputs(define);
        }
        public override ISerializableNode ToSerializableNode()
        {
            return new SerializableGeneratedReturnNode(this);
        }

        #endregion
        private void DefinitionInputs(GeneratedActionDefine actionDefine)
        {
            ControlInput controlInput(PortDefine def)
                => _inputs.OfType<ControlInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlInput(this, def);
            ValueInput valueInput(PortDefine def)
                => _inputs.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueInput(this, def, -1);

            List<IPort> inputList = new List<IPort>();

            if (actionDefine.outputDefines != null)
            {
                foreach (var def in actionDefine.outputDefines)
                {
                    if (def.GetPortType() == PortType.Control)
                        inputList.Add(controlInput(def));
                    else
                        inputList.Add(valueInput(def));
                }
            }


            foreach (var lostPort in _inputs.Except(inputList))
            {
                graph.disconnectAll(lostPort);
            }

            _inputs.Clear();
            _inputs.AddRange(inputList);
        }
        public GeneratedActionDefine define { get; set; }
        private List<IPort> _inputs = new List<IPort>();
        private IPort[] _outputs = new IPort[0];
        private Dictionary<string, object> _consts = new Dictionary<string, object>();
        public override IEnumerable<IPort> outputPorts => _outputs;
        public override IEnumerable<IPort> inputPorts => _inputs;
        public override IDictionary<string, object> consts => _consts;
    }

    [Serializable]
    public class SerializableGeneratedReturnNode : ISerializableNode
    {
        public SerializableGeneratedReturnNode(GeneratedActionReturnNode node)
        {
            id = node.id;
            posX = node.posX;
            posY = node.posY;
        }

        public GeneratedActionReturnNode ToGeneratedEntryNode()
        {
            return new GeneratedActionReturnNode()
            {
                id = id,
                posX = posX,
                posY = posY
            };
        }
        Node ISerializableNode.ToActionNode() => ToGeneratedEntryNode();
        public int id;
        public float posX;
        public float posY;

    }
}