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
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            eventName = trigger.eventName;
            if (trigger.condition != null)
            {
                condition = new SerializableActionValueRef(trigger.condition);
                if (trigger.condition.action != null)
                {
                    trigger.condition.action.traverse(a =>
                    {
                        if (a != null)
                            actionList.Add(new SerializableActionNode(a));
                    });
                }
            }
            else
                condition = null;
            if (trigger.targetCheckerList != null)
            {
                for (int i = 0; i < trigger.targetCheckerList.Count; i++)
                {
                    if (trigger.targetCheckerList[i] == null)
                        continue;
                    targetCheckerList.Add(new SerializableTargetChecker(trigger.targetCheckerList[i]));
                    if (trigger.targetCheckerList[i].condition != null && trigger.targetCheckerList[i].condition.action != null)
                    {
                        trigger.targetCheckerList[i].condition.action.traverse(a =>
                        {
                            if (a != null)
                                actionList.Add(new SerializableActionNode(a));
                        });
                    }
                }
            }
            if (trigger.action != null)
            {
                actionId = trigger.action.id;
                trigger.action.traverse(a =>
                {
                    if (a != null)
                        actionList.Add(new SerializableActionNode(a));
                });
            }
            else
                actionId = 0;
        }
        #endregion
        public TriggerGraph toTrigger()
        {
            Dictionary<int, ActionNode> actionNodeDict = new Dictionary<int, ActionNode>();
            TriggerGraph trigger = new TriggerGraph();
            trigger.eventName = eventName;
            if (condition != null)
            {
                try
                {
                    trigger.condition = condition.toActionValueRef(actionList, actionNodeDict);
                }
                catch (Exception e)
                {
                    throw new FormatException("反序列化触发器条件失败", e);
                }
            }
            else
                trigger.condition = null;
            for (int i = 0; i < targetCheckerList.Count; i++)
            {
                if (targetCheckerList[i] == null)
                    continue;
                try
                {
                    trigger.targetCheckerList.Add(targetCheckerList[i].toTargetChecker(actionList, actionNodeDict));
                }
                catch (Exception e)
                {
                    throw new FormatException("反序列化触发器目标条件" + i + "失败", e);
                }
            }
            if (actionId != 0)
            {
                try
                {
                    trigger.action = SerializableActionNode.toActionNodeGraph(actionId, actionList, actionNodeDict);
                }
                catch (Exception e)
                {
                    throw new FormatException("反序列化触发器动作失败", e);
                }
            }
            else
                trigger.action = null;
            return trigger;
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