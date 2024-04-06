using System;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;

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
        public void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<Node>();
            if (condition != null)
                condition.traverse(action, traversedActionNodeSet);
            if (targetCheckerList != null && targetCheckerList.Count > 0)
            {
                for (int i = 0; i < targetCheckerList.Count; i++)
                {
                    if (targetCheckerList[i] == null)
                        continue;
                    targetCheckerList[i].traverse(action, traversedActionNodeSet);
                }
            }
            if (this.action != null && !traversedActionNodeSet.Contains(this.action))
                this.action.traverse(action, traversedActionNodeSet);
        }
        #endregion
        public string eventName;
        public ActionValueRef condition;
        public List<TargetChecker> targetCheckerList = new List<TargetChecker>();
        public ActionNode action;
    }
}