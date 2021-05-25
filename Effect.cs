using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class GeneratedEffect : IEffect
    {
        #region 方法
        public GeneratedEffect(string[] piles, ActionNode onEnable, ActionNode onDisable, TriggerGraph[] triggers, string[] tags)
        {
            _pileList.AddRange(piles);
            _onEnableAction = onEnable;
            _onDisableAction = onDisable;
            _triggerList.AddRange(triggers);
            _tagList.AddRange(tags);
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
        public void onEnable(IGame game, ICard card, IBuff buff)
        {
            if (onEnableAction != null)
                game.doActionsAsync(card, buff, null, onEnableAction);
            foreach (var graph in triggerList)
            {
                string triggerName = getEffectName(game, card, buff, graph.eventName);
                game.logger.log("Effect", card + "注册触发器" + triggerName);
                Trigger trigger = new Trigger(args =>
                {
                    return game.doActionsAsync(card, buff, args.OfType<IEventArg>().FirstOrDefault(), graph.action);
                }, name: triggerName);
                card.setProp(game, triggerName, trigger);
                game.triggers.register(graph.eventName, trigger);
            }
        }
        public void onDisable(IGame game, ICard card, IBuff buff)
        {
            foreach (var graph in triggerList)
            {
                string triggerName = getEffectName(game, card, buff, graph.eventName);
                game.logger.log("Effect", card + "注销触发器" + triggerName);
                Trigger trigger = card.getProp<Trigger>(game, triggerName);
                card.setProp(game, triggerName, null);
                game.triggers.remove(graph.eventName, trigger);
            }
            if (onDisableAction != null)
                game.doActionsAsync(card, buff, null, onDisableAction);
        }
        public bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg eventArg)
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
        public bool checkTarget(IGame game, ICard card, IBuff buff, IEventArg eventArg, out string invalidMsg)
        {
            TriggerGraph trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
            {
                invalidMsg = null;
                if (trigger.targetCheckers == null || trigger.targetCheckers.Length < 1)
                    return true;
                foreach (var targetChecker in trigger.targetCheckers)
                {
                    var task = game.doActionAsync(card, buff, eventArg, targetChecker.condition.action);
                    if (task.IsCompleted)
                    {
                        object[] returnValues = game.doActionAsync(card, buff, eventArg, targetChecker.condition.action).Result;
                        if (returnValues[targetChecker.condition.index] is bool b)
                        {
                            if (b == false)
                            {
                                invalidMsg = targetChecker.invalidMsg;
                                return false;
                            }
                        }
                        else
                            throw new InvalidCastException(returnValues[targetChecker.condition.index] + "不是真值类型");
                    }
                    else
                        throw new InvalidOperationException("不能在条件中调用需要等待的动作");
                }
                return true;
            }
            else
            {
                invalidMsg = null;
                return false;
            }
        }
        public Task execute(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            TriggerGraph trigger = triggerList.FirstOrDefault(t => t.eventName == game.triggers.getName(eventArg));
            if (trigger != null)
                return game.doActionsAsync(card, buff, eventArg, trigger.action);
            else
                return Task.CompletedTask;
        }
        private string getEffectName(IGame game, ICard card, IBuff buff, string eventName)
        {
            return (buff != null ? buff.instanceID.ToString() : string.Empty) +
                "Effect" + Array.IndexOf(card.define.getEffects(), this) + eventName;
        }
        #endregion
        #region 属性字段
        public List<string> pileList => _pileList;
        List<string> _pileList = new List<string>();
        public ActionNode onEnableAction => _onEnableAction;
        ActionNode _onEnableAction;
        public ActionNode onDisableAction => _onDisableAction;
        ActionNode _onDisableAction;
        public List<TriggerGraph> triggerList => _triggerList;
        List<TriggerGraph> _triggerList = new List<TriggerGraph>();
        public List<string> tagList => _tagList;
        List<string> _tagList = new List<string>();
        #endregion
    }
    [Serializable]
    public class TriggerGraph
    {
        public TriggerGraph(string eventName, ActionValueRef condition, TargetChecker[] targetCheckers, ActionNode action)
        {
            this.eventName = eventName;
            this.condition = condition;
            this.targetCheckers = targetCheckers;
            this.action = action;
        }
        public TriggerGraph(string eventName, ActionValueRef condition, ActionNode action) : this(eventName, condition, new TargetChecker[0], action)
        {
        }
        public TriggerGraph() : this(string.Empty, null, new TargetChecker[0], null)
        {
        }
        public string eventName { get; set; }
        public ActionValueRef condition { get; set; }
        public TargetChecker[] targetCheckers { get; set; }
        public ActionNode action { get; set; }
    }
    [Serializable]
    public class TargetChecker
    {
        public TargetChecker(string targetName, ActionValueRef condition, string invalidMsg)
        {
            this.targetName = targetName;
            this.condition = condition;
            this.invalidMsg = invalidMsg;
        }
        public TargetChecker() : this(string.Empty, null, string.Empty)
        {
        }
        public string targetName { get; set; }
        public ActionValueRef condition { get; set; }
        public string invalidMsg { get; set; }
    }
    /// <summary>
    /// 单个动作的数据结构。
    /// 由于要方便编辑器统一进行操作更改和存储，这个数据结构不允许多态。
    /// 这个数据结构必须同时支持多种类型的语句，比如赋值，分支，循环，返回，方法调用。
    /// 所以这里有两个很矛盾的地方，
    /// </summary>
    [Serializable]
    public sealed class ActionNode
    {
        public ActionNode(string defineName, ActionValueRef[] inputs, object[] consts, ActionNode[] branches)
        {
            _defineName = defineName;
            _branchList.AddRange(branches);
            _inputList.AddRange(inputs);
            _constList.AddRange(consts);
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
        public ActionNode(string defineName, object[] consts) : this(defineName, new ActionValueRef[0], consts, new ActionNode[0])
        {
        }
        public ActionNode(string defineName) : this(defineName, new ActionValueRef[0], new object[0], new ActionNode[0])
        {
        }
        public ActionNode() : this(string.Empty, new ActionValueRef[0], new object[0], new ActionNode[0])
        {
        }
        public string defineName => _defineName;
        string _defineName;
        public List<ActionNode> branchList => _branchList;
        List<ActionNode> _branchList = new List<ActionNode>();
        public List<ActionValueRef> inputList => _inputList;
        List<ActionValueRef> _inputList = new List<ActionValueRef>();
        public List<object> constList => _constList;
        List<object> _constList = new List<object>();
    }
    [Serializable]
    public class ActionValueRef
    {
        public ActionValueRef(ActionNode action, int index)
        {
            this.action = action;
            this.index = index;
        }
        public ActionValueRef(ActionNode action) : this(action, 0)
        {
        }
        public ActionNode action { get; }
        public int index { get; }
    }
    public abstract class ActionDefine
    {
        #region 方法
        public static Task<ActionDefine[]> loadDefinesFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => loadDefinesFromAssemblies(assemblies));
        }
        /// <summary>
        /// 通过反射的方式加载所有目标程序集中的动作定义，包括派生的动作定义和反射方法生成的动作定义。
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static ActionDefine[] loadDefinesFromAssemblies(Assembly[] assemblies)
        {
            List<ActionDefine> defineList = new List<ActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    //是ActionDefine的子类，不是抽象类，具有零参数构造函数
                    if (type.IsSubclassOf(typeof(ActionDefine)) &&
                        !type.IsAbstract &&
                        type.GetConstructor(new Type[0]) is ConstructorInfo constructor)
                    {
                        defineList.Add((ActionDefine)constructor.Invoke(new object[0]));
                    }
                }
            }
            defineList.AddRange(MethodActionDefine.loadMethodsFromAssemblies(assemblies));
            return defineList.ToArray();
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
    public class BuiltinActionDefine : ActionDefine
    {
        #region 方法
        public BuiltinActionDefine(Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action) : base()
        {
            this.action = action;
            consts = new ValueDefine[0];
        }
        public override Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, object[] args, object[] constValues)
        {
            return action(game, card, buff, eventArg, args, constValues);
        }
        #endregion
        Func<IGame, ICard, IBuff, IEventArg, object[], object[], Task<object[]>> action { get; }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
    }
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
                    type = typeof(IntegerOperator)
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
        [ActionNodeMethod("Except")]
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
        [ActionNodeMethod("GetCount")]
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
        public static Task<MethodActionDefine[]> loadMethodsFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => Task.FromResult(loadMethodsFromAssemblies(assemblies)));
        }
        public static MethodActionDefine[] loadMethodsFromAssemblies(Assembly[] assemblies)
        {
            List<MethodActionDefine> defineList = new List<MethodActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (method.GetCustomAttribute<ActionNodeMethodAttribute>() is ActionNodeMethodAttribute attribute)
                        {
                            defineList.Add(new MethodActionDefine(attribute.methodName, method));
                        }
                    }
                }
            }
            return defineList.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo">必须是静态方法</param>
        public MethodActionDefine(string methodName, MethodInfo methodInfo)
        {
            if (!methodInfo.IsStatic)
                throw new ArgumentException("Target method must be static", nameof(methodInfo));
            this.methodName = methodName;
            _methodInfo = methodInfo;
            List<ValueDefine> outputList = new List<ValueDefine>();
            //首先如果方法返回类型为void或者Task，视为无返回值，否则有返回值
            ActionNodeParamAttribute attribute;
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType != typeof(Task))
            {
                attribute = methodInfo.ReturnParameter.GetCustomAttribute<ActionNodeParamAttribute>();
                string returnValueName = attribute?.paramName;
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
                attribute = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                //如果参数是out参数，那么它是一个输出
                if (paramInfo.IsOut)
                {
                    outputList.Add(new ValueDefine()
                    {
                        name = attribute != null && !string.IsNullOrEmpty(attribute.paramName) ? attribute.paramName : "Value",
                        type = paramInfo.ParameterType
                    });
                }
                else
                {
                    if (attribute != null)
                    {
                        //用特性指定了一定是输入
                        if (attribute.isConst)
                        {
                            //用特性指定是常量
                            constList.Add(new ValueDefine()
                            {
                                name = string.IsNullOrEmpty(attribute.paramName) ? "Value" : attribute.paramName,
                                type = paramInfo.ParameterType
                            });
                        }
                        else
                        {
                            //用特性指定是输入
                            if (attribute.isParams)
                            {
                                inputList.Add(new ValueDefine()
                                {
                                    name = string.IsNullOrEmpty(attribute.paramName) ? "Value" : attribute.paramName,
                                    type = paramInfo.ParameterType.GetElementType(),
                                    isParams = true
                                });
                            }
                            else
                            {
                                inputList.Add(new ValueDefine()
                                {
                                    name = string.IsNullOrEmpty(attribute.paramName) ? "Value" : attribute.paramName,
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
                if (task.GetType() == typeof(Task))
                    return null;
                else
                    return new object[] { (object)((dynamic)task).Result };
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
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
        MethodInfo _methodInfo;
        ParameterInfo[] _paramsInfo;
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ActionNodeMethodAttribute : Attribute
    {
        public ActionNodeMethodAttribute(string methodName)
        {
            this.methodName = methodName;
        }
        public string methodName { get; }
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