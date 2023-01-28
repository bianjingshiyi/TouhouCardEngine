using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.GraphicsBuffer;
using TouhouCardEngine.Interfaces;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    [Serializable]
    public sealed class FunctionNode
    {
        #region 公有方法
        #region 构造方法
        public FunctionNode(string functionName, ActionNode action = null, ReturnValueRef[] returns = null, object[] consts = null)
        {
            this.functionName = functionName;
            this.action = action;
            this.returns = returns ?? new ReturnValueRef[0];
            this.consts = consts ?? new object[0];
        }
        public FunctionNode() : this(null, null, null, null)
        {
        }
        #endregion
        public void traverse(Action<ActionNode> action, HashSet<ActionNode> traversedActionNodeSet = null)
        {
            if (action == null)
                return;
            if (traversedActionNodeSet == null)
                traversedActionNodeSet = new HashSet<ActionNode>();
            //遍历输入
            if (returns != null && returns.Length > 0)
            {
                for (int i = 0; i < returns.Length; i++)
                {
                    if (returns[i] == null)
                        continue;
                    returns[i].valueRef.traverse(action, traversedActionNodeSet);
                }
            }
            //遍历常量
            if (consts != null && consts.Length > 0)
            {
                for (int i = 0; i < consts.Length; i++)
                {
                    if (consts[i] == null)
                        continue;
                    if (consts[i] is FunctionNode childActionConditionNode)
                    {
                        childActionConditionNode.traverse(action, traversedActionNodeSet);
                    }
                    else if (consts[i] is ActionValueRef valueRef)
                    {
                        valueRef.traverse(action, traversedActionNodeSet);
                    }
                    else if (consts[i] is TargetChecker targetChecker)
                    {
                        targetChecker.traverse(action, traversedActionNodeSet);
                    }
                    else if (consts[i] is TriggerGraph trigger)
                    {
                        trigger.traverse(action, traversedActionNodeSet);
                    }
                }
            }
            //遍历后续
            if (this.action != null)
                this.action.traverse(action, traversedActionNodeSet);
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (consts != null && consts.Length > 0)
            {
                sb.Append('<');
                for (int i = 0; i < consts.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(consts[i] != null ? consts[i].ToString() : "null");
                }
                sb.Append('>');
            }
            sb.Append('(');
            if (returns != null && returns.Length > 0)
            {
                for (int i = 0; i < returns.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(returns[i] != null ? returns[i].ToString() : "null");
                }
            }
            sb.Append("); ");
            return string.Intern(sb.ToString());
        }

        /// <summary>
        /// 执行入口节点的后续动作（红线连接），并获取该动作的返回值。
        /// </summary>
        /// <param name="game">游戏对象。</param>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="args">参数列表。</param>
        /// <returns>返回值。</returns>
        public async Task<object[]> doFunctionAsync(CardEngine game, Card card, Buff buff, EventArg eventArg, params object[] args)
        {
            if (action != null)
            {
                var scope = new Scope() { args = args, consts = consts };
                return await game.doActionAsync(card, buff, eventArg, action, scope);
            }
            return new object[0];
        }
        /// <summary>
        /// 执行入口节点的后续动作（红线连接），并执行整个由红线连接起来的链表。
        /// </summary>
        /// <param name="game">游戏对象。</param>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="args">参数列表。</param>
        public async Task doFunctionsAsync(CardEngine game, Card card, Buff buff, EventArg eventArg, params object[] args)
        {
            if (action != null)
            {
                var scope = new Scope() { args = args, consts = consts };
                await game.doActionsAsync(card, buff, eventArg, action, scope);
            }
        }
        /// <summary>
        /// 执行出口节点的输入点连接的动作，并获取该动作的返回值。
        /// </summary>
        /// <typeparam name="T">返回值类型。</typeparam>
        /// <param name="game">游戏对象。</param>
        /// <param name="card">这张卡牌。</param>
        /// <param name="buff">该增益。</param>
        /// <param name="eventArg">当前事件。</param>
        /// <param name="valueIndex">返回值索引。</param>
        /// <param name="args">参数列表。</param>
        /// <returns>该动作的返回值。</returns>
        public async Task<T> getFunctionReturnValueAsync<T>(CardEngine game, Card card, Buff buff, EventArg eventArg, int valueIndex, params object[] args)
        {
            var returnRef = returns[valueIndex];
            if (returnRef != null)
            {
                var scope = new Scope() { args = args, consts = consts };
                return await game.getActionReturnValueAsync<T>(card, buff, eventArg, returnRef.valueRef.action, valueIndex, scope);
            }
            return default;
        }
        #endregion
        public string functionName;
        public ActionNode action;
        /// <summary>
        /// 该动作引用的返回值
        /// </summary>
        public ReturnValueRef[] returns;
        public object[] consts;
    }
    [Serializable]
    public sealed class SerializableFunctionNode
    {
        #region 公有方法
        #region 构造函数
        public SerializableFunctionNode(FunctionNode functionNode, bool isGraph = false)
        {
            if (functionNode == null)
                throw new ArgumentNullException(nameof(functionNode));
            functionName = functionNode.functionName;
            actionNodeId = functionNode.action != null ? functionNode.action.id : 0;
            returns = functionNode.returns != null ?
                Array.ConvertAll(functionNode.returns, i => i != null ? new SerializableReturnValueRef(i) : null) :
                new SerializableReturnValueRef[0];
            consts = functionNode.consts ?? new object[0];
            if (isGraph)
            {
                actionNodeList = new List<SerializableActionNode>();
                functionNode.traverse(a =>
                {
                    if (a != null)
                        actionNodeList.Add(new SerializableActionNode(a));
                });
            }
        }
        #endregion
        public FunctionNode toFunctionNodeGraph(List<SerializableActionNode> actionNodeList, Dictionary<int, ActionNode> actionNodeDict = null)
        {
            if (actionNodeDict == null)
                actionNodeDict = new Dictionary<int, ActionNode>();
            FunctionNode funcNode = new FunctionNode
            {
                functionName = functionName,
                consts = consts,
                returns = new ReturnValueRef[returns.Length]
            };
            if (actionNodeId != 0)
            {
                if (actionNodeDict.TryGetValue(actionNodeId, out ActionNode actionNode))
                    funcNode.action = actionNode;
                else
                    funcNode.action = SerializableActionNode.toActionNodeGraph(actionNodeId, actionNodeList, actionNodeDict);
            }
            //returns
            for (int i = 0; i < funcNode.returns.Length; i++)
            {
                if (returns[i] != null)
                    funcNode.returns[i] = returns[i].toReturnValueRef(actionNodeList, actionNodeDict);
                else
                    funcNode.returns[i] = null;
            }
            return funcNode;
        }

        public FunctionNode toFunctionNodeNodeGraph()
        {
            return toFunctionNodeGraph(actionNodeList);
        }
        #endregion
        #region 属性字段
        public int actionNodeId;
        public SerializableReturnValueRef[] returns;
        public string functionName;
        public object[] consts;
        public List<SerializableActionNode> actionNodeList;
        #endregion
    }

    public class FunctionDefine
    {
        public ValueDefine[] parameters;
        public ValueDefine[] consts;
        public ValueDefine[] returns;
    }
}