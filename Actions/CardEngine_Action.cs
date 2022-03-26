using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    partial class CardEngine
    {
        #region 公有方法
        /// <summary>
        /// 开始游戏
        /// </summary>
        /// <returns></returns>
        public async Task startGame(GameOption option, Player[] players)
        {
            this.option = option;
            playerList.Clear();
            playerList.AddRange(players);
            foreach (Player player in playerList)
            {
                await rule.onPlayerInit(this, player);
            }
            await rule.onGameStart(this);
        }
        #endregion
        #region 属性字段
        public List<Player> playerList { get; } = new List<Player>();
        #endregion
    }
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
        public async Task doActionsAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode actions)
        {
            ActionNode curAction = actions;
            while (curAction != null)
            {
                await doActionAsync(card, buff, eventArg, curAction);
                if (curAction.branches != null && curAction.branches.Length > 0)
                    curAction = curAction.branches[0];
                else
                    break;
            }
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
            return doActionAsync(card, buff, eventArg, action, new Scope());
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
        private async Task<object[]> doActionAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode action, Scope scope)
        {
            scope.actionNodeStack.Push(action);
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
                        object arg;
                        if (valueInput.isOut)
                        {
                            //输入参数不要求执行就可以从环境中获得
                            arg = scope.getLocalVar(valueRef.actionNodeId, valueRef.index);
                        }
                        else if (valueRef.action != null)
                        {
                            try
                            {
                                object[] result = await doActionAsync(card, buff, eventArg, valueRef.action, scope);
                                arg = result[valueRef.index];
                            }
                            catch (Exception e)
                            {
                                if (e is TargetInvocationException targetInvocationException)
                                    e = targetInvocationException.InnerException;
                                logger.logError("获取" + action + "的参数" + valueInput.name + "失败：" + e);
                                throw e;
                            }
                        }
                        else if (!string.IsNullOrEmpty(valueRef.eventVarName))
                        {
                            arg = eventArg.getVar(valueRef.eventVarName);
                        }
                        else
                        {
                            arg = scope.getLocalVar(valueRef.actionNodeId, valueRef.index);
                        }
                        paramList.Add(arg);
                    }
                    Array argArray = Array.CreateInstance(valueInput.type, paramList.Count);
                    Array.Copy(paramList.ToArray(), argArray, paramList.Count);
                    args[actionOutputs.Length + i] = argArray;
                }
                else
                {
                    if (action.inputs[i] is ActionValueRef valueRef)
                    {
                        object arg;
                        if (valueInput.isOut)
                        {
                            //输入参数不要求执行就可以从环境中获得
                            arg = scope.getLocalVar(valueRef.actionNodeId, valueRef.index);
                        }
                        else if (valueRef.action != null)
                        {
                            //logger.log("正在获取" + action + "的参数" + valueInput.name + "，值引用：" + valueRef);
                            try
                            {
                                object[] result = await doActionAsync(card, buff, eventArg, valueRef.action, scope);
                                arg = result[valueRef.index];
                                //logger.log("成功获取" + action + "的参数" + valueInput.name + "，返回结果：" + string.Join("，", result) + "，索引" + valueRef.index);
                            }
                            catch (Exception e)
                            {
                                if (e is TargetInvocationException targetInvocationException)
                                    e = targetInvocationException.InnerException;
                                logger.logError("获取" + action + "的参数" + valueInput.name + "失败：" + e);
                                throw e;
                            }
                        }
                        else if (!string.IsNullOrEmpty(valueRef.eventVarName))
                        {
                            arg = eventArg.getVar(valueRef.eventVarName);
                        }
                        else
                        {
                            arg = scope.getLocalVar(valueRef.actionNodeId, valueRef.index);
                        }
                        args[actionOutputs.Length + i] = arg;
                    }
                    else
                        args[actionOutputs.Length + i] = null;
                }
            }
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
                //#region 日志
                //string msg = "执行动作" + action + "成功";
                //if (args != null && args.Length > 0)
                //{
                //    msg += "，参数：" + string.Join(",", args);
                //}
                //if (result != null && result.Length > 0)
                //{
                //    msg += "，返回值：" + string.Join(",", result);
                //}
                //logger.log(msg);
                //#endregion
                return result;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException targetInvocationException)
                    e = targetInvocationException.InnerException;
                logger.logError("Game", "执行动作" + action + "发生异常：" + e);
                throw e;
            }
            finally
            {
                scope.actionNodeStack.Pop();
            }
        }
        #endregion
        Dictionary<string, ActionDefine> actionDefineDict { get; } = new Dictionary<string, ActionDefine>();
    }
    [Serializable]
    public class Scope
    {
        #region 公有方法
        public string getLocalVarName(int index)
        {
            return getLocalVarName(actionNodeStack.Peek().id, index);
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
            return localVarDict[getLocalVarName(actionNodeId, index)];
        }
        #endregion
        #region 属性字段
        public Stack<ActionNode> actionNodeStack = new Stack<ActionNode>();
        public Dictionary<string, object> localVarDict = new Dictionary<string, object>();
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
}