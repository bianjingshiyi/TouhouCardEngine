using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            return doActionAsync(card, buff, eventArg, action, new Dictionary<long, object>());
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
        public async Task<T> getActionReturnValueAsync<T>(Card card, Buff buff, EventArg eventArg, ActionNode action, int valueIndex = -1)
        {
            object[] results = await doActionAsync(card, buff, eventArg, action, new Dictionary<long, object>());
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
        private async Task<object[]> doActionAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode action, Dictionary<long, object> varDict)
        {
            //获取动作定义
            ActionDefine define = getActionDefine(action.defineName);
            //从环境中取参数值
            object[] args = new object[define.inputs.Length];
            for (int i = 0; i < define.inputs.Length; i++)
            {
                var input = define.inputs[i];
                if (input.isParams)
                {
                    //是变长参数，所有当前index之后的值塞进一个数组
                    List<object> paramList = new List<object>();
                    foreach (ActionValueRef valueRef in action.inputs.Skip(i))
                    {
                        object arg;
                        if (valueRef.action != null)
                        {
                            arg = (await doActionAsync(card, buff, eventArg, valueRef.action, varDict))[valueRef.index];
                        }
                        else if (!string.IsNullOrEmpty(valueRef.eventVarName))
                        {
                            arg = eventArg.getVar(valueRef.eventVarName);
                        }
                        else
                        {
                            long key = ((long)valueRef.actionNodeId << 32) + valueRef.index;
                            arg = varDict[key];
                        }
                        paramList.Add(arg);
                    }
                    Array argArray = Array.CreateInstance(input.type, paramList.Count);
                    Array.Copy(paramList.ToArray(), argArray, paramList.Count);
                    args[i] = argArray;
                }
                else
                {
                    ActionValueRef valueRef = action.inputs[i];
                    if (valueRef != null)
                    {
                        object arg;
                        if (valueRef.action != null)
                        {
                            arg = (await doActionAsync(card, buff, eventArg, valueRef.action, varDict))[valueRef.index];
                        }
                        else if (!string.IsNullOrEmpty(valueRef.eventVarName))
                        {
                            arg = eventArg.getVar(valueRef.eventVarName);
                        }
                        else
                        {
                            long key = ((long)valueRef.actionNodeId << 32) + valueRef.index;
                            arg = varDict[key];
                        }
                        args[i] = arg;
                    }
                    else
                        args[i] = null;
                }
            }
            try
            {
                var result = await define.execute(this, card, buff, eventArg, args, action.consts.ToArray());
                if (action.regVar != null && action.regVar.Length > 0)
                {
                    for (int i = 0; i < action.regVar.Length; i++)
                    {
                        if (action.regVar[i] && i < result.Length)
                        {
                            long key = ((long)action.id << 32) + i;
                            varDict[key] = result[i];
                        }
                    }
                }
                string msg = "执行动作" + action + "成功";
                if (args != null && args.Length > 0)
                {
                    msg += "，参数：" + string.Join(",", args);
                }
                if (result != null && result.Length > 0)
                {
                    msg += "，返回值：" + string.Join(",", result);
                }
                logger.log(msg);
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
        Dictionary<string, ActionDefine> actionDefineDict { get; } = new Dictionary<string, ActionDefine>();
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