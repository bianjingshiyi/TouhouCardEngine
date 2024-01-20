using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    /// <summary>
    /// 通过反射生成的方法动作定义，目标方法必须是静态方法，并且使用特性标记参数与返回信息。
    /// </summary>
    public class MethodActionDefine : ActionDefine
    {
        #region 公有方法
        #region 静态方法
        public static Task<List<MethodActionDefine>> loadMethodsFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => Task.FromResult(loadMethodsFromAssemblies(assemblies)));
        }
        public static List<MethodActionDefine> loadMethodsFromAssemblies(Assembly[] assemblies)
        {
            List<MethodActionDefine> defineDict = new List<MethodActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (method.GetCustomAttribute<ActionNodeMethodAttribute>() is ActionNodeMethodAttribute attribute)
                        {
                            var define = new MethodActionDefine(attribute, method);
                            defineDict.Add(define);
                        }
                    }
                }
            }
            return defineDict;
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInfo">必须是静态方法</param>
        public MethodActionDefine(ActionNodeMethodAttribute attribute, MethodInfo methodInfo) : base(attribute.defineId, attribute.editorName, attribute.obsoleteNames)
        {
            if (!methodInfo.IsStatic)
                throw new ArgumentException("Target method must be static", nameof(methodInfo));
            category = attribute.category;
            type = attribute.actionType;
            _methodInfo = methodInfo;
            //分析参数设置输入和输出
            _paramsInfo = methodInfo.GetParameters();
            _paramAttributes = new ActionNodeParamAttribute[_paramsInfo.Length];
            ActionNodeParamAttribute paramAttr;
            List<PortDefine> inputList = new List<PortDefine>();
            if (type != NodeDefineType.Function)
            {
                inputList.Add(enterPortDefine);
            }
            List<PortDefine> outputList = new List<PortDefine>();
            if (type != NodeDefineType.Function)
            {
                outputList.Add(exitPortDefine);
            }
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                paramAttr = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                _paramAttributes[i] = paramAttr;
                bool isParams = paramAttr != null ? paramAttr.isParams : false;
                bool isOut = paramAttr != null ? paramAttr.isOut : false;

                var metaAttributes = paramInfo.GetCustomAttributes<NodeParamMetaAttribute>();

                Type paramType = paramInfo.ParameterType;
                if (paramType.IsByRef)
                {
                    paramType = paramType.GetElementType();
                }
                if (isParams)
                {
                    if (paramType.IsArray)
                    {
                        paramType = paramType.GetElementType();
                    }
                }

                var metas = metaAttributes.Select(m => m.toPortMeta());
                if (paramInfo.ParameterType == typeof(ControlInput) && paramAttr != null)
                {
                    //如果参数类型是动作节点，那么它是一个动作分支输出
                    PortDefine controlDefine = PortDefine.Control(paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name, metas: metas);
                    outputList.Add(controlDefine);
                }
                else if (paramInfo.IsOut || isOut)
                {
                    //如果参数是out参数，那么它是一个输出
                    PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name, metas: metas);
                    outputList.Add(valueDefine);
                }
                else
                {
                    if (paramAttr != null)
                    {
                        //用特性指定是输入
                        PortDefine inputDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name, paramAttr.isParams, isConst: paramAttr.isConst, metas: metas);
                        inputList.Add(inputDefine);
                    }
                    else if (!typeof(IGame).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(ICard).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(Flow).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(ActionNode).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IEffect).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //不是Game,Card,Buff,EventArg,Flow,Effect, ActionNode这种可以缺省的参数也一定是输入
                        PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name, paramAttr.isParams, metas: metas);
                        inputList.Add(valueDefine);
                    }
                }
            }
            //如果方法返回类型为void或者Task，视为无返回值，否则有返回值
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType != typeof(Task))
            {
                paramAttr = methodInfo.ReturnParameter.GetCustomAttribute<ActionNodeParamAttribute>();
                var metaAttributes = methodInfo.ReturnParameter.GetCustomAttributes<NodeParamMetaAttribute>();
                var returnMetas = metaAttributes.Select(m => m.toPortMeta());
                Type returnType = methodInfo.ReturnType;
                if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    //如果返回类型为Task<T>，则返回值类型视为T
                    returnType = methodInfo.ReturnType.GetGenericArguments()[0];
                }
                outputList.Add(PortDefine.Value(returnType, returnValueName, paramAttr?.paramName ?? "Value", metas: returnMetas));
            }
            //设置输入输出
            _inputs = inputList.ToArray();
            _outputs = outputList.ToArray();

            // 设置过期提示
            var obsolete = methodInfo.GetCustomAttribute<ObsoleteAttribute>();
            if (obsolete != null)
                setObsoleteMessage(obsolete.Message);
        }
        public override async Task<ControlOutput> run(Flow flow, Node node)
        {
            try
            {
                var parameters = await getParameters(flow, node);

                object returnValue = _methodInfo.Invoke(null, parameters);

                await sendReturnValue(flow, node, returnValue, parameters);
            }
            catch (Exception e)
            {
                flow.env?.game?.logger?.logError($"执行节点({editorName})时出错：{e}");
            }

            return node.getOutputPort<ControlOutput>(exitPortName);
        }
        #endregion
        #region 私有方法

        private async Task<object[]> getParameters(Flow flow, Node node)
        {
            object[] parameters = new object[_paramsInfo.Length];
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                var paramAttr = _paramAttributes[i];
                string name = paramInfo.Name;
                if (paramAttr != null)
                {
                    // 动作输出
                    if (paramInfo.ParameterType == typeof(ControlInput))
                    {
                        ControlOutput port = node.getOutputPort<ControlOutput>(name);
                        parameters[i] = port?.getConnectedInputPort();
                    }
                    // 值引用
                    else if (paramInfo.ParameterType.IsGenericType && paramInfo.ParameterType.GetGenericTypeDefinition() == typeof(NodeValueRef<>))
                    {
                        ValueInput port = node.getInputPort<ValueInput>(name);
                        ValueOutput output = port?.getConnectedOutputPort();
                        parameters[i] = NodeValueRef.FromType(paramInfo.ParameterType, output);
                    }
                    //指定了不能省略的参数
                    else if (paramInfo.IsOut || paramAttr.isOut)
                    {
                        //out参数输出留空
                        parameters[i] = null;
                    }
                    else
                    {
                        object param = null;
                        if (paramAttr.isParams)
                        {
                            var ports = node.getParamInputPorts(paramInfo.Name);
                            // 这里要-1，少包括最后一个变长参数的内容。
                            var array = new object[ports.Length - 1];
                            for (int paramIndex = 0; paramIndex < array.Length; paramIndex++)
                            {
                                var port = ports[paramIndex];
                                if (port != null)
                                {
                                    array[paramIndex] = await flow.getValue(port);
                                }
                            }
                            param = array;
                        }
                        else
                        {
                            ValueInput port = node.getInputPort<ValueInput>(paramInfo.Name);
                            if (port != null)
                            {
                                if (!port.hasValue() && paramInfo.HasDefaultValue)
                                {
                                    param = paramInfo.DefaultValue;
                                }
                                else
                                {
                                    param = await flow.getValue(port);
                                }
                            }
                        }
                        param = FlowConvert.convertType(flow, param, paramInfo.ParameterType);
                        parameters[i] = param;
                    }
                }
                else
                {
                    if (paramInfo.ParameterType == typeof(ControlInput))
                    {
                        ControlOutput port = node.getOutputPort<ControlOutput>(name);
                        parameters[i] = port?.getConnectedInputPort();
                    }
                    else if (paramInfo.IsOut)
                    {
                        //out参数输出留空
                        parameters[i] = null;
                    }
                    else if (typeof(IGame).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        parameters[i] = flow.env.game;
                    }
                    else if (typeof(ICard).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Card
                        parameters[i] = flow.env.card;
                    }
                    else if (typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Buff
                        parameters[i] = flow.env.buff;
                    }
                    else if (typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的EventArg
                        parameters[i] = flow.env.eventArg;
                    }
                    else if (typeof(Flow).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Flow
                        parameters[i] = flow;
                    }
                    else if (typeof(ActionNode).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Node
                        parameters[i] = node as ActionNode;
                    }
                    else if (typeof(IEffect).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的EventArg
                        parameters[i] = flow.env.effect;
                    }
                    else
                    {
                        ValueInput port = node.getInputPort<ValueInput>(name);
                        if (port != null)
                        {
                            object param = await flow.getValue(port);
                            param = FlowConvert.convertType(flow, param, paramInfo.ParameterType);
                            parameters[i] = param;
                        }
                    }
                }
            }
            return parameters;
        }
        private async Task sendReturnValue(Flow flow, Node node, object returnValue, object[] parameters)
        {
            if (returnValue is Task task)
            {
                await task;
                //返回Task则视为返回null，返回Task<T>则返回对应值
                if (task.GetType().GetProperty(nameof(Task<object>.Result)) is PropertyInfo propInfo)
                {
                    returnValue = propInfo.GetValue(task);
                }
                else
                {
                    returnValue = null;
                }
            }

            //返回由返回值和out参数组成的数组
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                if (paramInfo.IsOut)
                {
                    var name = paramInfo.Name;
                    ValueOutput port = node.getOutputPort<ValueOutput>(name);
                    if (port == null)
                        continue;
                    flow.setValue(port, parameters[i]);
                }
            }

            var returnName = returnValueName;
            ValueOutput returnPort = node.getOutputPort<ValueOutput>(returnName);
            if (returnPort != null)
            {
                flow.setValue(returnPort, returnValue);
            }


        }
        #endregion
        public const string returnValueName = "return";
        MethodInfo _methodInfo;
        ParameterInfo[] _paramsInfo;
        ActionNodeParamAttribute[] _paramAttributes;

        private PortDefine[] _inputs;
        private PortDefine[] _outputs;
        public override IEnumerable<PortDefine> inputDefines => _inputs;
        public override IEnumerable<PortDefine> outputDefines => _outputs;
    }
}