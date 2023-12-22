using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
namespace TouhouCardEngine
{
    public abstract class ActionDefine
    {
        #region 公有方法
        #region 构造方法
        public ActionDefine(int defineId, string editorName, params string[] obsoleteNames)
        {
            this.defineId = defineId;
            this.editorName = editorName;
            this.obsoleteNames = obsoleteNames;
        }
        #endregion
        #region 静态方法
        public static Task<List<ActionDefine>> loadDefinesFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => loadDefinesFromAssemblies(assemblies));
        }
        /// <summary>
        /// 通过反射的方式加载所有目标程序集中的动作定义，包括派生的动作定义和反射方法生成的动作定义。
        /// </summary>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static List<ActionDefine> loadDefinesFromAssemblies(Assembly[] assemblies)
        {
            List<ActionDefine> defineDict = new List<ActionDefine>();
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
                        defineDict.Add(actionDefine);
                    }
                }
            }
            foreach (var define in MethodActionDefine.loadMethodsFromAssemblies(assemblies))
            {
                defineDict.Add(define);
            }
            return defineDict;
        }
        #endregion
        public abstract Task<ControlOutput> run(Flow flow, Node node);
        /// <summary>
        /// 获取指定索引的输出端口定义，动作类型输出不会被包括在内。
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public PortDefine getValueOutputAt(int index)
        {
            return getValueOutputs().ElementAtOrDefault(index);
        }
        public PortDefine getValueInputAt(int index)
        {
            var inputs = getValueInputs();
            int count = inputs.Count();
            if (count <= 0)
                return null;
            var lastInput = inputs.LastOrDefault();
            if (lastInput != null && lastInput.isParams && index >= count)
            {
                //多参数，获取最后一个参数类型
                return lastInput;
            }
            return inputs.ElementAtOrDefault(index);
        }
        public IEnumerable<PortDefine> getValueInputs()
        {
            return inputDefines.Where(d => d.GetPortType() == PortType.Value);
        }
        public IEnumerable<PortDefine> getValueOutputs()
        {
            return outputDefines.Where(d => d.GetPortType() == PortType.Value);
        }
        public IEnumerable<PortDefine> getActionOutputs()
        {
            return outputDefines.Where(d => d.GetPortType() == PortType.Control);
        }
        public PortDefine getInputDefine(string name)
        {
            return inputDefines.FirstOrDefault(d => d.name == name);
        }
        public PortDefine getOutputDefine(string name)
        {
            return outputDefines.FirstOrDefault(d => d.name == name);
        }
        public void setObsoleteMessage(string message)
        {
            if (message == null)
                message = "";
            obsoleteMsg = message;
        }
        public override string ToString()
        {
            return editorName;
        }
        #endregion
        #region 属性字段
        public int defineId;
        public long cardPoolId;
        public string editorName;
        public string category { get; protected set; }
        public string[] obsoleteNames;
        /// <summary>
        /// 节点过期提示。不为null则说明过期
        /// </summary>
        public string obsoleteMsg;
        public NodeDefineType type;

        public abstract IEnumerable<PortDefine> inputDefines { get; }
        public abstract IEnumerable<PortDefine> outputDefines { get; }

        public const string enterPortName = "enter";

        public const string exitPortName = "exit";
        public static readonly PortDefine enterPortDefine = PortDefine.Control(enterPortName, string.Empty);
        public static readonly PortDefine exitPortDefine = PortDefine.Control(exitPortName, string.Empty);
        #endregion
    }

    public enum NodeDefineType
    {
        Action,
        Function,
        Event
    }
}