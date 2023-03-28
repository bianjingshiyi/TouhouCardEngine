using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public class TriggerEntryNode : Node
    {
        #region 公有方法
        public TriggerEntryNode(int id, string eventName)
        {
            this.id = id;
            this.eventName = eventName;

            _conditions = new List<IPort>()
            {
                new ValueInput(this, triggerConditionPortDefine),
            };
            _outputs = new List<IPort>()
            {
                new ControlOutput(this, actionPortDefine),
            };
        }
        public override Task<ControlOutput> run(Flow flow)
        {
            return Task.FromResult(getOutputPort<ControlOutput>("action"));
        }
        public void Define()
        {
            DefinitionConditions();
            DefinitionOutputs(eventTypeInfo);
        }
        public ValueInput getTriggerCondtionValuePort()
        {
            return getInputPort<ValueInput>("condition");
        }
        public ControlOutput getActionOutputPort()
        {
            return _outputs?.FirstOrDefault() as ControlOutput;
        }
        public ValueInput getTargetConditionPort(int index)
        {
            return getInputPorts<ValueInput>().FirstOrDefault(p => p.name == targetConditionName && p.paramIndex == index);
        }
        public void AddTargetChecker(TargetChecker checker)
        {
            targetCheckerList.Add(checker);
            DefinitionConditions();
        }
        public void RemoveTargetChecker(int index)
        {
            targetCheckerList.RemoveAt(index);
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
                 => _conditions.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(triggerConditionPortDefine)) ??
                 new ValueInput(this, triggerConditionPortDefine);
            ValueInput targetCondition(int paramIndex)
                 => _conditions.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(targetConditionPortDefine) && d.paramIndex == paramIndex) ??
                 new ValueInput(this, targetConditionPortDefine, paramIndex);


            var inputs = new List<IPort> { valueCondition() };
            for (int i = 0; i < targetCheckerList.Count; i++)
            {
                inputs.Add(targetCondition(i));
            }

            foreach (var lostPort in _conditions.Except(inputs))
            {
                graph.disconnectAll(lostPort);
            }
            _conditions.Clear();
            _conditions.AddRange(inputs);
        }
        private void DefinitionOutputs(EventDefine typeInfo)
        {
            ValueOutput valueOutput(EventVariableInfo info)
                 => _outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.type == info.type && d.define.name == info.name) ??
                 new ValueOutput(this, PortDefine.Value(info.type, info.name, info.name), flow =>
                 {
                     return Task.FromResult(flow.env.eventArg.getVar(info.name));
                 });
            ControlOutput controlOutput()
                 => _outputs.OfType<ControlOutput>().FirstOrDefault() ??
                 new ControlOutput(this, actionPortDefine);


            var outputs = new List<IPort>()
            {
                controlOutput()
            };
            foreach (EventVariableInfo info in typeInfo.variableInfos)
            {
                outputs.Add(valueOutput(info));
            }

            foreach (var lostPort in _outputs.Except(outputs))
            {
                graph.disconnectAll(lostPort);
            }
            _outputs.Clear();
            _outputs.AddRange(outputs);
        }

        #endregion
        public EventDefine eventTypeInfo { get; set; }
        public string eventName;
        public bool hideEvents;
        public List<TargetChecker> targetCheckerList { get; private set; } = new List<TargetChecker>(); 
        private List<IPort> _conditions;
        private List<IPort> _outputs;
        public override IEnumerable<IPort> outputPorts => _outputs;
        public override IEnumerable<IPort> inputPorts => _conditions;
        public override IDictionary<string, object> consts => targetCheckerList.ToDictionary(t => $"target[{t.targetIndex}]", t => (object)t);

        private const string targetConditionName = "targetCondition";
        private static PortDefine targetConditionPortDefine = PortDefine.Value(typeof(bool), targetConditionName, "目标{0}条件");
        private static PortDefine triggerConditionPortDefine = PortDefine.Value(typeof(bool), "condition", "触发条件");
        private static PortDefine actionPortDefine = PortDefine.Control("action", string.Empty);
    }
    public class SerializableTriggerNode : ISerializableNode
    {
        public TriggerEntryNode ToActionNode()
        {
            var entry = new TriggerEntryNode(id, eventName)
            {
                posX = posX,
                posY = posY,
                hideEvents = hideEvents
            };
            foreach (var seri in targetCheckerList)
            {
                var checker = seri.toTargetChecker();
                checker.trigger = entry;
                entry.AddTargetChecker(checker);
            }
            return entry;
        }
        Node ISerializableNode.ToActionNode() => ToActionNode();

        public int id;
        public float posX;
        public float posY;
        public string eventName;
        public bool hideEvents;
        public List<SerializableTargetChecker> targetCheckerList = new List<SerializableTargetChecker>();
    }
}