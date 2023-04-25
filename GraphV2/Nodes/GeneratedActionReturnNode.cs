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
                => getInputPorts<ControlInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlInput(this, def);
            ValueInput valueInput(PortDefine def)
                => getInputPorts<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ValueInput(this, def, -1);

            List<IPort> inputs = new List<IPort>();

            if (actionDefine.outputDefines != null)
            {
                foreach (var def in actionDefine.outputDefines)
                {
                    if (def == ActionDefine.exitPortDefine)
                        continue;
                    if (def.GetPortType() == PortType.Control)
                        inputs.Add(controlInput(def));
                    else
                        inputs.Add(valueInput(def));
                }
            }


            foreach (var lostPort in inputList.Except(inputs))
            {
                graph.disconnectAll(lostPort);
            }

            inputList.Clear();
            inputList.AddRange(inputs);
        }
        public GeneratedActionDefine define { get; set; }
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

        public GeneratedActionReturnNode ToGeneratedEntryNode(ActionGraph graph)
        {
            var node = new GeneratedActionReturnNode()
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