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
                            defineDict.Add(attribute.methodName, new MethodActionDefine(attribute, method));
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
            List<ValueDefine> outputList = new List<ValueDefine>();
            //分析参数设置输入和输出
            _paramsInfo = methodInfo.GetParameters();
            ActionNodeParamAttribute paramAttr;
            List<ValueDefine> inputList = new List<ValueDefine>();
            List<ValueDefine> constList = new List<ValueDefine>();
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                paramAttr = paramInfo.GetCustomAttribute<ActionNodeParamAttribute>();
                ValueDefine valueDefine = new ValueDefine(
                    paramInfo.ParameterType,
                    paramAttr != null && !string.IsNullOrEmpty(paramAttr.paramName) ? paramAttr.paramName : "Value",
                    paramAttr != null ? paramAttr.isParams : false,
                    paramAttr != null ? paramAttr.isOut : false);
                if (paramInfo.IsOut || valueDefine.isOut)
                {
                    //如果参数是out参数，那么它是一个输出
                    outputList.Add(valueDefine);
                }
                else if (paramInfo.ParameterType == typeof(ActionNode))
                {
                    //如果参数类型是动作节点，那么它是一个动作分支输出
                    outputList.Add(valueDefine);
                }
                else
                {
                    if (paramAttr != null)
                    {
                        if (paramAttr.isConst)
                        {
                            //用特性指定是常量
                            constList.Add(valueDefine);
                        }
                        else
                        {
                            //用特性指定是输入
                            inputList.Add(valueDefine);
                        }
                    }
                    else if (!typeof(IGame).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(ICard).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType) &&
                        !typeof(Scope).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //不是Game,Card,Buff,EventArg,Scope这种可以缺省的参数也一定是输入
                        inputList.Add(valueDefine);
                    }
                }
            }
            //如果方法返回类型为void或者Task，视为无返回值，否则有返回值
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType != typeof(Task))
            {
                paramAttr = methodInfo.ReturnParameter.GetCustomAttribute<ActionNodeParamAttribute>();
                string returnValueName = paramAttr?.paramName;
                if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    //如果返回类型为Task<T>，则返回值类型视为T
                    outputList.Add(new ValueDefine(
                        methodInfo.ReturnType.GetGenericArguments()[0],
                        string.IsNullOrEmpty(returnValueName) ? "Value" : returnValueName,
                        false, false));
                }
                else
                {
                    outputList.Add(new ValueDefine(
                        methodInfo.ReturnType,
                        string.IsNullOrEmpty(returnValueName) ? "Value" : returnValueName,
                        false, false));
                }
            }
            //设置输入输出
            inputs = inputList.ToArray();
            consts = constList.ToArray();
            outputs = outputList.ToArray();
        }
        public override async Task<object[]> execute(IGame game, ICard card, IBuff buff, IEventArg eventArg, Scope scope, object[] args, object[] constValues)
        {
            object[] paramters = new object[_paramsInfo.Length];
            int actionArgIndex = 0;
            int valueArgIndex = 0;
            int constIndex = 0;
            int actionOutputsCount = getActionOutputs().Length;
            object[] actionArgs = new object[actionOutputsCount];
            Array.Copy(args, 0, actionArgs, 0, actionArgs.Length);
            object[] valueArgs = new object[args.Length - actionOutputsCount];
            Array.Copy(args, actionOutputsCount, valueArgs, 0, valueArgs.Length);
            for (int i = 0; i < _paramsInfo.Length; i++)
            {
                var paramInfo = _paramsInfo[i];
                if (paramInfo.IsOut)
                {
                    //out参数输出留空
                    paramters[i] = null;
                }
                else if (paramInfo.ParameterType == typeof(ActionNode))
                {
                    paramters[i] = actionArgs[actionArgIndex];
                    actionArgIndex++;
                }
                else if (paramInfo.GetCustomAttribute<ActionNodeParamAttribute>() is ActionNodeParamAttribute attribute)
                {
                    //指定了不能省略的参数
                    if (attribute.isOut)
                    {
                        //out参数输出留空
                        paramters[i] = null;
                    }
                    else if (attribute.isConst)
                    {
                        if (isObjectNeedToPackForType(constValues[constIndex], paramInfo.ParameterType))
                            constValues[constIndex] = packObjectToArray(constValues[constIndex], paramInfo.ParameterType.GetElementType());
                        else if (isObjectNeedToUnpackForType(constValues[constIndex], paramInfo.ParameterType))
                            constValues[constIndex] = unpackArrayToObject(constValues[constIndex] as Array);
                        else if (constValues[constIndex] is Array array && isArrayNeedToCastForType(array, paramInfo.ParameterType))
                            constValues[constIndex] = castArrayToTargetTypeArray(array, paramInfo.ParameterType.GetElementType());
                        paramters[i] = constValues[constIndex];
                        constIndex++;
                    }
                    else
                    {
                        if (isObjectNeedToPackForType(valueArgs[valueArgIndex], paramInfo.ParameterType))
                            valueArgs[valueArgIndex] = packObjectToArray(valueArgs[valueArgIndex], paramInfo.ParameterType.GetElementType());
                        else if (isObjectNeedToUnpackForType(valueArgs[valueArgIndex], paramInfo.ParameterType))
                            valueArgs[valueArgIndex] = unpackArrayToObject(valueArgs[valueArgIndex] as Array);
                        else if (valueArgs[valueArgIndex] is Array array && isArrayNeedToCastForType(array, paramInfo.ParameterType))
                            valueArgs[valueArgIndex] = castArrayToTargetTypeArray(array, paramInfo.ParameterType.GetElementType());
                        paramters[i] = valueArgs[valueArgIndex];
                        valueArgIndex++;
                    }
                }
                else
                {
                    if (typeof(IGame).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        paramters[i] = game;
                    }
                    else if (typeof(ICard).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Card
                        paramters[i] = card;
                    }
                    else if (typeof(IBuff).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Buff
                        paramters[i] = buff;
                    }
                    else if (typeof(IEventArg).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的EventArg
                        paramters[i] = eventArg;
                    }
                    else if (typeof(Scope).IsAssignableFrom(paramInfo.ParameterType))
                    {
                        //可以省略的Scope
                        paramters[i] = scope;
                    }
                    else
                    {
                        if (isObjectNeedToPackForType(valueArgs[valueArgIndex], paramInfo.ParameterType))
                            valueArgs[valueArgIndex] = packObjectToArray(valueArgs[valueArgIndex], paramInfo.ParameterType.GetElementType());
                        else if (isObjectNeedToUnpackForType(valueArgs[valueArgIndex], paramInfo.ParameterType))
                            valueArgs[valueArgIndex] = unpackArrayToObject(valueArgs[valueArgIndex] as Array);
                        else if (valueArgs[valueArgIndex] is Array array && isArrayNeedToCastForType(array, paramInfo.ParameterType))
                            valueArgs[valueArgIndex] = castArrayToTargetTypeArray(array, paramInfo.ParameterType.GetElementType());
                        paramters[i] = valueArgs[valueArgIndex];
                        valueArgIndex++;
                    }
                }
            }
            object returnValue = _methodInfo.Invoke(null, paramters);
            if (returnValue is Task task)
            {
                await task;
                //返回Task则视为返回null，返回Task<T>则返回对应值
                if (task.GetType().GetProperty(nameof(Task<object>.Result)) is PropertyInfo propInfo)
                {
                    return new object[] { propInfo.GetValue(task) };
                }
                else
                    return null;
            }
            else
            {
                //不是Task，返回由返回值和out参数组成的数组
                List<object> outputList = new List<object>();
                for (int i = 0; i < _paramsInfo.Length; i++)
                {
                    var paramInfo = _paramsInfo[i];
                    if (paramInfo.IsOut ||
                        (paramInfo.GetCustomAttribute<ActionNodeParamAttribute>() is ActionNodeParamAttribute attr && attr.isOut))
                    {
                        outputList.Add(paramters[i]);
                    }
                }
                outputList.Add(returnValue);
                return outputList.ToArray();
            }
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
            return !(type == typeof(Array) || type.IsArray) && array is Array;
        }
        private bool isArrayNeedToCastForType(Array array, Type type)
        {
            if (array == null)
                return false;
            if (type != typeof(Array) && !type.IsArray)
                return false;
            if (type.IsAssignableFrom(array.GetType()))
                return false;
            return true;
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
        #endregion
        public string methodName => defineName;
        public string category { get; }
        public override ValueDefine[] inputs { get; }
        public override ValueDefine[] consts { get; }
        public override ValueDefine[] outputs { get; }
        MethodInfo _methodInfo;
        ParameterInfo[] _paramsInfo;
    }
}