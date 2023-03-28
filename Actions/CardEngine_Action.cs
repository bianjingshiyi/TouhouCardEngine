using System;
using System.Collections.Generic;
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
        public async Task runActions(Flow flow, ControlInput inputPort)
        {
            await flow.Run(inputPort);
        }
        public async Task runActions(Flow flow, ControlOutput outputPort)
        {
            await flow.Run(outputPort);
        }
        public async Task<T> getValue<T>(Flow flow, ValueInput input)
        {
            var value = await flow.getValue(input);
            if (value is T result)
                return result;
            return default;
        }
        public Task<object> getValue(Flow flow, ValueInput input)
        {
            return getValue<object>(flow, input);
        }
        public async Task<T> getValue<T>(Flow flow, ValueOutput output)
        {
            var value = await flow.getValue(output);
            if (value is T result)
                return result;
            return default;
        }
        public Task<object> getValue(Flow flow, ValueOutput output)
        {
            return getValue<object>(flow, output);
        }
        #endregion
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