using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    [Serializable]
    public class TriggerEntryNode : IActionNode
    {
        #region 公有方法
        public TriggerEntryNode(int id, string eventName)
        {
            this.id = id;
            this.eventName = eventName;

            conditions = new List<IPort>()
            {
                new ValueInput(this, triggerConditionPortDefine),
            };
            outputs = new List<IPort>()
            {
                new ControlOutput(this, actionPortDefine),
            };
        }
        public Task<ControlOutput> run(Flow flow)
        {
            return Task.FromResult(this.getOutputPort<ControlOutput>("action"));
        }
        public void Define()
        {
            DefinitionConditions();
            DefinitionOutputs(eventTypeInfo);
        }
        public void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;

            var conditionPort = getTriggerCondtionValuePort();
            var nextAction = getActionOutputPort();

            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<IActionNode>();

            conditionPort?.traverse(action, traversedActionNodeSet);
            if (targetCheckerList != null && targetCheckerList.Count > 0)
            {
                for (int i = 0; i < targetCheckerList.Count; i++)
                {
                    if (targetCheckerList[i] == null)
                        continue;
                    targetCheckerList[i].traverse(action, traversedActionNodeSet);
                }
            }
            IActionNode nextNode = nextAction.connections.FirstOrDefault()?.destination?.node;
            if (nextNode != null && !traversedActionNodeSet.Contains(nextNode))
                nextAction.traverse(action, traversedActionNodeSet);
        }
        public ValueInput getTriggerCondtionValuePort()
        {
            return this.getInputPort<ValueInput>("condition");
        }
        public ControlOutput getActionOutputPort()
        {
            return outputs?.FirstOrDefault() as ControlOutput;
        }
        public ValueInput getTargetConditionPort(int index)
        {
            return this.getInputPorts<ValueInput>().FirstOrDefault(p => p.name == targetConditionName && p.paramIndex == index);
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
        public SerializableTriggerNode ToSerializableNode()
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
        ISerializableNode IActionNode.ToSerializableNode() => ToSerializableNode();


        private void DefinitionConditions()
        {
            ValueInput valueCondition()
                 => conditions.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(triggerConditionPortDefine)) ??
                 new ValueInput(this, triggerConditionPortDefine);
            ValueInput targetCondition(int paramIndex)
                 => conditions.OfType<ValueInput>().FirstOrDefault(d => d != null && d.define.Equals(targetConditionPortDefine) && d.paramIndex == paramIndex) ??
                 new ValueInput(this, targetConditionPortDefine, paramIndex);


            var inputs = new List<IPort> { valueCondition() };
            for (int i = 0; i < targetCheckerList.Count; i++)
            {
                inputs.Add(targetCondition(i));
            }

            foreach (var lostPort in conditions.Except(inputs))
            {
                graph.disconnectAll(lostPort);
            }
            conditions.Clear();
            conditions.AddRange(inputs);
        }
        private void DefinitionOutputs(EventTypeInfo typeInfo)
        {
            ValueOutput valueOutput(EventVariableInfo info)
                 => this.outputs.OfType<ValueOutput>().FirstOrDefault(d => d != null && d.define.type == info.type && d.define.name == info.name) ??
                 new ValueOutput(this, PortDefine.Value(info.type, info.name, info.name), flow =>
                 {
                     return Task.FromResult(flow.env.eventArg.getVar(info.name));
                 });
            ControlOutput controlOutput()
                 => this.outputs.OfType<ControlOutput>().FirstOrDefault() ??
                 new ControlOutput(this, actionPortDefine);


            var outputs = new List<IPort>()
            {
                controlOutput()
            };
            foreach (EventVariableInfo info in typeInfo.variableInfos)
            {
                outputs.Add(valueOutput(info));
            }

            foreach (var lostPort in this.outputs.Except(outputs))
            {
                graph.disconnectAll(lostPort);
            }
            this.outputs.Clear();
            this.outputs.AddRange(outputs);
        }

        #endregion
        [BsonIgnore]
        public ActionGraph graph { get; set; }
        public int id { get; private set; }
        public float posX { get; set; }
        public float posY { get; set; }
        public EventTypeInfo eventTypeInfo { get; set; }
        public string eventName;
        public bool hideEvents;
        public List<TargetChecker> targetCheckerList { get; private set; } = new List<TargetChecker>(); 
        public List<IPort> conditions;
        public List<IPort> outputs;
        IEnumerable<IPort> IActionNode.outputPorts => outputs;
        IEnumerable<IPort> IActionNode.inputPorts => conditions;
        IDictionary<string, object> IActionNode.consts => targetCheckerList.ToDictionary(t => $"target[{t.targetIndex}]", t => (object)t);

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
        IActionNode ISerializableNode.ToActionNode() => ToActionNode();

        public int id;
        public float posX;
        public float posY;
        public string eventName;
        public bool hideEvents;
        public List<SerializableTargetChecker> targetCheckerList = new List<SerializableTargetChecker>();
    }
}