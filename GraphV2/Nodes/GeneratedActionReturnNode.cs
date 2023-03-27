using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionReturnNode : IActionNode
    {
        #region 公有方法
        public GeneratedActionReturnNode(int id, GeneratedActionDefine actionDefine)
        {
            this.id = id;
            generatedDefine = actionDefine;
        }
        public GeneratedActionReturnNode()
        {
            id = 0;
        }
        public Task<ControlOutput> run(Flow flow)
        {
            ActionNode node = flow.parent.currentNode as ActionNode;
            return Task.FromResult(node?.getExitPort());
        }
        public void Define()
        {
            DefinitionInputs(generatedDefine);
        }
        public void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<IActionNode>();

            foreach (var input in inputs)
            {
                input.traverse(action, traversedActionNodeSet);
            }
        }
        public ISerializableNode ToSerializableNode()
        {
            return new SerializableGeneratedReturnNode(this);
        }

        #endregion
        private void DefinitionInputs(GeneratedActionDefine actionDefine)
        {
            ControlInput controlInput(PortDefine def)
                => inputs.OfType<ControlInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlInput(this, def);
            ValueInput valueInput(PortDefine def)
                => inputs.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueInput(this, def, -1);

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


            foreach (var lostPort in inputs.Except(inputList))
            {
                graph.disconnectAll(lostPort);
            }

            inputs.Clear();
            inputs.AddRange(inputList);
        }
        public ActionGraph graph { get; set; }
        public int id { get; set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public GeneratedActionDefine generatedDefine { get; set; }
        public List<IPort> inputs = new List<IPort>();
        private IPort[] _outputs = new IPort[0];
        private Dictionary<string, object> _consts = new Dictionary<string, object>();
        IEnumerable<IPort> IActionNode.outputPorts => _outputs;
        IEnumerable<IPort> IActionNode.inputPorts => inputs;
        IDictionary<string, object> IActionNode.consts => _consts;
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
        IActionNode ISerializableNode.ToActionNode() => ToGeneratedEntryNode();
        public int id;
        public float posX;
        public float posY;

    }
}