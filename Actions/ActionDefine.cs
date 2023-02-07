using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;
namespace TouhouCardEngine
{
    public abstract class ActionDefine
    {
        #region 公有方法
        #region 构造方法
        public ActionDefine(string defineName, params string[] obsoleteNames)
        {
            this.defineName = defineName;
            this.obsoleteNames = obsoleteNames;
        }
        #endregion
        #region 静态方法
        public static Task<Dictionary<string, ActionDefine>> loadDefinesFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => loadDefinesFromAssemblies(assemblies));
        }
        /// <summary>
        /// 通过反射的方式加载所有目标程序集中的动作定义，包括派生的动作定义和反射方法生成的动作定义。
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static Dictionary<string, ActionDefine> loadDefinesFromAssemblies(Assembly[] assemblies)
        {
            Dictionary<string, ActionDefine> defineDict = new Dictionary<string, ActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    //是ActionDefine的子类，不是抽象类，具有零参数构造函数
                    if (type.IsSubclassOf(typeof(ActionDefine)) &&
                        !type.IsAbstract &&
                        type.GetConstructor(new Type[0]) is ConstructorInfo constructor)
                    {
                        ActionDefine actionDefine = (ActionDefine)constructor.Invoke(new object[0]);
                        string name = actionDefine.GetType().Name;
                        if (name.EndsWith("ActionDefine"))
                            name = name.Substring(0, name.Length - 12);
                        defineDict.Add(name, actionDefine);
                    }
                }
            }
            foreach (var pair in MethodActionDefine.loadMethodsFromAssemblies(assemblies))
            {
                defineDict.Add(pair.Key, pair.Value);
            }
            return defineDict;
        }
        #endregion
        public abstract Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues);
        /// <summary>
        /// 获取指定索引的输出值定义，动作类型输出不会被包括在内。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ValueDefine getValueOutputAt(int index)
        {
            int count = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i].type == typeof(ActionNode))
                {
                    continue;
                }
                if (count == index)
                {
                    return outputs[i];
                }
                count++;
            }
            throw new IndexOutOfRangeException("索引" + index + "超出动作" + defineName + "的值输出数量上限或下限");
        }
        public ValueDefine getValueInputAt(int index)
        {
            if (inputs.Length <= 0)
                return null;
            var lastInput = inputs[inputs.Length - 1];
            if (lastInput != null && lastInput.isParams && index >= inputs.Length)
            {
                //多参数，获取最后一个参数类型
                return lastInput;
            }
            return inputs[index];
        }
        public ValueDefine[] getValueInputs()
        {
            return inputs;
        }
        public ValueDefine[] getValueOutputs()
        {
            return Array.FindAll(outputs, output => output.type != typeof(ActionNode));
        }
        public ValueDefine[] getActionOutputs()
        {
            return Array.FindAll(outputs, output => output.type == typeof(ActionNode));
        }
        public void setObsoleteMessage(string message)
        {
            if (message == null)
                message = "";
            obsoleteMsg = message;
        }
        #endregion
        #region 属性字段
        public string defineName;
        public string[] obsoleteNames;
        /// <summary>
        /// 节点过期提示。不为null则说明过期
        /// </summary>
        public string obsoleteMsg;

        public abstract ValueDefine[] inputs { get; }
        public abstract ValueDefine[] consts { get; }
        public abstract ValueDefine[] outputs { get; }
        #endregion
    }
}