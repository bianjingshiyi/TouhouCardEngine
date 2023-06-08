using System;
using System.Collections.Generic;
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
        public static Task<Dictionary<string, MethodActionDefine>> loadMethodsFromAssembliesAsync(Assembly[] assemblies)
        {
            return Task.Run(() => Task.FromResult(loadMethodsFromAssemblies(assemblies)));
        }
        public static Dictionary<string, MethodActionDefine> loadMethodsFromAssemblies(Assembly[] assemblies)
        {
            Dictionary<string, MethodActionDefine> defineDict = new Dictionary<string, MethodActionDefine>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (method.GetCustomAttribute<ActionNodeMethodAttribute>() is ActionNodeMethodAttribute attribute)
                        {
                            var define = new MethodActionDefine(attribute, method);
                            defineDict.Add(define.defineName, define);
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
        public MethodActionDefine(ActionNodeMethodAttribute attribute, MethodInfo methodInfo) : base(attribute.defineName, attribute.editorName, attribute.obsoleteNames)
        {
            if (!methodInfo.IsStatic)
                throw new ArgumentException("Target method must be static", nameof(methodInfo));
            category = attribute.category;
            _methodInfo = methodInfo;
            //分析参数设置输入和输出
            _paramsInfo = methodInfo.GetParameters();
            ActionNodeParamAttribute paramAttr;
            List<PortDefine> inputList = new List<PortDefine>()
            {
                enterPortDefine
            };
            List<PortDefine> constList = new List<PortDefine>();
            List<PortDefine> outputList = new List<PortDefine>()
            {
                exitPortDefine
            };
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                paramAttr = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                bool isParams = paramAttr != null ? paramAttr.isParams : false;
                bool isOut = paramAttr != null ? paramAttr.isOut : false;

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

                if (paramInfo.ParameterType == typeof(ControlInput) && paramAttr != null)
                {
                    //如果参数类型是动作节点，那么它是一个动作分支输出
                    PortDefine controlDefine = PortDefine.Control(paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
                    outputList.Add(controlDefine);
                }
                else if (paramInfo.IsOut || isOut)
                {
                    //如果参数是out参数，那么它是一个输出
                    PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
                    outputList.Add(valueDefine);
                }
                else
                {
                    if (paramAttr != null)
                    {
                        if (paramAttr.isConst)
                        {
                            //用特性指定是常量
                            PortDefine constDefine = PortDefine.Const(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
                            constList.Add(constDefine);
                        }
                        else
                        {
                            //用特性指定是输入
                            PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name, paramAttr.isParams);
                            inputList.Add(valueDefine);
                        }
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
                        PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name, paramAttr.isParams);
                        inputList.Add(valueDefine);
                    }
                }
            }
            //如果方法返回类型为void或者Task，视为无返回值，否则有返回值
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType != typeof(Task))
            {
                paramAttr = methodInfo.ReturnParameter.GetCustomAttribute<ActionNodeParamAttribute>();
                Type returnType = methodInfo.ReturnType;
                if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    //如果返回类型为Task<T>，则返回值类型视为T
                    returnType = methodInfo.ReturnType.GetGenericArguments()[0];
                }
                outputList.Add(PortDefine.Value(returnType, returnValueName, paramAttr?.paramName ?? "Value"));
            }
            //设置输入输出
            _inputs = inputList.ToArray();
            _consts = constList.ToArray();
            _outputs = outputList.ToArray();

            // 设置过期提示
            var obsolete = methodInfo.GetCustomAttribute<ObsoleteAttribute>();
            if (obsolete != null)
                setObsoleteMessage(obsolete.Message);
        }
        public override async Task<ControlOutput> run(Flow flow, Node node)
        {
            var parameters = await getParameters(flow, node);

            object returnValue = _methodInfo.Invoke(null, parameters);

            await sendReturnValue(flow, node, returnValue, parameters);
            return node.getOutputPort<ControlOutput>(exitPortName);
        }
        #endregion
        #region 私有方法
        /// <summary>
        /// is object nessesary to convert to target array type?
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool isObjectNeedToPackForType(object obj, Type type)
        {
            return !(obj is Array) && //值不是数组
                (type == typeof(Array) || type.IsArray); //类型是数组
        }
        private bool isObjectNeedToUnpackForType(object obj, Type type)
        {
            return obj is Array && //值是数组
                !(type == typeof(Array) || type.IsArray || type.IsAssignableFrom(obj.GetType())); //类型不是数组
        }
        /// <summary>
        /// 是否需要将数组转换为目标类型的数组？
        /// </summary>
        /// <param name="array">要转换的数组。</param>
        /// <param name="type">目标类型。</param>
        /// <returns></returns>
        private bool isArrayNeedToCastForType(Array array, Type type)
        {
            if (array == null) // 数组为null。
                return false;
            if (type != typeof(Array) && !type.IsArray) // 类型不是数组。
                return false;
            if (!type.HasElementType)// 目标类型数组没有元素类型。
                return false; 

            Type arrayType = array.GetType();
            if (type.IsAssignableFrom(arrayType)) // 该数组可以直接转换到目标数组的类型。
                return false;

            Type arrayElementType = arrayType.GetElementType();
            Type elementType = type.GetElementType();
            return Flow.canConvertTo(arrayElementType, elementType) || Flow.canConvertTo(elementType, arrayElementType); // 目标类型数组的元素可以和数组的元素类型相转换。
        }
        private object packObjectToArray(object obj, Type elementType)
        {
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(obj, 0);
            return array;
        }
        private object unpackArrayToObject(Array array)
        {
            return array.GetValue(0);
        }
        private object castArrayToTargetTypeArray(Flow flow, Array array, Type elementType)
        {
            Array targetArray = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                var value = array.GetValue(i);
                value = flow.convertTo(value, elementType);
                targetArray.SetValue(value, i);
            }
            return targetArray;
        }
        private object processType(Flow flow, object param, Type type)
        {
            if (flow.tryConvertTo(param, type, out var output))
            {
                return output;
            }

            if (isObjectNeedToPackForType(param, type))
                param = packObjectToArray(param, type.GetElementType());
            else if (isObjectNeedToUnpackForType(param, type))
                param = unpackArrayToObject(param as Array);
            else if (param is Array array && isArrayNeedToCastForType(array, type))
                param = castArrayToTargetTypeArray(flow, array, type.GetElementType());
            return param;
        }

        private async Task<object[]> getParameters(Flow flow, Node node)
        {
            object[] parameters = new object[_paramsInfo.Length];
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                var paramAttr = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                string name = paramInfo.Name;
                if (paramAttr != null)
                {
                    if (paramInfo.ParameterType == typeof(ControlInput))
                    {
                        ControlOutput port = node.getOutputPort<ControlOutput>(name);
                        parameters[i] = port?.getConnectedInputPort();
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
                        if (paramAttr.isConst)
                        {
                            param = node.getConst(paramInfo.Name);
                        }
                        else
                        {
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
                                    param = await flow.getValue(port);
                                }
                            }
                        }
                        param = processType(flow, param, paramInfo.ParameterType);
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
                            param = processType(flow, param, paramInfo.ParameterType);
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
        private const string returnValueName = "return";
        MethodInfo _methodInfo;
        ParameterInfo[] _paramsInfo;

        private PortDefine[] _inputs;
        private PortDefine[] _consts;
        private PortDefine[] _outputs;
        public override IEnumerable<PortDefine> inputDefines => _inputs;
        public override IEnumerable<PortDefine> constDefines => _consts;
        public override IEnumerable<PortDefine> outputDefines => _outputs;
    }
}