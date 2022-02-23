using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class TriggerTime
    {
        public abstract string getEventName(ITriggerManager manager);

    }
    public class Before<T> : TriggerTime where T : IEventArg
    {
        public override string getEventName(ITriggerManager manager)
        {
            return manager.getNameBefore<T>();
        }
    }
    public class On<T> : TriggerTime where T : IEventArg
    {
        public override string getEventName(ITriggerManager manager)
        {
            return manager.getName<T>();
        }
    }
    public class After<T> : TriggerTime where T : IEventArg
    {
        public override string getEventName(ITriggerManager manager)
        {
            return manager.getNameAfter<T>();
        }
    }
    /// <summary>
    /// 效果
    /// </summary>
    public abstract class Effect : ITriggerEffect
    {
        /// <summary>
        /// 效果的作用时机
        /// </summary>
        [Obsolete]
        public virtual string trigger { get; } = null;
        public abstract TriggerTime[] triggerTimes { get; }
        string[] ITriggerEffect.events
        {
            get { return new string[] { trigger }; }
        }
        string[] ITriggerEffect.getEvents(ITriggerManager manager)
        {
            List<string> eventList = new List<string>();
            if (!string.IsNullOrEmpty(trigger))
                eventList.Add(trigger);
            foreach (var triggerTime in triggerTimes)
            {
                eventList.Add(triggerTime.getEventName(manager));
            }
            return eventList.ToArray();
        }
        /// <summary>
        /// 效果的作用域
        /// </summary>
        public abstract string pile { get; }
        string[] IPassiveEffect.piles
        {
            get { return new string[] { pile }; }
        }
        /// <summary>
        /// 检查效果能否发动
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public abstract bool checkCondition(CardEngine engine, Player player, Card card, Buff buff, object[] vars);
        bool ITriggerEffect.checkCondition(IGame game, ICard card, IBuff buff, object[] vars)
        {
            return checkCondition(game as CardEngine, null, card as Card, buff as Buff, vars);
        }
        /// <summary>
        /// 检查效果的目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        public abstract bool checkTargets(CardEngine engine, Player player, Card card, Buff buff, object[] targets);
        bool ITriggerEffect.checkTargets(IGame game, ICard card, IBuff buff, object[] vars, object[] targets)
        {
            return checkTargets(game as CardEngine, null, card as Card, buff as Buff, targets);
        }
        public abstract Task execute(CardEngine engine, Player player, Card card, Buff buff, object[] vars, object[] targets);
        Task ITriggerEffect.execute(IGame game, ICard card, IBuff buff, object[] vars, object[] targets)
        {
            return execute(game as CardEngine, null, card as Card, buff as Buff, vars, targets);
        }
        /// <summary>
        /// 发动效果
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="player"></param>
        /// <param name="card"></param>
        /// <param name="targets"></param>
        [Obsolete]
        public virtual void execute(CardEngine engine, Player player, Card card, Buff buff, object[] targets)
        {
            execute(engine, player, card, buff, new object[0], targets);
        }
        /// <summary>
        /// 检查效果目标是否合法
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="card"></param>
        /// <param name="targetCards"></param>
        /// <returns></returns>
        [Obsolete]
        public virtual bool checkTarget(CardEngine engine, Player player, Card card, Buff buff, Card[] targetCards)
        {
            return checkTargets(engine, player, card, buff, targetCards);
        }
        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="engine"></param>
        [Obsolete]
        public virtual void execute(CardEngine engine, Player player, Card card, Buff buff, Card[] targetCards)
        {
            execute(engine, player, card, buff, targetCards.Cast<object>().ToArray());
        }
        public async Task onEnable(IGame game, ICard card, IBuff buff)
        {
            foreach (TriggerTime time in triggerTimes)
            {
                Trigger trigger = new Trigger(args =>
                {
                    if ((this as ITriggerEffect).checkCondition(game, card, buff, args))
                        return (this as ITriggerEffect).execute(game, card, buff, args, new object[0]);
                    else
                        return Task.CompletedTask;
                });
                await card.setProp(game, getTriggerName(game, card, buff, time), trigger);
                game.triggers.register(time.getEventName(game.triggers), trigger);
            }
        }
        public Task onDisable(IGame game, ICard card, IBuff buff)
        {
            foreach (TriggerTime time in triggerTimes)
            {
                Trigger trigger = card.getProp<Trigger>(game, getTriggerName(game, card, buff, time));
                game.triggers.remove(trigger);
            }
            return Task.CompletedTask;
        }
        private string getTriggerName(IGame game, ICard card, IBuff buff, TriggerTime time)
        {
            return (buff != null ? buff.instanceID.ToString() : string.Empty) + "Effect" + Array.IndexOf(card.define.getEffects(), this) + time.getEventName(game.triggers);
        }
    }
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
                    if (targetChecker.condition.action == null)
                        continue;
                    var task = game.doActionAsync(card, buff, eventArg, targetChecker.condition.action);
                    if (task.IsCompleted)
                    {
                        object[] returnValues = game.doActionAsync(card, buff, eventArg, targetChecker.condition.action).Result;
                        if (returnValues[targetChecker.condition.index] is bool b)
                        {
                            if (b == false)
                            {
                                //有条件没有通过，不是合法目标
                                invalidMsg = targetChecker.errorTip;
                                return false;
                            }
                        }
                        else
                            throw new InvalidCastException(returnValues[targetChecker.condition.index] + "不是真值类型");
                    }
                    else
                        throw new InvalidOperationException("不能在条件中调用需要等待的动作");
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
    [Serializable]
    public class PileNameCollection : List<string>
    {
    }
    [Serializable]
    public class TriggerCollection : List<TriggerGraph>
    {
    }
    [Serializable]
    public class EffectTagCollection : List<string>
    {
    }
    public class EffectPropertyInfo
    {
        public EffectPropertyInfo(Type type, string name)
        {
            this.type = type;
            this.name = name;
        }
        public string name;
        public Type type;
    }
    [Serializable]
    public class TriggerGraph
    {
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
        public string eventName;
        public ActionValueRef condition;
        public List<TargetChecker> targetCheckerList = new List<TargetChecker>();
        public ActionNode action;
    }
    [Serializable]
    public class TargetChecker
    {
        public TargetChecker(string targetType, ActionValueRef condition, string invalidMsg)
        {
            this.targetType = targetType;
            this.condition = condition;
            this.errorTip = invalidMsg;
        }
        public TargetChecker() : this(string.Empty, null, string.Empty)
        {
        }
        public string targetType;
        public ActionValueRef condition;
        public string errorTip;
    }
    /// <summary>
    /// 单个动作的数据结构。
    /// 由于要方便编辑器统一进行操作更改和存储，这个数据结构不允许多态。
    /// 这个数据结构必须同时支持多种类型的语句，比如赋值，分支，循环，返回，方法调用。
    /// </summary>
    [Serializable]
    public sealed class ActionNode
    {
        #region 方法
        public ActionNode(string defineName, ActionValueRef[] inputs, object[] consts, ActionNode[] branches)
        {
            this.defineName = defineName;
            this.branches = branches;
            this.inputs = inputs;
            this.consts = consts;
        }
        public ActionNode(string defineName, ActionValueRef[] inputs, object[] consts) : this(defineName, inputs, consts, new ActionNode[0])
        {
        }
        public ActionNode(string defineName, ActionValueRef[] inputs, ActionNode next) : this(defineName, inputs, new object[0], new ActionNode[] { next })
        {
        }
        public ActionNode(string defineName, ActionValueRef[] inputs) : this(defineName, inputs, new object[0], new ActionNode[0])
        {
        }
        public ActionNode(string defineName, params object[] consts) : this(defineName, new ActionValueRef[0], consts, new ActionNode[0])
        {
        }
        public ActionNode(string defineName) : this(defineName, new ActionValueRef[0], new object[0], new ActionNode[0])
        {
        }
        public ActionNode() : this(string.Empty, new ActionValueRef[0], new object[0], new ActionNode[0])
        {
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(defineName);
            if (consts != null && consts.Length > 0)
            {
                sb.Append('<');
                for (int i = 0; i < consts.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(consts[i].ToString());
                }
                sb.Append('>');
            }
            sb.Append('(');
            if (inputs != null && inputs.Length > 0)
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(inputs[i].ToString());
                }
            }
            sb.Append("); ");
            return string.Intern(sb.ToString());
        }
        #endregion
        /// <summary>
        /// 用来区分不同动作节点的ID。
        /// </summary>
        /// <remarks>其实这个ID在逻辑上并没有什么特殊的作用，但是编辑器需要一个ID来保存对应的视图信息。</remarks>
        public int id;
        public string defineName;
        /// <summary>
        /// 该动作的后续动作，根据动作的类型不同可能有多个动作分支，比如条件结构，或者循环结构
        /// </summary>
        public ActionNode[] branches;
        /// <summary>
        /// 该动作引用的输入值
        /// </summary>
        public ActionValueRef[] inputs;
        public object[] consts;
        /// <summary>
        /// 用于标识返回值是否需要保存为局部变量
        /// </summary>
        public bool[] regVar;
    }
    [Serializable]
    public class ActionValueRef
    {
        #region 公有方法
        /// <summary>
        /// 返回指定索引返回值的构造器
        /// </summary>
        /// <param name="action"></param>
        /// <param name="index"></param>
        public ActionValueRef(ActionNode action, int index)
        {
            this.action = action;
            this.index = index;
        }
        public ActionValueRef(int actionNodeId, int index)
        {
            this.actionNodeId = actionNodeId;
            this.index = index;
        }
        public ActionValueRef(string eventVarName)
        {
            this.eventVarName = eventVarName;
        }
        /// <summary>
        /// 返回第一个返回值的构造器
        /// </summary>
        /// <param name="action"></param>
        public ActionValueRef(ActionNode action) : this(action, 0)
        {
        }
        /// <summary>
        /// 供序列化使用的默认构造器
        /// </summary>
        public ActionValueRef() : this(null, 0)
        {
        }
        public override string ToString()
        {
            string s;
            if (action != null)
            {
                s = action.ToString() + "[" + index + "]";
            }
            else if (actionNodeId != 0)
            {
                s = "{" + actionNodeId + "}[" + index + "]";
            }
            else if (!string.IsNullOrEmpty(eventVarName))
            {
                s = "{" + eventVarName + "}";
            }
            else
            {
                s = "null";
            }
            return string.Intern(s);
        }
        #endregion
        /// <summary>
        /// 引用的动作节点
        /// </summary>
        public ActionNode action;
        /// <summary>
        /// 引用的动作节点返回值索引
        /// </summary>
        public int index;
        /// <summary>
        /// 引用的动作节点ID
        /// </summary>
        public int actionNodeId;
        /// <summary>
        /// 事件变量名称
        /// </summary>
        public string eventVarName;
    }
    public abstract class ActionDefine
    {
        #region 方法
        public static Task<Dictionary<string, ActionDefine>> loadDefinesFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => loadDefinesFromAssemblies(assemblies));
        }
        /// <summary>
        /// 通过反射的方式加载所有目标程序集中的动作定义，包括派生的动作定义和反射方法生成的动作定义。
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static Dictionary<string, ActionDefine> loadDefinesFromAssemblies(Assembly[] assemblies)
        {
            Dictionary<string, ActionDefine> defineDict = new Dictionary<string, ActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    //是ActionDefine的子类，不是抽象类，具有零参数构造函数
                    if (type.IsSubclassOf(typeof(ActionDefine)) &&
                        !type.IsAbstract &&
                        type.GetConstructor(new Type[0]) is ConstructorInfo constructor)
                    {
                        ActionDefine actionDefine = (ActionDefine)constructor.Invoke(new object[0]);
                        string name = actionDefine.GetType().Name;
                        if (name.EndsWith("ActionDefine"))
                            name = name.Substring(0, name.Length - 12);
                        defineDict.Add(name, actionDefine);
                    }
                }
            }
            foreach (var pair in MethodActionDefine.loadMethodsFromAssemblies(assemblies))
            {
                defineDict.Add(pair.Key, pair.Value);
            }
            return defineDict;
        }
        public abstract Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues);
        #endregion
        #region 属性字段
        public abstract ValueDefine[] inputs { get; }
        public abstract ValueDefine[] consts { get; }
        public abstract ValueDefine[] outputs { get; }
        #endregion
    }
    public class ValueDefine
    {
        public Type type { get; set; }
        public string name { get; set; }
        public bool isParams { get; set; }
    }
    //public class BuiltinActionDefine : ActionDefine
    //{
    //    #region 方法
    //    public BuiltinActionDefine(Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action) : base()
    //    {
    //        this.action = action;
    //        consts = new ValueDefine[0];
    //    }
    //    public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
    //    {
    //        return action(game, card, buff, eventArg, args, constValues);
    //    }
    //    #endregion
    //    Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action { get; }
    //    public override ValueDefine[] inputs { get; }
    //    public override ValueDefine[] consts { get; }
    //    public override ValueDefine[] outputs { get; }
    //}
    public class IntegerOperationActionDefine : ActionDefine
    {
        public IntegerOperationActionDefine()
        {
            inputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(int),
                    isParams = true
                }
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "operator",
                    type = typeof(IntegerOperator)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "result",
                    type = typeof(int)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            IntegerOperator op = (IntegerOperator)constValues[0];
            switch (op)
            {
                case IntegerOperator.add:
                    return Task.FromResult(new object[] { ((int[])args[0]).Sum(a => a) });
                case IntegerOperator.sub:
                    int[] numbers = (int[])args[0];
                    int result = numbers[0];
                    for (int i = 1; i < numbers.Length; i++)
                    {
                        result -= numbers[i];
                    }
                    return Task.FromResult(new object[] { result });
                case IntegerOperator.mul:
                    numbers = (int[])args[0];
                    result = 1;
                    for (int i = 0; i < numbers.Length; i++)
                    {
                        result *= numbers[i];
                    }
                    return Task.FromResult(new object[] { result });
                case IntegerOperator.div:
                    numbers = (int[])args[0];
                    result = numbers[0];
                    for (int i = 1; i < numbers.Length; i++)
                    {
                        result /= numbers[i];
                    }
                    return Task.FromResult(new object[] { result });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class LogicOperationActionDefine : ActionDefine
    {
        public LogicOperationActionDefine()
        {
            inputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(bool),
                    isParams = true
                }
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "operator",
                    type = typeof(LogicOperator)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "result",
                    type = typeof(bool)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            LogicOperator op = (LogicOperator)constValues[0];
            switch (op)
            {
                case LogicOperator.not:
                    return Task.FromResult(new object[] { !(bool)args[0] });
                case LogicOperator.and:
                    foreach (var value in args.Cast<bool>())
                    {
                        if (value == false)
                            return Task.FromResult(new object[] { false });
                    }
                    return Task.FromResult(new object[] { true });
                case LogicOperator.or:
                    foreach (var value in args.Cast<bool>())
                    {
                        if (value == true)
                            return Task.FromResult(new object[] { true });
                    }
                    return Task.FromResult(new object[] { false });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class CollectionHelper
    {
        /// <summary>
        /// 除了...以外
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        [ActionNodeMethod("Except", "CollectionOperation")]
        [return: ActionNodeParam("Collection")]
        public static IEnumerable except([ActionNodeParam("Collection")] IEnumerable collection, [ActionNodeParam("Element", isParams: true)] object[] elements)
        {
            return collection.Cast<object>().Where(obj => !elements.Contains(obj));
        }
        /// <summary>
        /// 计算集合中元素数量
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        [ActionNodeMethod("GetCount", "CollectionOperation")]
        [return: ActionNodeParam("Collection")]
        public static int getCount([ActionNodeParam("Collection")] IEnumerable collection)
        {
            return collection.Cast<object>().Count();
        }
    }
    public class CompareActionDefine : ActionDefine
    {
        public CompareActionDefine()
        {
            inputs = new ValueDefine[2]
            {
                new ValueDefine()
                {
                    name = "A",
                    type = typeof(object)
                },
                new ValueDefine()
                {
                    name = "B",
                    type = typeof(object)
                }
            };
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "operator",
                    type = typeof(CompareOperator)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "result",
                    type = typeof(bool)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            CompareOperator op = (CompareOperator)constValues[0];
            switch (op)
            {
                case CompareOperator.equals:
                    return Task.FromResult(new object[] { args[0] == args[1] });
                case CompareOperator.unequals:
                    return Task.FromResult(new object[] { args[0] != args[1] });
                default:
                    throw new InvalidOperationException("未知的操作符" + op);
            }
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class IntegerConstActionDefine : ActionDefine
    {
        public IntegerConstActionDefine()
        {
            inputs = new ValueDefine[0];
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(int)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(int)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            return Task.FromResult(constValues);
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class StringConstActionDefine : ActionDefine
    {
        public StringConstActionDefine()
        {
            inputs = new ValueDefine[0];
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(string)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(string)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            return Task.FromResult(constValues);
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public class BooleanConstActionDefine : ActionDefine
    {
        public BooleanConstActionDefine()
        {
            inputs = new ValueDefine[0];
            consts = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(bool)
                }
            };
            outputs = new ValueDefine[1]
            {
                new ValueDefine()
                {
                    name = "value",
                    type = typeof(bool)
                }
            };
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            return Task.FromResult(constValues);
        }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
    public enum CompareOperator
    {
        equals,
        unequals
    }
    public enum IntegerOperator
    {
        add,
        sub,
        mul,
        div
    }
    public enum LogicOperator
    {
        not,
        and,
        or
    }
    /// <summary>
    /// 通过反射生成的方法动作定义，目标方法必须是静态方法，并且使用特性标记参数与返回信息。
    /// </summary>
    public class MethodActionDefine : ActionDefine
    {
        public static Task<Dictionary<string, MethodActionDefine>> loadMethodsFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => Task.FromResult(loadMethodsFromAssemblies(assemblies)));
        }
        public static Dictionary<string, MethodActionDefine> loadMethodsFromAssemblies(Assembly[] assemblies)
        {
            Dictionary<string, MethodActionDefine> defineDict = new Dictionary<string, MethodActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (method.GetCustomAttribute<ActionNodeMethodAttribute>() is ActionNodeMethodAttribute attribute)
                        {
                            defineDict.Add(attribute.methodName, new MethodActionDefine(attribute, method));
                        }
                    }
                }
            }
            return defineDict;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo">必须是静态方法</param>
        public MethodActionDefine(ActionNodeMethodAttribute attribute, MethodInfo methodInfo)
        {
            if (!methodInfo.IsStatic)
                throw new ArgumentException("Target method must be static", nameof(methodInfo));
            methodName = attribute.methodName;
            category = attribute.category;
            _methodInfo = methodInfo;
            List<ValueDefine> outputList = new List<ValueDefine>();
            //首先如果方法返回类型为void或者Task，视为无返回值，否则有返回值
            ActionNodeParamAttribute paramAttr;
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType != typeof(Task))
            {
                paramAttr = methodInfo.ReturnParameter.GetCustomAttribute<ActionNodeParamAttribute>();
                string returnValueName = paramAttr?.paramName;
                if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    //如果返回类型为Task<T>，则返回值类型视为T
                    outputList.Add(new ValueDefine()
                    {
                        name = string.IsNullOrEmpty(returnValueName) ? "Value" : returnValueName,
                        type = methodInfo.ReturnType.GetGenericArguments()[0]
                    });
                }
                else
                {
                    outputList.Add(new ValueDefine()
                    {
                        name = string.IsNullOrEmpty(returnValueName) ? "Value" : returnValueName,
                        type = methodInfo.ReturnType
                    });
                }
            }
            //分析参数设置输入和输出
            _paramsInfo = methodInfo.GetParameters();
            List<ValueDefine> inputList = new List<ValueDefine>();
            List<ValueDefine> constList = new List<ValueDefine>();
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                paramAttr = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                //如果参数是out参数，那么它是一个输出
                if (paramInfo.IsOut)
                {
                    outputList.Add(new ValueDefine()
                    {
                        name = paramAttr != null && !string.IsNullOrEmpty(paramAttr.paramName) ? paramAttr.paramName : "Value",
                        type = paramInfo.ParameterType
                    });
                }
                else
                {
                    if (paramAttr != null)
                    {
                        //用特性指定了一定是输入
                        if (paramAttr.isConst)
                        {
                            //用特性指定是常量
                            constList.Add(new ValueDefine()
                            {
                                name = string.IsNullOrEmpty(paramAttr.paramName) ? "Value" : paramAttr.paramName,
                                type = paramInfo.ParameterType
                            });
                        }
                        else
                        {
                            //用特性指定是输入
                            if (paramAttr.isParams)
                            {
                                inputList.Add(new ValueDefine()
                                {
                                    name = string.IsNullOrEmpty(paramAttr.paramName) ? "Value" : paramAttr.paramName,
                                    type = paramInfo.ParameterType.GetElementType(),
                                    isParams = true
                                });
                            }
                            else
                            {
                                inputList.Add(new ValueDefine()
                                {
                                    name = string.IsNullOrEmpty(paramAttr.paramName) ? "Value" : paramAttr.paramName,
                                    type = paramInfo.ParameterType,
                                    isParams = false
                                });
                            }
                        }
                    }
                    else if (!typeof(IGame).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(ICard).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //不是Game,Card,Buff,EventArg这种可以缺省的参数也一定是输入
                        inputList.Add(new ValueDefine()
                        {
                            name = "Value",
                            type = paramInfo.ParameterType
                        });
                    }
                }
            }
            //设置输入输出
            inputs = inputList.ToArray();
            consts = constList.ToArray();
            outputs = outputList.ToArray();
        }
        public override async Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            object[] paramters = new object[_paramsInfo.Length];
            int argIndex = 0;
            int constIndex = 0;
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                if (paramInfo.IsOut)
                {
                    //out参数输出留空
                    paramters[i] = null;
                }
                else if (paramInfo.GetCustomAttribute<ActionNodeParamAttribute>() is ActionNodeParamAttribute attribute)
                {
                    //指定了不能省略的参数
                    if (attribute.isConst)
                    {
                        paramters[i] = constValues[constIndex];
                        constIndex++;
                    }
                    else
                    {
                        paramters[i] = args[argIndex];
                        argIndex++;
                    }
                }
                else
                {
                    if (typeof(IGame).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        paramters[i] = game;
                    }
                    else if (typeof(ICard).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Card
                        paramters[i] = card;
                    }
                    else if (typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Buff
                        paramters[i] = buff;
                    }
                    else if (typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的EventArg
                        paramters[i] = eventArg;
                    }
                    else
                    {
                        //不是可以省略的类型
                        paramters[i] = args[argIndex];
                        argIndex++;
                    }
                }
            }
            object returnValue = _methodInfo.Invoke(null, paramters);
            if (returnValue is Task task)
            {
                await task;
                //返回Task则视为返回null，返回Task<T>则返回对应值
                if (task.GetType().GetProperty(nameof(Task<object>.Result)) is PropertyInfo propInfo)
                {
                    return new object[] { propInfo.GetValue(task) };
                }
                else
                    return null;
            }
            else
            {
                //不是Task，返回由返回值和out参数组成的数组
                List<object> outputList = new List<object>
                {
                    returnValue
                };
                for (int i = 0; i < _paramsInfo.Length; i++)
                {
                    var paramInfo = _paramsInfo[i];
                    if (paramInfo.IsOut)
                        outputList.Add(paramters[i]);
                }
                return outputList.ToArray();
            }
        }
        public string methodName { get; }
        public string category { get; }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
        MethodInfo _methodInfo;
        ParameterInfo[] _paramsInfo;
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ActionNodeMethodAttribute : Attribute
    {
        public ActionNodeMethodAttribute(string methodName, string category)
        {
            this.methodName = methodName;
            this.category = category;
        }
        public string methodName { get; }
        public string category { get; }
    }
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ActionNodeParamAttribute : Attribute
    {
        public ActionNodeParamAttribute(string paramName, bool isConst = false, bool isParams = false)
        {
            this.paramName = paramName;
            this.isConst = isConst;
            this.isParams = isParams;
        }
        public string paramName { get; }
        public bool isConst { get; }
        public bool isParams { get; }
    }
}