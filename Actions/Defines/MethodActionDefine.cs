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
        public MethodActionDefine(ActionNodeMethodAttribute attribute, MethodInfo methodInfo) : base(attribute.methodName, attribute.obsoleteNames)
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
                if (isParams)
                {
                    if (paramType.IsArray)
                    {
                        paramType = paramType.GetElementType();
                    }
                }

                if (paramInfo.IsOut || isOut)
                {
                    //如果参数是out参数，那么它是一个输出
                    PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
                    outputList.Add(valueDefine);
                }
                else if (paramInfo.ParameterType == typeof(ActionNode) && paramAttr != null)
                {
                    //如果参数类型是动作节点，那么它是一个动作分支输出
                    PortDefine controlDefine = PortDefine.Control(paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
                    outputList.Add(controlDefine);
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
                            PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
                            inputList.Add(valueDefine);
                        }
                    }
                    else if (!typeof(IGame).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(ICard).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(Flow).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(ActionNode).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //不是Game,Card,Buff,EventArg,Scope这种可以缺省的参数也一定是输入
                        PortDefine valueDefine = PortDefine.Value(paramType, paramInfo.Name, paramAttr?.paramName ?? paramInfo.Name);
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
            return node.getOutputPort<ControlOutput>("exit");
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
            return !(obj is Array) && (type == typeof(Array) || type.IsArray);
        }
        private bool isObjectNeedToUnpackForType(object array, Type type)
        {
            return array is Array && !(type.IsAssignableFrom(array.GetType()) || type == typeof(Array) || type.IsArray);
        }
        private bool isArrayNeedToCastForType(Array array, Type type)
        {
            if (array == null)
                return false;
            if (type != typeof(Array) && !type.IsArray)
                return false;
            if (type.IsAssignableFrom(array.GetType()))
                return false;
            return type.HasElementType && type.GetElementType() is Type elementType &&
                (elementType.IsAssignableFrom(array.GetType().GetElementType()) ||
                array.GetType().GetElementType().IsAssignableFrom(elementType));
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
        private object castArrayToTargetTypeArray(Array array, Type elementType)
        {
            Array targetArray = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                targetArray.SetValue(array.GetValue(i), i);
            }
            return targetArray;
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
                    //指定了不能省略的参数
                    if (paramInfo.IsOut || paramAttr.isOut)
                    {
                        //out参数输出留空
                        parameters[i] = null;
                    }
                    else if (paramInfo.ParameterType == typeof(Node))
                    {
                        ControlOutput port = node.getOutputPort<ControlOutput>(name);
                        parameters[i] = port?.getConnectedInputPort()?.node;
                    }
                    else
                    {
                        object param = null;
                        if (paramAttr.isConst)
                        {
                            param = node.consts;
                        }
                        else
                        {
                            ValueInput port = node.getInputPort<ValueInput>(paramAttr.paramName);
                            if (port != null)
                            {
                                param = await flow.getValue(port);
                            }
                        }
                        if (isObjectNeedToPackForType(param, paramInfo.ParameterType))
                            param = packObjectToArray(param, paramInfo.ParameterType.GetElementType());
                        else if (isObjectNeedToUnpackForType(param, paramInfo.ParameterType))
                            param = unpackArrayToObject(param as Array);
                        else if (param is Array array && isArrayNeedToCastForType(array, paramInfo.ParameterType))
                            param = castArrayToTargetTypeArray(array, paramInfo.ParameterType.GetElementType());
                        parameters[i] = param;
                    }
                }
                else
                {
                    if (paramInfo.IsOut)
                    {
                        //out参数输出留空
                        parameters[i] = null;
                    }
                    else if (paramInfo.ParameterType == typeof(Node))
                    {
                        ControlOutput port = node.getOutputPort<ControlOutput>(name);
                        parameters[i] = port?.getConnectedInputPort()?.node;
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
                    else
                    {
                        ValueInput port = node.getInputPort<ValueInput>(name);
                        if (port != null)
                        {
                            object param = await flow.getValue(port);
                            if (isObjectNeedToPackForType(param, paramInfo.ParameterType))
                                param = packObjectToArray(param, paramInfo.ParameterType.GetElementType());
                            else if (isObjectNeedToUnpackForType(param, paramInfo.ParameterType))
                                param = unpackArrayToObject(param as Array);
                            else if (param is Array array && isArrayNeedToCastForType(array, paramInfo.ParameterType))
                                param = castArrayToTargetTypeArray(array, paramInfo.ParameterType.GetElementType());
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
            }

            //返回由返回值和out参数组成的数组
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                var attr = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                if (paramInfo.IsOut || (attr != null && attr.isOut))
                {
                    var name = paramInfo.Name;
                    ValueOutput port = node.getOutputPort<ValueOutput>(name);
                    flow.setValue(port, parameters[i]);
                }
            }

            var returnName = returnValueName;
            ValueOutput returnPort = node.getOutputPort<ValueOutput>(returnName);
            flow.setValue(returnPort, returnValue);


        }
        #endregion
        private const string returnValueName = "return";
        public string methodName => defineName;
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