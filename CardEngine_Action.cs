using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public partial class CardEngine
    {
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
        /// 执行单个动作并返回指定变量值，这是一个会等待动作的异步方法。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="buff"></param>
        /// <param name="eventArg"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<object[]> doActionAsync(ICard card, IBuff buff, IEventArg eventArg, ActionNode action)
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
                    foreach (var valueRef in action.inputs.Skip(i))
                    {
                        paramList.Add((await doActionAsync(card, buff, eventArg, valueRef.action))[valueRef.index]);
                    }
                    Array argArray = Array.CreateInstance(input.type, paramList.Count);
                    Array.Copy(paramList.ToArray(), argArray, paramList.Count);
                    args[i] = argArray;
                }
                else
                {
                    var valueRef = action.inputs[i];
                    if (valueRef != null)
                    {
                        object arg = (await doActionAsync(card, buff, eventArg, valueRef.action))[valueRef.index];
                        args[i] = arg;
                    }
                    else
                        args[i] = null;
                }
            }
            return await define.execute(this, card, buff, eventArg, args, action.consts.ToArray());
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
        #region 动作定义
        [ActionNodeMethod("ThisCard")]
        [return: ActionNodeParam("Card")]
        public static Card thisCard(Card card)
        {
            return card;
        }
        #endregion
        Dictionary<string, ActionDefine> actionDefineDict { get; } = new Dictionary<string, ActionDefine>();
    }
}