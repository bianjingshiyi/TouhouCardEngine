using System;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public class TargetChecker
    {
        #region 公有方法
        public TargetChecker(string targetType, ActionValueRef condition, string invalidMsg)
        {
            this.targetType = targetType;
            this.condition = condition;
            this.errorTip = invalidMsg;
        }
        public TargetChecker() : this(string.Empty, null, string.Empty)
        {
        }
        public void traverse(Action<ActionNode> action, HashSet<ActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<ActionNode>();
            if (condition != null)
                condition.traverse(action, traversedActionNodeSet);
        }
        public bool isValidTarget(IGame game, ICard card, IBuff buff, IEventArg eventArg, out string invalidMsg)
        {
            if (condition == null || condition.action == null)
            {
                invalidMsg = null;
                return true;
            }
            var task = game.doActionAsync(card, buff, eventArg, condition.action);
            if (task.IsCompleted)
            {
                object[] returnValues = game.doActionAsync(card, buff, eventArg, condition.action).Result;
                if (returnValues[condition.index] is bool returnValue)
                {
                    if (!returnValue)
                    {
                        //有条件没有通过，不是合法目标
                        invalidMsg = errorTip;
                        return false;
                    }
                    else
                    {
                        //是合法目标
                        invalidMsg = null;
                        return true;
                    }
                }
                else
                    throw new InvalidCastException(returnValues[condition.index] + "不是真值类型");
            }
            else
                throw new InvalidOperationException("不能在条件中调用需要等待的动作");
        }
        #endregion
        public string targetType;
        public ActionValueRef condition;
        public string errorTip;
    }
    [Serializable]
    public class SerializableTargetChecker
    {
        #region 公有方法
        #region 构造方法
        public SerializableTargetChecker(TargetChecker targetChecker)
        {
            if (targetChecker == null)
                throw new ArgumentNullException(nameof(targetChecker));
            targetType = targetChecker.targetType;
            condition = targetChecker.condition != null ? new SerializableActionValueRef(targetChecker.condition) : null;
            errorTip = targetChecker.errorTip;
        }
        #endregion
        public TargetChecker toTargetChecker(List<SerializableActionNode> actionNodeList, Dictionary<int, ActionNode> actionNodeDict)
        {
            return new TargetChecker(
                targetType,
                condition != null ? condition.toActionValueRef(actionNodeList, actionNodeDict) : null,
                errorTip);
        }
        #endregion
        #region 属性字段
        public string targetType;
        public SerializableActionValueRef condition;
        public string errorTip;
        #endregion
    }
}