using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
using UnityEngine;

namespace TouhouCardEngine
{
    public partial class CardEngine
    {
        #region 公有方法
        public void addActionDefine(ActionReference actRef, ActionDefine actionDefine)
        {
            addActionDefine(actRef.cardPoolId, actRef.defineId, actionDefine);
        }
        public void addActionDefine(long cardPoolId, int actionId, ActionDefine actionDefine)
        {
            if (!actionDefineDict.TryGetValue(cardPoolId, out var cardPoolActions))
            {
                cardPoolActions = new Dictionary<int, ActionDefine>();
                actionDefineDict.Add(cardPoolId, cardPoolActions);
            }

            if (cardPoolActions.ContainsKey(actionId))
                throw new InvalidOperationException($"已存在卡池为{cardPoolId}，ID为{actionId}的动作定义");
            cardPoolActions.Add(actionId, actionDefine);
        }
        public ActionDefine getActionDefine(ActionReference actRef)
        {
            return getActionDefine(actRef.cardPoolId, actRef.defineId);
        }
        public ActionDefine getActionDefine(long cardPoolId, int actionId)
        {
            if (actionDefineDict.TryGetValue(cardPoolId, out var cardPool))
            {
                if (cardPool.TryGetValue(actionId, out ActionDefine actionDefine))
                    return actionDefine;
            }
            return default;
        }
        public async Task runActions(Flow flow, ControlInput inputPort)
        {
            pushFlow(flow);
            await flow.Run(inputPort);
            popFlow();
        }
        public async Task runActions(Flow flow, ControlOutput outputPort)
        {
            pushFlow(flow);
            await flow.Run(outputPort);
            popFlow();
        }
        public async Task<T> getValue<T>(Flow flow, ValueInput input)
        {
            pushFlow(flow);
            var value = await flow.getValue<T>(input);
            popFlow();
            return value;
        }
        public async Task<object> getValue(Flow flow, ValueInput input)
        {
            pushFlow(flow);
            var value = await flow.getValue(input);
            popFlow();
            return value;
        }
        public async Task<T> getValue<T>(Flow flow, ValueOutput output)
        {
            pushFlow(flow);
            var value = await flow.getValue<T>(output);
            popFlow();
            return value;
        }
        public async Task<object> getValue(Flow flow, ValueOutput output)
        {
            pushFlow(flow);
            var value = await flow.getValue(output);
            popFlow();
            return value;
        }
        #endregion

        #region 私有方法
        private void pushFlow(Flow flow)
        {
            _flowStack.Push(flow);
        }
        private void popFlow()
        {
            _flowStack.Pop();
        }
        #endregion
        public Flow currentFlow => _flowStack.Count > 0 ? _flowStack.Peek() : null;
        Dictionary<long, Dictionary<int, ActionDefine>> actionDefineDict { get; } = new Dictionary<long, Dictionary<int, ActionDefine>>();
        Stack<Flow> _flowStack = new Stack<Flow>();
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