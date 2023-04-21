using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public class TriggerEntryNode : Node, IDefineNode<EventDefine>
    {
        #region 公有方法
        public TriggerEntryNode(int id, string eventName)
        {
            this.id = id;
            this.eventName = eventName;

            inputList.Add(new ValueInput(this, triggerConditionPortDefine));
            outputList.Add(new ControlOutput(this, actionPortDefine));
        }
        public override Task<ControlOutput> run(Flow flow)
        {
            foreach (var output in getOutputPorts<ValueOutput>())
            {
                flow.setValue(output, flow.env.eventArg.getVar(output.name));
            }
            return Task.FromResult(getOutputPort<ControlOutput>(actionPortName));
        }
        public void Define()
        {
            DefinitionConditions();
            DefinitionOutputs(define);
        }
        public ValueInput getTriggerCondtionValuePort()
        {
            return getInputPort<ValueInput>("condition");
        }
        public ControlOutput getActionOutputPort()
        {
            if (outputList.Count <= 0)
                return null;
            return outputList[0] as ControlOutput;
        }
        public ValueInput getTargetConditionPort(int index)
        {
            return getParamInputPort(targetConditionName, index);
        }
        public void AddTargetChecker(TargetChecker checker)
        {
            checker.trigger = this;
            targetCheckerList.Add(checker);
            DefinitionConditions();
        }
        public void RemoveTargetChecker(int index)
        {
            var checker = targetCheckerList[index];
            checker.trigger = null;
            targetCheckerList.Remove(checker);
            DefinitionConditions();
        }
        public override ISerializableNode ToSerializableNode()
        {
            return new SerializableTriggerNode()
            {
                id = id,
                eventName = eventName,
                targetCheckerList = targetCheckerList.ConvertAll(t => new SerializableTargetChecker(t)),
                posX = posX,
                posY = posY,
                hideEvents = hideEvents
            };
        }


        private void DefinitionConditions()
        {
            ValueInput valueCondition()
                 => inputList.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(triggerConditionPortDefine)) ??
                 new ValueInput(this, triggerConditionPortDefine);
            ValueInput targetCondition(int paramIndex)
                 => inputList.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(targetConditionPortDefine) && d.paramIndex == paramIndex) ??
                 new ValueInput(this, targetConditionPortDefine, paramIndex);


            var inputs = new List<IPort> { valueCondition() };
            for (int i = 0; i < targetCheckerList.Count; i++)
            {
                inputs.Add(targetCondition(i));
            }

            foreach (var lostPort in inputList.Except(inputs))
            {
                graph.disconnectAll(lostPort);
            }
            inputList.Clear();
            inputList.AddRange(inputs);
        }
        private void DefinitionOutputs(EventDefine typeInfo)
        {
            ValueOutput valueOutput(EventVariableInfo info)
                 => outputList.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.type == info.type && d.define.name == info.name) ??
                 new ValueOutput(this, PortDefine.Value(info.type, info.name, info.name));
            ControlOutput controlOutput()
                 => outputList.OfType<ControlOutput>().FirstOrDefault() ??
                 new ControlOutput(this, actionPortDefine);


            var outputs = new List<IPort>()
            {
                controlOutput()
            };
            if (typeInfo?.variableInfos != null)
            {
                foreach (EventVariableInfo info in typeInfo.variableInfos)
                {
                    outputs.Add(valueOutput(info));
                }
            }

            foreach (var lostPort in outputList.Except(outputs))
            {
                graph.disconnectAll(lostPort);
            }
            outputList.Clear();
            outputList.AddRange(outputs);
        }

        #endregion
        public EventDefine define { get; set; }
        public string eventName;
        public bool hideEvents;
        public List<TargetChecker> targetCheckerList { get; private set; } = new List<TargetChecker>(); 

        private const string targetConditionName = "targetCondition";
        private const string actionPortName = "action";
        private static PortDefine targetConditionPortDefine = PortDefine.Value(typeof(bool), targetConditionName, "目标{0}条件");
        private static PortDefine triggerConditionPortDefine = PortDefine.Value(typeof(bool), "condition", "触发条件");
        private static PortDefine actionPortDefine = PortDefine.Control(actionPortName, string.Empty);
    }
    public class SerializableTriggerNode : ISerializableNode
    {
        public TriggerEntryNode ToActionNode(ActionGraph graph)
        {
            var entry = new TriggerEntryNode(id, eventName)
            {
                posX = posX,
                posY = posY,
                hideEvents = hideEvents
            };
            entry.graph = graph;
            foreach (var seri in targetCheckerList)
            {
                var checker = seri.toTargetChecker();
                entry.AddTargetChecker(checker);
            }
            return entry;
        }
        Node ISerializableNode.ToActionNode(ActionGraph graph) => ToActionNode(graph);

        public int id;
        public float posX;
        public float posY;
        public string eventName;
        public bool hideEvents;
        public List<SerializableTargetChecker> targetCheckerList = new List<SerializableTargetChecker>();
    }
}