using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public partial class CardEngine
    {
        #region 公有方法
        public void addActionDefine(string name, ActionDefine actionDefine)
        {
            if (actionDefineDict.ContainsKey(name))
                throw new InvalidOperationException("已存在名为" + name + "的动作定义");
            actionDefineDict.Add(name, actionDefine);
        }
        public ActionDefine getActionDefine(string name)
        {
            if (actionDefineDict.ContainsKey(name))
                return actionDefineDict[name];
            else
                return null;
        }
        /// <summary>
        /// 执行一串动作链表
        /// </summary>
        /// <param name="card"></param>
        /// <param name="buff"></param>
        /// <param name="eventArg"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public Task doActionsAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode actions)
        {
            return doActionsAsync(card, buff, eventArg, actions, new Scope());
        }

        public async Task doActionsAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode actions, Scope scope)
        {
            ActionNode curAction = actions;
            while (curAction != null)
            {
                await doActionAsyncImp(card, buff, eventArg, curAction, scope);
                if (curAction.branches != null && curAction.branches.Length > 0)
                    curAction = curAction.branches[0];
                else
                    break;
            }
        }
        /// <summary>
        /// 执行动作和它的所有后续动作。
        /// 如果动作输出值中有输出到返回节点的连线，将该值加入到返回值中。
        /// </summary>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="scope">作用域。</param>
        /// <param name="returnValueRefs">所有返回值的引用。</param>
        /// <returns>返回值列表。</returns>
        public async Task<object[]> getActionsReturnValueAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode actions, Scope scope, ReturnValueRef[] returnValueRefs)
        {
            if (scope == null)
            {
                scope = new Scope();
            }
            ActionNode curAction = actions;
            scope.returnValueRefs = returnValueRefs;
            while (curAction != null)
            {
                await doActionAsyncImp(card, buff, eventArg, curAction, scope);
                if (curAction.branches != null && curAction.branches.Length > 0)
                    curAction = curAction.branches[0];
                else
                    break;
            }
            return scope.returns;
        }
        /// <summary>
        /// 执行单个动作并返回指定变量值，这是一个会等待动作的异步方法。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="buff"></param>
        /// <param name="eventArg"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public Task<object[]> doActionAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode action)
        {
            return doActionAsyncImp(card, buff, eventArg, action, new Scope());
        }
        public Task<object[]> doActionAsync(Card card, Buff buff, EventArg eventArg, ActionNode action, Scope scope = null)
        {
            if (scope == null)
                scope = new Scope();
            return doActionAsyncImp(card, buff, eventArg, action, scope);
        }
        /// <summary>
        /// 获取动作的返回值
        /// </summary>
        /// <param name="card">执行动作环境的卡牌</param>
        /// <param name="buff">执行动作环境的增益</param>
        /// <param name="eventArg">执行动作环境的事件</param>
        /// <param name="action">执行的动作</param>
        /// <param name="valueIndex">如果不指定返回值的索引，则会从返回值中查找指定类型的返回结果并返回第一个匹配的，否则将尝试将指定索引的返回值转化为指定类型</param>
        /// <typeparam name="T">指定返回值的类型</typeparam>
        /// <returns></returns>
        public async Task<T> getActionReturnValueAsync<T>(Card card, Buff buff, EventArg eventArg, ActionNode action, int valueIndex = -1, Scope scope = null)
        {
            object[] results = await doActionAsync(card, buff, eventArg, action, scope == null ? new Scope() : scope);
            if (valueIndex < 0)
            {
                for (int i = 0; i < results.Length; i++)
                {
                    if (results[i] is T result)
                        return result;
                }
                throw new InvalidCastException("执行结果{" + string.Join(",", results) + "}中不包含类型为" + typeof(T).Name + "的返回值");
            }
            else
            {
                if (results[valueIndex] is T result)
                    return result;
                else
                    throw new InvalidCastException("无法将" + results[valueIndex] + "转化为" + typeof(T).Name);
            }
        }
        #endregion
        #region 私有方法
        private Task<object[]> doActionAsyncImp(ICard card, IBuff buff, IEventArg eventArg, ActionNode action, Scope scope)
        {
            return doActionAsyncImp(new ActionContext()
            {
                card = card,
                buff = buff,
                eventArg = eventArg,
                action = action,
                scope = scope
            });
        }
        private async Task<object[]> doActionAsyncImp(ActionContext context)
        {
            ICard card = context.card;
            IBuff buff = context.buff;
            IEventArg eventArg = context.eventArg;
            Scope scope = context.scope;
            ActionNode action = context.action;

            //作用域记录当前正在执行的动作节点，这样就不必将节点传递给某些将输出值写入局部变量的方法中
            scope.actionNode = action;
            ActionDefine define = getActionDefine(action.defineName);
            // 获取所有输入值作为参数。
            object[] args = await getActionArgs(context);
            try
            {
                //执行动作
                var result = await define.execute(this, card, buff, eventArg, scope, args, action.consts.ToArray());
                //输出变量
                if (action.regVar != null && action.regVar.Length > 0)
                {
                    for (int i = 0; i < action.regVar.Length; i++)
                    {
                        if (action.regVar[i] && i < result.Length)
                        {
                            scope.localVarDict[scope.getLocalVarName(i)] = result[i];
                        }
                    }
                }

                scope.SendReturnValue(context, result);
                return result;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException targetInvocationException)
                    e = targetInvocationException.InnerException;
                logger.logError("Game", "执行动作" + action + "发生异常：" + e);
                throw e;
            }
        }
        #endregion
        /// <summary>
        /// 获取要执行的一个动作节点的所有需要的参数。
        /// </summary>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="action">要执行的动作节点。</param>
        /// <param name="scope">当前作用域。</param>
        /// <returns>所有的参数。</returns>
        private async Task<object[]> getActionArgs(ActionContext context)
        {
            ActionNode action = context.action;

            //获取动作定义
            ActionDefine define = getActionDefine(action.defineName);
            //从环境中取参数值
            ValueDefine[] actionOutputs = define.getActionOutputs();
            ValueDefine[] valueInputs = define.getValueInputs();
            object[] args = new object[valueInputs.Length + actionOutputs.Length];
            for (int i = 0; i < actionOutputs.Length; i++)
            {
                //是动作节点参数，从分支里面取实际参数
                args[i] = action.branches[1 + i];
            }
            for (int i = 0; i < valueInputs.Length; i++)
            {
                ValueDefine valueInput = valueInputs[i];
                if (valueInput.isParams)
                {
                    //是变长参数，所有当前index之后的值塞进一个数组
                    List<object> paramList = new List<object>();
                    foreach (ActionValueRef valueRef in action.inputs.Skip(i))
                    {
                        if (valueRef == null)
                            continue;
                        object arg = await getActionArg(context, valueRef, valueInput);
                        paramList.Add(arg);
                    }
                    Array argArray = Array.CreateInstance(valueInput.type, paramList.Count);
                    Array.Copy(paramList.ToArray(), argArray, paramList.Count);
                    args[actionOutputs.Length + i] = argArray;
                }
                else
                {
                    object arg = null;
                    if (action.inputs[i] is ActionValueRef valueRef)
                    {
                        arg = await getActionArg(context, valueRef, valueInput);
                    }
                    args[actionOutputs.Length + i] = arg;
                }
            }
            return args;
        }
        /// <summary>
        /// 获取动作参数。
        /// </summary>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="curAction">当前正在执行的动作。</param>
        /// <param name="valueRef">动作参数的引用对象。</param>
        /// <param name="scope">当前作用域。</param>
        /// <param name="define">值定义。</param>
        /// <returns>获取到的动作参数。</returns>
        private async Task<object> getActionArg(ActionContext context, ActionValueRef valueRef, ValueDefine define)
        {
            IEventArg eventArg = context.eventArg;
            Scope scope = context.scope;

            object arg;
            if (define.isOut)
            {
                //输入参数不要求执行就可以从环境中获得
                arg = getArgFromLocal(context, valueRef.actionNodeId, valueRef.index, define);
            }
            else if (valueRef.action != null)
            {
                arg = await getArgFromActionNode(context, valueRef.action, valueRef.index, define);
            }
            else if (!string.IsNullOrEmpty(valueRef.eventVarName))
            {
                arg = eventArg != null ? eventArg.getVar(valueRef.eventVarName) : scope.getLocalVar(valueRef.eventVarName);
            }
            else if (valueRef.actionNodeId != 0)
            {
                arg = getArgFromLocal(context, valueRef.actionNodeId, valueRef.index, define);
            }
            else
            {
                arg = scope.getArg(valueRef.argIndex);
            }
            return arg;
        }
        /// <summary>
        /// 从作用域的局部变量处获取参数。
        /// </summary>
        /// <param name="targetActionId">目标动作节点ID。</param>
        /// <param name="outputIndex">目标动作节点的输出值位置。</param>
        /// <param name="scope">当前作用域。</param>
        /// <param name="curAction">当前正在执行的动作。（仅用于输出日志）</param>
        /// <param name="define">值定义。（仅用于输出日志）</param>
        /// <returns>获取到的参数。</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private object getArgFromLocal(ActionContext context, int targetActionId, int outputIndex, ValueDefine define)
        {
            ActionNode action = context.action;
            Scope scope = context.scope;

            if (!scope.tryGetLoacalVar(targetActionId, outputIndex, out object arg))
            {
                string msg = "从局部变量中获取" + action + "的参数" + define.name + "失败";
                logger.logError(msg);
                throw new KeyNotFoundException(msg);
            }
            return arg;
        }
        /// <summary>
        /// 从输入端的动作节点中获取参数。
        /// </summary>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="curAction">当前正在执行的动作节点。</param>
        /// <param name="action">要获取输入值的动作节点。</param>
        /// <param name="outputIndex">要获取输入值的动作节点的输出位置。</param>
        /// <param name="scope">作用域对象。</param>
        /// <param name="define">值定义。（仅用于输出日志）</param>
        /// <returns>获取到的参数。</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        private async Task<object> getArgFromActionNode(ActionContext context, ActionNode action, int outputIndex, ValueDefine define)
        {
            Scope scope = context.scope;

            object arg;
            ActionDefine actionDefine = getActionDefine(action.defineName);
            if (actionDefine != null &&//是输出型参数
                outputIndex < actionDefine.getValueOutputs().Length &&
                actionDefine.getValueOutputAt(outputIndex) is ValueDefine output &&
                output.isOut)
            {
                arg = getArgFromLocal(context, action.id, outputIndex, define);
            }
            else if (outputIndex < action.regVar.Length &&//或者已经将结果注册局部变量
                action.regVar[outputIndex] &&
                scope.tryGetLoacalVar(action.id, outputIndex, out arg))
            {
                //已经在条件中获取到了参数
            }
            else
            {
                try
                {
                    var newContext = new ActionContext(context);
                    newContext.action = action;

                    object[] result = await doActionAsyncImp(newContext);
                    //执行其他动作节点来获取返回值会改变作用域当前正在执行的节点，将它恢复回来
                    scope.actionNode = context.action;
                    arg = outputIndex < result.Length ? result[outputIndex] : null;
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException targetInvocationException)
                        e = targetInvocationException.InnerException;
                    logger.logError("获取" + context.action + "的参数" + (define != null ? define.name : outputIndex.ToString()) + "失败：" + e);
                    throw e;
                }
            }
            return arg;
        }
        Dictionary<string, ActionDefine> actionDefineDict { get; } = new Dictionary<string, ActionDefine>();
    }
    [Serializable]
    public class Scope
    {
        #region 公有方法
        #region 构造方法
        public Scope(Scope parentScope)
        {
            this.parentScope = parentScope;
        }
        public Scope() : this(null)
        {
        }
        #endregion
        public void setOutParamValue(int index, object value)
        {
            localVarDict[getLocalVarName(index)] = value;
        }
        public string getLocalVarName(int index)
        {
            return getLocalVarName(actionNode.id, index);
        }
        public string getLocalVarName(int actionNodeId, int index)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(actionNodeId.ToString());
            sb.Append("_");
            sb.Append(index.ToString());
            return string.Intern(sb.ToString());
        }
        public object getLocalVar(int actionNodeId, int index)
        {
            return getLocalVar(getLocalVarName(actionNodeId, index));
        }
        public object getLocalVar(string varName)
        {
            Scope scope = this;
            while (scope != null)
            {
                if (scope.localVarDict.TryGetValue(varName, out object value))
                    return value;
                scope = scope.parentScope;
            }
            return null;
        }
        public bool tryGetLoacalVar(int actionNodeId, int index, out object value)
        {
            Scope scope = this;
            while (scope != null)
            {
                if (scope.localVarDict.TryGetValue(getLocalVarName(actionNodeId, index), out value))
                    return true;
                scope = scope.parentScope;
            }
            value = null;
            return false;
        }
        public object getArg(int argIndex)
        {
            tryGetArg(argIndex, out object arg);
            return arg;
        }
        public bool tryGetArg(int argIndex, out object arg)
        {
            arg = null;
            Scope curScope = this;
            while (curScope != null)
            {
                if (curScope.args != null)
                {
                    if (argIndex < curScope.args.Length)
                    {
                        arg = curScope.args[argIndex];
                        return true;
                    }
                    else if (curScope.consts != null && argIndex < curScope.args.Length + curScope.consts.Length)
                    {
                        arg = curScope.consts[argIndex - args.Length];
                        return true;
                    }
                }
                curScope = curScope.parentScope;
            }
            return false;
        }
        public void SendReturnValue(ActionContext context, object[] results)
        {
            Scope curScope = this;
            while (curScope != null)
            {
                if (returnValueRefs != null && returnValueRefs.Length > 0)
                {
                    for (int i = 0; i < returnValueRefs.Length; i++)
                    {
                        var returnRef = returnValueRefs[i];
                        if (returnRef == null ||
                            returnRef.valueRef == null ||
                            context.action != returnRef.valueRef.action)
                            continue;

                        var valueRef = returnRef.valueRef;
                        int returnIndex = returnRef.returnIndex;
                        if (returns == null)
                        {
                            returns = new object[returnIndex + 1];
                        }
                        else if (returns.Length <= returnIndex)
                        {
                            object[] newArray = new object[returnIndex + 1];
                            Array.Copy(returns, newArray, returns.Length);
                            returns = newArray;
                        }
                        returns[returnIndex] = results[valueRef.index];
                    }
                }
                curScope = curScope.parentScope;
            }
        }
        #endregion
        #region 属性字段
        //public Stack<ActionNode> actionNodeStack = new Stack<ActionNode>();
        public Scope parentScope = null;
        public ActionNode actionNode = null;
        public object[] args = null;
        public object[] consts = null;
        public Dictionary<string, object> localVarDict = new Dictionary<string, object>();
        public object[] returns = null;
        public ReturnValueRef[] returnValueRefs = null;
        #endregion
    }
    /// <summary>
    /// 当以同步的方式调用异步方法但是返回Task并未执行完成时抛出
    /// </summary>
    [Serializable]
    public class TaskNotCompleteException : Exception
    {
        public TaskNotCompleteException() { }
        public TaskNotCompleteException(string message) : base(message) { }
        public TaskNotCompleteException(string message, Exception inner) : base(message, inner) { }
        protected TaskNotCompleteException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    public class ActionContext
    {
        public ICard card;
        public IBuff buff;
        public IEventArg eventArg;
        public ActionNode action;
        public Scope scope;
        public ActionContext()
        {

        }
        public ActionContext(ActionContext other)
        {
            card = other.card;
            buff = other.buff;
            eventArg = other.eventArg;
            action = other.action;
            scope = other.scope;
        }
    }
}