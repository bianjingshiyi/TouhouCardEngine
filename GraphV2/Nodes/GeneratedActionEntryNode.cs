using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public class GeneratedActionEntryNode : Node
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
        public override SerializableNode ToSerializableNode()
        {
            return new SerializableGeneratedEntryNode(this);
        }

        #endregion

        private void DefinitionOutputs(GeneratedActionDefine actionDefine)
        {
            List<IPort> outputs = new List<IPort>();


            foreach (var def in actionDefine.inputDefines)
            {
                outputs.Add(getOrCreateOutputPort(def));
            }


            foreach (var lostPort in outputList.Except(outputs))
            {
                graph.disconnectAll(lostPort);
            }
            outputList.Clear();
            outputList.AddRange(outputs);
        }

        public GeneratedActionDefine define { get; set; }
    }
    [Serializable]
    public class SerializableGeneratedEntryNode : SerializableNode
    {
        public SerializableGeneratedEntryNode(GeneratedActionEntryNode node) : base(node)
        {
        }

        public GeneratedActionEntryNode ToGeneratedEntryNode(ActionGraph graph)
        {
            var node = new GeneratedActionEntryNode()
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