using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public class GeneratedEffect : IPassiveEffect
    {
        #region 公有方法
        #region 构造方法
        public GeneratedEffect(ActionGraph graph)
        {
            this.graph = graph;
        }
        public GeneratedEffect()
        {
        }
        #endregion
        public virtual async Task onEnable(IGame game, ICard card, IBuff buff)
        {
            if (onEnableAction != null)
            {
                var flow = new Flow(game, card, buff, null);
                await game.runActions(flow, onEnableAction);
            }
            foreach (var entryNode in triggerList)
            {
                string triggerName = getEffectName(card, buff, entryNode.eventName);
                game.logger.log("Effect", card + "注册触发器" + triggerName);
                Trigger trigger = new Trigger(
                    args =>
                    {
                        var condition = entryNode.getTriggerCondtionValuePort();
                        if (condition == null || condition.getConnectedOutputPort() == null)
                            return true;

                        var flow = new Flow(game, card, buff, args.OfType<EventArg>().FirstOrDefault());
                        Task<bool> task = game.getValue<bool>(flow, condition);

                        if (task.IsCompleted)
                            return task.Result;
                        else
                            throw new InvalidOperationException("无法在触发器条件中执行需要等待的动作");
                    },
                    args =>
                    {
                        var flow = new Flow(game, card, buff, args.OfType<IEventArg>().FirstOrDefault());
                        return game.runActions(flow, entryNode.getActionOutputPort());
                    }, name: triggerName);
                await card.setProp(game, triggerName, trigger);
                game.triggers.register(entryNode.eventName, trigger);
            }
            // 设置该Effect已被启用。
            string disableName = getEffectName(card, buff, "Disabled");
            await card.setProp(game, disableName, false);
        }
        public virtual async Task onDisable(IGame game, ICard card, IBuff buff)
        {
            foreach (var graph in triggerList)
            {
                string triggerName = getEffectName(card, buff, graph.eventName);
                game.logger.log("Effect", card + "注销触发器" + triggerName);
                Trigger trigger = card.getProp<Trigger>(game, triggerName);
                await card.setProp(game, triggerName, null);
                game.triggers.remove(graph.eventName, trigger);
            }
            // 设置该Effect已被禁用。
            string disableName = getEffectName(card, buff, "Disabled");
            await card.setProp(game, disableName, true);

            if (onDisableAction != null)
            {
                var flow = new Flow(game, card, buff, null);
                await game.runActions(flow, onDisableAction);
            }
        }
        public virtual bool isDisabled(IGame game, ICard card, IBuff buff)
        {
            string disableName = getEffectName(card, buff, "Disabled");
            return card.getProp<bool>(game, disableName);
        }
        public T getProp<T>(string name)
        {
            return (T)getProp(name);
        }
        public virtual object getProp(string name)
        {
            if (propDict.TryGetValue(name, out object value))
                return value;
            else
                return null;
        }
        public virtual void setProp(string name, object value)
        {
            propDict[name] = value;
        }
        /// <summary>
        /// 遍历效果中的动作节点
        /// </summary>
        /// <param name="action"></param>
        public void traverseActionNode(Action<Node> action)
        {
            if (action == null)
                return;
            foreach (var value in getTraversableProps())
            {
                if (value == null)
                    continue;
                if (value is ITraversable actionNode)
                {
                    actionNode.traverse(action);
                }
                else if (value is IEnumerable<ITraversable> actionNodeCol)
                {
                    foreach (var actionNodeEle in actionNodeCol)
                    {
                        if (actionNodeEle == null)
                            continue;
                        actionNodeEle.traverse(action);
                    }
                }
            }
        }
        public virtual void Init()
        {
            graph.createActionNode(GeneratedEffectData.defName, 0, 500);
            graph.createTriggerEntryNode("ActiveEvent", 0, 0);
        }
        public ActionNode getDataNode()
        {
            return graph?.findActionNode(GeneratedEffectData.defName);
        }
        public virtual bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            var trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
            {
                var condition = trigger.getTriggerCondtionValuePort();
                if (condition == null || condition.getConnectedOutputPort() == null)
                    return true;

                var flow = new Flow(game, card, buff, eventArg);
                var task = game.getValue<bool>(flow, condition);
                if (task.IsCompleted)
                {
                    return task.Result;
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
            var trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
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
            var trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
            {
                var flow = new Flow(game, card, buff, eventArg);
                return game.runActions(flow, trigger.getActionOutputPort());
            }
            return Task.CompletedTask;
        }
        #endregion
        #region 私有方法
        protected virtual IEnumerable<ITraversable> getTraversableProps()
        {
            if (onEnableAction != null)
                yield return onEnableAction;
            if (onDisableAction != null)
                yield return onDisableAction;
            foreach (var trigger in triggerList)
            {
                yield return trigger;
            }
        }
        protected string getEffectName(ICard card, IBuff buff, string eventName)
        {
            return (buff != null ? buff.instanceID.ToString() : string.Empty) +
                "Effect" + Array.IndexOf(card.define.getEffects(), this) + eventName;
        }
        #endregion
        #region 属性字段
        public string name;
        public ActionGraph graph { get; set; }
        public Dictionary<string, object> propDict = new Dictionary<string, object>();
        protected virtual PileNameCollection pileList => getDataNode()?.getConst<PileNameCollection>(GeneratedEffectData.pileListName);
        public virtual ControlOutput onEnableAction => getDataNode()?.getOutputPort<ControlOutput>(GeneratedEffectData.enableActionName);
        public virtual ControlOutput onDisableAction => getDataNode()?.getOutputPort<ControlOutput>(GeneratedEffectData.disableActionName);
        public virtual IEnumerable<TriggerEntryNode> triggerList => graph?.nodes?.OfType<TriggerEntryNode>();

        public string[] piles => pileList?.ToArray() ?? Array.Empty<string>();
        #endregion
    }

    [Serializable]
    public class SerializableEffect
    {
        public SerializableEffect(GeneratedEffect effect)
        {
            name = effect.name;
            typeName = effect.GetType().Name;
            propDict = effect.propDict;
            graph = new SerializableActionNodeGraph(effect.graph);
        }

        public GeneratedEffect toGeneratedEffect(ActionDefineFinder defineFinder, EventTypeInfoFinder eventFinder, TypeFinder typeFinder = null)
        {
            GeneratedEffect effect = !string.IsNullOrEmpty(typeName) && typeFinder != null && typeFinder(typeName) is Type type ? 
                Activator.CreateInstance(type) as GeneratedEffect : 
                null;
            effect.name = name;
            effect.propDict = propDict;
            effect.graph = graph.toActionGraph(defineFinder, eventFinder);
            return effect;
        }
        #region 属性字段
        public string name;
        public string typeName;
        public Dictionary<string, object> propDict;
        public SerializableActionNodeGraph graph;

        [Obsolete]
        public PileNameCollection pileList;
        [Obsolete]
        public int onEnableRootActionNodeId;
        [Obsolete]
        public int onDisableRootActionNodeId;
        [Obsolete]
        public List<SerializableActionNode> onEnableActionList = new List<SerializableActionNode>();
        [Obsolete]
        public List<SerializableActionNode> onDisalbeActionList = new List<SerializableActionNode>();
        [Obsolete]
        public List<SerializableTrigger> triggerList;
        [Obsolete]
        public EffectTagCollection tagList;
        #endregion
    }
}