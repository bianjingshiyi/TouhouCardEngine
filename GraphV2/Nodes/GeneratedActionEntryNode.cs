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
        #endregion

        private void DefinitionOutputs(GeneratedActionDefine actionDefine)
        {
            IPort valueOutput(PortDefine inputDef)
            {
                if (inputDef.isParams && inputDef == actionDefine.inputDefines.LastOrDefault())
                {
                    inputDef = PortDefine.Value(inputDef.type.MakeArrayType(), inputDef.name, inputDef.displayName);
                }
                return getOrCreateOutputPort(inputDef);
            }

            List<IPort> outputs = new List<IPort>();

            foreach (var def in actionDefine.inputDefines)
            {
                outputs.Add(valueOutput(def));
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
}