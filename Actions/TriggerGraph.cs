using System;
using System.Collections.Generic;
namespace TouhouCardEngine
{
    [Serializable]
    public class TriggerGraph
    {
        #region 公有方法
        public TriggerGraph(string eventName, ActionValueRef condition, TargetChecker[] targetCheckers, ActionNode action)
        {
            this.eventName = eventName;
            this.condition = condition;
            targetCheckerList.AddRange(targetCheckers);
            this.action = action;
        }
        public TriggerGraph(string eventName, ActionValueRef condition, ActionNode action) : this(eventName, condition, new TargetChecker[0], action)
        {
        }
        public TriggerGraph() : this(string.Empty, null, new TargetChecker[0], null)
        {
        }
        public void traverse(Action<ActionNode> action)
        {
            if (action == null)
                return;
            if (condition != null)
                condition.traverse(action);
            if (targetCheckerList != null && targetCheckerList.Count > 0)
            {
                for (int i = 0; i < targetCheckerList.Count; i++)
                {
                    if (targetCheckerList[i] == null)
                        continue;
                    targetCheckerList[i].traverse(action);
                }
            }
            if (this.action != null)
                this.action.traverse(action);
        }
        #endregion
        public string eventName;
        public ActionValueRef condition;
        public List<TargetChecker> targetCheckerList = new List<TargetChecker>();
        public ActionNode action;
    }
    [Serializable]
    public class SerializableTrigger
    {
        #region 公有方法
        #region 构造方法
        public SerializableTrigger(TriggerGraph trigger)
        {
            eventName = trigger.eventName;
            condition = new SerializableActionValueRef(trigger.condition);
            targetCheckerList = trigger.targetCheckerList.ConvertAll(t => new SerializableTargetChecker(t));
            actionId = trigger.action.id;
            trigger.action.traverse(a => actionList.Add(new SerializableActionNode(a)));
        }
        #endregion
        public TriggerGraph toTrigger()
        {
            Dictionary<int, ActionNode> actionNodeDict = new Dictionary<int, ActionNode>();
            return new TriggerGraph(eventName,
                condition.toActionValueRef(actionList, actionNodeDict),
                targetCheckerList.ConvertAll(t => t.toTargetChecker(actionList, actionNodeDict)).ToArray(),
                SerializableActionNode.toActionNodeGraph(actionId, actionList, actionNodeDict));
        }
        #endregion
        #region 属性字段
        public string eventName;
        public SerializableActionValueRef condition;
        public List<SerializableTargetChecker> targetCheckerList = new List<SerializableTargetChecker>();
        public int actionId;
        public List<SerializableActionNode> actionList = new List<SerializableActionNode>();
        #endregion
    }
}