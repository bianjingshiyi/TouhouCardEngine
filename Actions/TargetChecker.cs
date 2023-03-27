using System;
using System.Collections.Generic;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class TargetChecker : ITraversable
    {
        #region 公有方法
        public TargetChecker(string targetType, int targetIndex, string invalidMsg)
        {
            this.targetType = targetType;
            this.targetIndex = targetIndex;
            this.errorTip = invalidMsg;
        }
        public TargetChecker() : this(string.Empty, -1, string.Empty)
        {
        }
        public void traverse(Action<IActionNode> action, HashSet<IActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<IActionNode>();
            var condition = trigger?.getTargetConditionPort(targetIndex);
            condition?.traverse(action, traversedActionNodeSet);
        }
        public bool isValidTarget(IGame game, ICard card, IBuff buff, IEventArg eventArg, out string invalidMsg)
        {
            var condition = trigger?.getTargetConditionPort(targetIndex);
            if (condition == null || condition.getConnectedOutputPort() == null)
            {
                invalidMsg = null;
                return true;
            }
            var task = game.getValue<bool>(card, buff, eventArg, condition);
            if (task.IsCompleted)
            {
                bool returnValue = task.Result;
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
                throw new InvalidOperationException("不能在条件中调用需要等待的动作");
        }
        #endregion
        public TriggerEntryNode trigger;
        public string targetType;
        public int targetIndex;
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
            targetIndex = targetChecker.targetIndex;
            errorTip = targetChecker.errorTip;
        }
        #endregion
        public TargetChecker toTargetChecker()
        {
            return new TargetChecker(targetType, targetIndex, errorTip);
        }
        #endregion
        #region 属性字段
        public string targetType;
        [Obsolete]
        public SerializableActionValueRef condition;
        public int targetIndex;
        public string errorTip;
        #endregion
    }
}