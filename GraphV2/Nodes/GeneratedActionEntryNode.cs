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
            foreach (var input in getOutputPorts<ValueOutput>())
            {
                bool isParams = input.define != null && input.define.isParams;
                if (isParams)
                {
                    var paramInputs = outerNode.getParamInputPorts(input.name);
                    object[] array = new object[paramInputs.Length];
                    for (int i = 0; i < paramInputs.Length; i++)
                    {
                        array[i] = await parentFlow.getValue(paramInputs[i]);
                    }
                    flow.setValue(input, array);
                    continue;
                }
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
            ValueOutput valueOutput(PortDefine inputDef)
            {
                if (inputDef.isParams && inputDef == actionDefine.inputDefines.LastOrDefault())
                {
                    inputDef = PortDefine.Value(inputDef.type.MakeArrayType(), inputDef.name, inputDef.displayName);
                }
                return getOutputPorts<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(inputDef)) ?? new ValueOutput(this, inputDef);
            }
            ValueOutput valueConst(PortDefine constDef)
                => getOutputPorts<ValueOutput>().FirstOrDefault(d => d != null && d.define.Equals(constDef)) ?? new ValueOutput(this, constDef);
            ControlOutput controlOutput(PortDefine def)
                => getOutputPorts<ControlOutput>().FirstOrDefault(d => d != null && d.define.Equals(def)) ?? new ControlOutput(this, def);


            List<IPort> outputs = new List<IPort>();


            foreach (var def in actionDefine.inputDefines)
            {
                if (def.GetPortType() == PortType.Control)
                    outputs.Add(controlOutput(def));
                else
                    outputs.Add(valueOutput(def));
            }
            foreach (var def in actionDefine.constDefines)
            {
                outputs.Add(valueConst(def));
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