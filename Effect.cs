using System;
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
            return (buff != null ? buff.instanceID.ToString() : string.Empty) + "Effect" + Array.IndexOf(card.define.effects, this) + time.getEventName(game.triggers);
        }
    }
    [Serializable]
    public class GeneratedEffect : IEffect
    {
        #region 方法
        public GeneratedEffect(ActionValueRef condition, TargetChecker[] targetCheckers, ActionNode action, string[] tags)
        {
            this.condition = condition;
            this.targetCheckers = targetCheckers;
            this.action = action;
            this.tags = tags;
        }
        public GeneratedEffect(ActionNode action, string[] tags) : this(null, new TargetChecker[0], action, tags)
        {
        }
        public GeneratedEffect(ActionNode action) : this(null, new TargetChecker[0], action, new string[0])
        {
        }
        public bool checkCondition(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            if (condition == null)
                return true;
            var task = game.doActionAsync(card, buff, eventArg, condition.action);
            if (task.IsCompleted)
            {
                object[] returnValues = task.Result;
                if (returnValues[condition.index] is bool b)
                    return b;
                else
                    throw new InvalidCastException(returnValues[condition.index] + "不是真值类型");
            }
            else
                throw new InvalidOperationException("不能在条件中调用需要等待的动作");
        }
        public bool checkTarget(IGame game, ICard card, IBuff buff, IEventArg eventArg, out string invalidMsg)
        {
            invalidMsg = null;
            if (targetCheckers == null || targetCheckers.Length < 1)
                return true;
            foreach (var targetChecker in targetCheckers)
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
        public Task execute(IGame game, ICard card, IBuff buff, IEventArg eventArg)
        {
            return game.doActionsAsync(card, buff, eventArg, action);
        }
        #endregion
        #region 属性字段
        public string[] tags { get; }
        ActionValueRef condition { get; }
        TargetChecker[] targetCheckers { get; set; }
        ActionNode action { get; }
        #endregion
    }
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
        public string defineName { get; set; }
        public ActionNode[] branches { get; set; }
        public ActionValueRef[] inputs { get; set; }
        public object[] consts { get; set; }
    }
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
    public class IntegerBinaryOperationActionDefine : ActionDefine
    {
        public IntegerBinaryOperationActionDefine()
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
                    type = typeof(BinaryOperator)
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
            BinaryOperator op = (BinaryOperator)constValues[0];
            switch (op)
            {
                case BinaryOperator.add:
                    return Task.FromResult(new object[] { ((int[])args[0]).Sum(a => a) });
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
    public enum BinaryOperator
    {
        add,
        sub,
        mul,
        div
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
                            inputList.Add(new ValueDefine()
                            {
                                name = string.IsNullOrEmpty(attribute.paramName) ? "Value" : attribute.paramName,
                                type = paramInfo.ParameterType
                            });
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
        public ActionNodeParamAttribute(string paramName, bool isConst = false)
        {
            this.paramName = paramName;
            this.isConst = isConst;
        }
        public string paramName { get; }
        public bool isConst { get; }
    }
}