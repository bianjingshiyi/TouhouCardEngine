using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class TargetChecker
    {
        #region 公有方法
        public TargetChecker(string targetType, string invalidMsg)
        {
            this.targetType = targetType;
            errorTip = invalidMsg;
        }
        public TargetChecker() : this(string.Empty, string.Empty)
        {
        }
        public int getIndex()
        {
            return node.getTargetCheckerIndex(this);
        }
        public void traverse(Action<Node> action, HashSet<Node> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<Node>();
            var condition = node?.getTargetConditionPort(getIndex());
            condition?.traverse(action, traversedActionNodeSet);
        }
        public bool isValidTarget(FlowEnv env, out string invalidMsg)
        {
            var condition = node?.getTargetConditionPort(getIndex());
            if (condition == null || condition.getConnectedOutputPort() == null)
            {
                invalidMsg = null;
                return true;
            }
            var flow = new Flow(env);
            var task = env.game.getValue<bool>(flow, condition);
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
        public ITargetCheckerNode node;
        public string targetType;
        public string errorTip;
    }
    public interface ITargetCheckerNode
    {
        int getTargetCheckerIndex(TargetChecker checker);
        ValueInput getTargetConditionPort(int index);
    }
    [Serializable]
    [BsonIgnoreExtraElements]
    public class SerializableTargetChecker
    {
        #region 公有方法
        #region 构造方法
        public SerializableTargetChecker(TargetChecker targetChecker)
        {
            if (targetChecker == null)
                throw new ArgumentNullException(nameof(targetChecker));
            targetType = targetChecker.targetType;
            errorTip = targetChecker.errorTip;
        }
        #endregion
        public TargetChecker toTargetChecker()
        {
            return new TargetChecker(targetType, errorTip);
        }
        #endregion
        #region 属性字段
        public string targetType;
        [Obsolete]
        public SerializableActionValueRef condition;
        public string errorTip;
        #endregion
    }
}