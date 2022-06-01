using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    [Serializable]
    public class GeneratedEffect : IPassiveEffect
    {
        #region 公有方法
        #region 构造方法
        public GeneratedEffect(IEnumerable<string> piles, ActionNode onEnable, ActionNode onDisable, TriggerGraph[] triggers, string[] tags)
        {
            if (piles != null)
                pileList.AddRange(piles);
            onEnableAction = onEnable;
            onDisableAction = onDisable;
            if (triggers != null)
                triggerList.AddRange(triggers);
            if (tags != null)
                tagList.AddRange(tags);
        }
        public GeneratedEffect(string[] piles, ActionNode onEnable, ActionNode onDisable, TriggerGraph[] triggers) : this(piles, onEnable, onDisable, triggers, new string[0])
        {
        }
        /// <summary>
        /// 构造一个主动效果
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="targetCheckers"></param>
        /// <param name="action"></param>
        /// <param name="tags"></param>
        public GeneratedEffect(ActionValueRef condition, TargetChecker[] targetCheckers, ActionNode action, string[] tags) : this(new string[0], null, null, new TriggerGraph[]
        {
            new TriggerGraph("ActiveEvent", condition, targetCheckers, action)
        }, tags)
        {
        }
        /// <summary>
        /// 构造一个无条件无目标的主动效果
        /// </summary>
        /// <param name="action"></param>
        /// <param name="tags"></param>
        public GeneratedEffect(ActionNode action, string[] tags) : this(null, new TargetChecker[0], action, tags)
        {
        }
        /// <summary>
        /// 构造一个无条件无目标的主动效果
        /// </summary>
        /// <param name="action"></param>
        public GeneratedEffect(ActionNode action) : this(null, new TargetChecker[0], action, new string[0])
        {
        }
        /// <summary>
        /// 构造一个包含一个触发器的被动效果
        /// </summary>
        /// <param name="piles"></param>
        /// <param name="trigger"></param>
        public GeneratedEffect(IEnumerable<string> piles, TriggerGraph trigger) : this(piles.ToArray(), null, null, new TriggerGraph[] { trigger }, new string[0])
        {

        }
        public GeneratedEffect() : this(null, null, null, null, null)
        {
        }
        #endregion
        public virtual async Task onEnable(IGame game, ICard card, IBuff buff)
        {
            if (onEnableAction != null)
                await game.doActionsAsync(card, buff, null, onEnableAction);
            foreach (var graph in triggerList)
            {
                string triggerName = getEffectName(game, card, buff, graph.eventName);
                game.logger.log("Effect", card + "注册触发器" + triggerName);
                Trigger trigger = new Trigger(args =>
                {
                    return game.doActionsAsync(card, buff, args.OfType<IEventArg>().FirstOrDefault(), graph.action);
                }, name: triggerName);
                await card.setProp(game, triggerName, trigger);
                game.triggers.register(graph.eventName, trigger);
            }
        }
        public virtual async Task onDisable(IGame game, ICard card, IBuff buff)
        {
            foreach (var graph in triggerList)
            {
                string triggerName = getEffectName(game, card, buff, graph.eventName);
                game.logger.log("Effect", card + "注销触发器" + triggerName);
                Trigger trigger = card.getProp<Trigger>(game, triggerName);
                await card.setProp(game, triggerName, null);
                game.triggers.remove(graph.eventName, trigger);
            }
            if (onDisableAction != null)
                await game.doActionsAsync(card, buff, null, onDisableAction);
        }
        public virtual bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            TriggerGraph trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
            {
                if (trigger.condition == null)
                    return true;
                var task = game.doActionAsync(card, buff, eventArg, trigger.condition.action);
                if (task.IsCompleted)
                {
                    object[] returnValues = task.Result;
                    if (returnValues[trigger.condition.index] is bool b)
                        return b;
                    else
                        throw new InvalidCastException(returnValues[trigger.condition.index] + "不是真值类型");
                }
                else
                    throw new InvalidOperationException("不能在条件中调用需要等待的动作");
            }
            else
                return false;
        }
        /// <summary>
        /// 检查目标卡牌是否是效果的合法目标
        /// </summary>
        /// <param name="game"></param>
        /// <param name="card">目标卡牌</param>
        /// <param name="buff"></param>
        /// <param name="eventArg"></param>
        /// <param name="invalidMsg"></param>
        /// <returns></returns>
        public virtual bool checkTarget(IGame game, ICard card, IBuff buff, IEventArg eventArg, out string invalidMsg)
        {
            TriggerGraph trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
            {
                invalidMsg = null;
                //如果触发不包含任何目标，那么目标肯定不是合法目标
                if (trigger.targetCheckerList == null || trigger.targetCheckerList.Count < 1)
                    return false;
                foreach (var targetChecker in trigger.targetCheckerList)
                {
                    //检查目标条件
                    if (!targetChecker.isValidTarget(game, card, buff, eventArg, out invalidMsg))
                    {
                        //有条件没有通过，不是合法目标
                        return false;
                    }
                }
                //有目标并且没有条件不通过或者没有条件，返回真
                return true;
            }
            else
            {
                invalidMsg = null;
                return false;
            }
        }
        public virtual Task execute(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            TriggerGraph trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
                return game.doActionsAsync(card, buff, eventArg, trigger.action);
            else
                return Task.CompletedTask;
        }
        public virtual EffectPropertyInfo[] getPropInfos()
        {
            return fieldInfos;
        }
        public T getProp<T>(string name)
        {
            return (T)getProp(name);
        }
        public virtual object getProp(string name)
        {
            if (name == nameof(pileList))
                return pileList;
            else if (name == nameof(onEnableAction))
                return onEnableAction;
            else if (name == nameof(onDisableAction))
                return onDisableAction;
            else if (name == nameof(triggerList))
                return triggerList;
            else if (name == nameof(tagList))
                return tagList;
            else
            {
                if (propDict.TryGetValue(name, out object value))
                    return value;
                else
                    return null;
            }
        }
        public virtual void setProp(string name, object value)
        {
            if (name == nameof(pileList))
                pileList = value as PileNameCollection;
            else if (name == nameof(onEnableAction))
                onEnableAction = value as ActionNode;
            else if (name == nameof(onDisableAction))
                onDisableAction = value as ActionNode;
            else if (name == nameof(triggerList))
                triggerList = value as TriggerCollection;
            else if (name == nameof(tagList))
                tagList = value as EffectTagCollection;
            else
                propDict[name] = value;
        }
        /// <summary>
        /// 遍历效果中的动作节点
        /// </summary>
        /// <param name="action"></param>
        public void traverseActionNode(Action<ActionNode> action)
        {
            if (action == null)
                return;
            foreach (var propInfo in getPropInfos())
            {
                object value = getProp(propInfo.name);
                if (value == null)
                    continue;
                if (value is ActionNode actionNode)
                {
                    actionNode.traverse(action);
                }
                else if (value is IEnumerable<ActionNode> actionNodeCol)
                {
                    foreach (var actionNodeEle in actionNodeCol)
                    {
                        if (actionNodeEle == null)
                            continue;
                        actionNodeEle.traverse(action);
                    }
                }
                else if (value is ActionValueRef valueRef)
                {
                    valueRef.traverse(action);
                }
                else if (value is IEnumerable<ActionValueRef> valueRefCol)
                {
                    foreach (var valueRefEle in valueRefCol)
                    {
                        if (valueRefEle == null)
                            continue;
                        valueRefEle.traverse(action);
                    }
                }
                else if (value is TargetChecker targetChecker)
                {
                    targetChecker.traverse(action);
                }
                else if (value is IEnumerable<TargetChecker> targetCheckerCol)
                {
                    foreach (var targetCheckerEle in targetCheckerCol)
                    {
                        if (targetCheckerEle == null)
                            continue;
                        targetCheckerEle.traverse(action);
                    }
                }
                else if (value is TriggerGraph trigger)
                {
                    trigger.traverse(action);
                }
                else if (value is IEnumerable<TriggerGraph> triggerCol)
                {
                    foreach (var triggerEle in triggerCol)
                    {
                        if (triggerEle == null)
                            continue;
                        triggerEle.traverse(action);
                    }
                }
            }
        }
        #endregion
        #region 私有方法
        private string getEffectName(IGame game, ICard card, IBuff buff, string eventName)
        {
            return (buff != null ? buff.instanceID.ToString() : string.Empty) +
                "Effect" + Array.IndexOf(card.define.getEffects(), this) + eventName;
        }
        #endregion
        #region 属性字段
        string[] IPassiveEffect.piles => pileList.ToArray();
        public PileNameCollection pileList = new PileNameCollection();
        public ActionNode onEnableAction;
        public ActionNode onDisableAction;
        public TriggerCollection triggerList = new TriggerCollection();
        public EffectTagCollection tagList = new EffectTagCollection();
        public Dictionary<string, object> propDict = new Dictionary<string, object>();
        static EffectPropertyInfo[] fieldInfos = new EffectPropertyInfo[]
        {
            new EffectPropertyInfo(typeof(PileNameCollection),nameof(pileList)),
            new EffectPropertyInfo(typeof(ActionNode),nameof(onEnableAction)),
            new EffectPropertyInfo(typeof(ActionNode),nameof(onDisableAction)),
            new EffectPropertyInfo(typeof(TriggerCollection),nameof(triggerList)),
            new EffectPropertyInfo(typeof(EffectTagCollection),nameof(tagList)),
        };
        #endregion
    }
    public class SerializableEffect
    {
        #region 公有方法
        #region 构造方法
        public SerializableEffect(GeneratedEffect generatedEffect)
        {
            pileList = generatedEffect.pileList;
            onEnableRootActionNodeId = generatedEffect.onEnableAction.id;
            onDisableRootActionNodeId = generatedEffect.onDisableAction.id;
            triggerList = generatedEffect.triggerList.ConvertAll(t => new SerializableTrigger(t));
            tagList = generatedEffect.tagList;
            propDict = generatedEffect.propDict;
            //onEnable
            generatedEffect.onEnableAction.traverse(a => actionNodeList.Add(new SerializableActionNode(a)));
            //onDisable
            generatedEffect.onDisableAction.traverse(a => actionNodeList.Add(new SerializableActionNode(a)));
        }
        #endregion
        public GeneratedEffect toGeneratedEffect()
        {
            Dictionary<int, ActionNode> actionNodeDict = new Dictionary<int, ActionNode>();
            return new GeneratedEffect(pileList,
                SerializableActionNode.toActionNodeGraph(onEnableRootActionNodeId, actionNodeList, actionNodeDict),
                SerializableActionNode.toActionNodeGraph(onDisableRootActionNodeId, actionNodeList, actionNodeDict),
                triggerList.ConvertAll(t => t.toTrigger()).ToArray(),
                tagList.ToArray())
            {
                propDict = propDict
            };
        }
        #endregion
        #region 属性字段
        public PileNameCollection pileList = new PileNameCollection();
        public int onEnableRootActionNodeId;
        public int onDisableRootActionNodeId;
        public List<SerializableTrigger> triggerList = new List<SerializableTrigger>();
        public EffectTagCollection tagList = new EffectTagCollection();
        public Dictionary<string, object> propDict = new Dictionary<string, object>();
        public List<SerializableActionNode> actionNodeList = new List<SerializableActionNode>();
        #endregion
    }
}