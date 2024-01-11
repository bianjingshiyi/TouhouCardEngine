using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionReturnNode : Node
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
            return Task.FromResult<ControlOutput>(null);
        }
        public void Define()
        {
            DefinitionInputs(define);
        }
        public override SerializableNode ToSerializableNode()
        {
            return new SerializableGeneratedReturnNode(this);
        }

        #endregion
        private void DefinitionInputs(GeneratedActionDefine actionDefine)
        {
            List<IPort> inputs = new List<IPort>();

            if (actionDefine.outputDefines != null)
            {
                foreach (var def in actionDefine.outputDefines)
                {
                    if (def == ActionDefine.exitPortDefine)
                        continue;
                    inputs.Add(getOrCreateInputPort(def));
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
    public class SerializableGeneratedReturnNode : SerializableNode
    {
        public SerializableGeneratedReturnNode(GeneratedActionReturnNode node) : base(node)
        {
        }

        public GeneratedActionReturnNode ToGeneratedEntryNode(ActionGraph graph)
        {
            var node = new GeneratedActionReturnNode()
            {
                id = id,
                posX = posX,
                posY = posY
            };
            InitNode(node, graph);
            return node;
        }
        public override Node ToNode(ActionGraph graph) => ToGeneratedEntryNode(graph);

    }
}