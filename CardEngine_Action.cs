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
            actionDefineDict[name] = actionDefine;
        }
        public ActionDefine getActionDefine(string name)
        {
            if (actionDefineDict.ContainsKey(name))
                return actionDefineDict[name];
            else
                return null;
        }
        /// <summary>
        /// 执行一串动作。
        /// </summary>
        /// <param name="card"></param>
        /// <param name="buff"></param>
        /// <param name="eventArg"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public Task doAction(ICard card, IBuff buff, IEventArg eventArg, ActionNode action)
        {
            return doAction<object>(card, buff, eventArg, action, null);
        }
        /// <summary>
        /// 执行一串动作并返回指定变量值
        /// </summary>
        /// <param name="card"></param>
        /// <param name="buff"></param>
        /// <param name="eventArg"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<T> doAction<T>(ICard card, IBuff buff, IEventArg eventArg, ActionNode action, string returnVarName)
        {
            ActionNode curAction = action;
            while (curAction != null)
            {
                //获取动作定义
                ActionDefine define = getActionDefine(curAction.defineName);
                //从环境中取参数值
                object[] args = new object[define.inputs.Length];
                for (int i = 0; i < define.inputs.Length; i++)
                {
                    var input = define.inputs[i];
                    if (input.isParams)
                    {
                        //是变长参数，所有当前index之后的值塞进一个数组
                        object[] objArray = curAction.inputs.Where(v => v.index >= i).OrderBy(v => v.index).Select(v => eventArg.getVar(v.varName)).ToArray();
                        Array argArray = Array.CreateInstance(input.type, objArray.Length);
                        Array.Copy(objArray, argArray, objArray.Length);
                        args[i] = argArray;
                    }
                    else
                    {
                        var varRef = curAction.inputs.FirstOrDefault(v => v.index == i);
                        if (varRef != null)
                        {
                            object arg = eventArg.getVar(varRef.varName);
                            args[i] = arg;
                        }
                        else
                            args[i] = null;
                    }
                }
                object[] outputValues = await define.execute(this, card, buff, eventArg, args, curAction.consts);
                //将输出值赋值给环境变量
                if (curAction.outputs != null && curAction.outputs.Length > 0)
                {
                    foreach (var output in curAction.outputs)
                    {
                        eventArg.setVar(output.varName, outputValues[output.index]);
                    }
                }
                //下一个动作
                curAction = curAction.next;
            }
            if (string.IsNullOrEmpty(returnVarName))
                return default;
            else
                return (T)eventArg.getVar(returnVarName);
        }
        Dictionary<string, ActionDefine> actionDefineDict { get; } = new Dictionary<string, ActionDefine>();
    }
}