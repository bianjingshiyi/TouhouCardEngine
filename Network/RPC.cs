using LiteNetLib;
using LiteNetLib.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NitoriNetwork.Common
{
    /// <summary>
    /// RPC执行器
    /// </summary>
    public class RPCExecutor
    {
        Dictionary<string, HashSet<MethodInfo>> MethodList = new Dictionary<string, HashSet<MethodInfo>>();
        Dictionary<string, object> InstanceList = new Dictionary<string, object>();

        /// <summary>
        /// 尝试执行一个RPC请求
        /// </summary>
        /// <param name="request">RPC请求</param>
        /// <param name="result">返回结果</param>
        /// <returns></returns>
        public bool TryInvoke(RPCRequest request, out object result)
        {
            return TryInvoke(request, new object[0], out result);
        }

        /// <summary>
        /// 尝试执行一个RPC请求
        /// </summary>
        /// <param name="request">RPC请求</param>
        /// <param name="scopedInstance">动态插入目标函数的参数</param>
        /// <param name="result">返回结果</param>
        /// <returns></returns>
        public bool TryInvoke(RPCRequest request, object[] scopedInstance, out object result)
        {
            result = null;

            // 判断是否有指定名字的方法
            if (!MethodList.ContainsKey(request.MethodName))
            {
                return false;
            }

            MethodInfo targetMethod = null;
            List<object> args = new List<object>();

            var methods = MethodList[request.MethodName];
            foreach (var method in methods)
            {
                // 返回类型判断
                if (method.ReturnType != request.ReturnType) continue;

                // 参数判断
                var param = method.GetParameters();
                if (param.Length < request.Arguments.Length) continue;

                var offset = param.Length - request.Arguments.Length;

                // 后面参数判断
                bool match = true;
                for (int i = offset; i < param.Length; i++)
                {
                    if (!param[i].ParameterType.IsInstanceOfType(request.Arguments[i - offset]))
                    {
                        match = false;
                        break;
                    }
                }
                if (!match) continue;

                // 前方插入参数判断
                for (int i = 0; i < offset; i++)
                {
                    // 先判断Scope插入的是否有需要的
                    foreach (var obj in scopedInstance)
                    {
                        if (param[i].ParameterType.IsInstanceOfType(obj))
                        {
                            args.Add(obj);
                            break;
                        }
                    }
                    if (args.Count > i) continue;

                    // 再判断Global插入的是否有需要的
                    foreach (var obj in InstanceList.Values)
                    {
                        if (param[i].ParameterType.IsInstanceOfType(obj))
                        {
                            args.Add(obj);
                            break;
                        }
                    }
                    if (args.Count > i) continue;
                    else
                    {
                        match = false;
                        break;
                    }
                }
                if (!match) continue;

                // 找到目标
                targetMethod = method;
                break;
            }

            // 如果没找到
            if (targetMethod == null)
            {
                return false;
            }

            // 查找方法对应的类的Instance
            var cObjType = targetMethod.DeclaringType;
            object cObj = null;

            foreach (var item in scopedInstance)
            {
                if (cObjType.IsInstanceOfType(item))
                {
                    cObj = item;
                    break;
                }
            }
            if (cObj == null)
            {
                if (!InstanceList.ContainsKey(cObjType.FullName))
                    return false;

                cObj = InstanceList[cObjType.FullName];
            }

            // 执行
            args.AddRange(request.Arguments);
            result = targetMethod.Invoke(cObj, args.ToArray());

            return true;
        }

        /// <summary>
        /// 执行一个RPC请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public object Invoke(RPCRequest request)
        {
            object result;
            if (!TryInvoke(request, out result))
            {
                throw new RPCException();
            }
            return result;
        }

        /// <summary>
        /// 执行一个RPC请求
        /// </summary>
        /// <param name="request">RPC请求</param>
        /// <param name="scopedInstance">动态插入目标函数的参数</param>
        /// <returns>返回结果</returns>
        public object Invoke(RPCRequest request, object[] scopedInstance)
        {
            object result;
            if (!TryInvoke(request, scopedInstance, out result))
            {
                throw new RPCException();
            }
            return result;
        }

        /// <summary>
        /// 添加一个方法
        /// </summary>
        /// <param name="method"></param>
        public void AddTargetMethod(MethodInfo method)
        {
            var name = method.Name;
            if (MethodList.ContainsKey(name))
                MethodList[name].Add(method);
            else
            {
                var set = new HashSet<MethodInfo>();
                set.Add(method);
                MethodList.Add(name, set);
            }
        }

        /// <summary>
        /// 添加指定类中的一个方法
        /// </summary>
        /// <typeparam name="T">类</typeparam>
        /// <param name="expression">方法表达式</param>
        public void AddTargetMethod<T>(Expression<Action<T>> expression)
        {
            AddTargetMethod(TypeHelper.GetMethodInfo<T>(expression));
        }

        /// <summary>
        /// 添加一个Singleton作为执行环境插入变量
        /// </summary>
        /// <param name="obj"></param>
        public void AddSingleton(object obj)
        {
            var name = obj.GetType().FullName;
            InstanceList[name] = obj;
        }

        /// <summary>
        /// 将提供的Object的所有公共方法作为可Invoke方法，
        /// 并将此Instance插入变量
        /// </summary>
        /// <param name="obj"></param>
        public void AddTargetObject(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var type = obj.GetType();
            var methods = type.GetMethods();
            foreach (var item in methods)
            {
                AddTargetMethod(item);
            }

            AddSingleton(obj);
        }
    }


    [Serializable]
    public class RPCException : Exception
    {
        public RPCException() { }
        public RPCException(string message) : base(message) { }
        public RPCException(string message, Exception inner) : base(message, inner) { }
        protected RPCException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// 无权限调用某个接口时的Exception，RPC用
    /// </summary>
    [Serializable]
    public class PermissionDenyException : Exception
    {
        public PermissionDenyException() { }
        public PermissionDenyException(string message) : base(message) { }
        public PermissionDenyException(string message, Exception inner) : base(message, inner) { }
        protected PermissionDenyException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// RPC请求
    /// </summary>
    public class RPCRequest
    {
        public Type ReturnType;
        public string MethodName;
        public object[] Arguments;

        /// <summary>
        /// 新建RPC请求
        /// </summary>
        /// <param name="returnType">返回类型</param>
        /// <param name="method">方法名</param>
        /// <param name="args">参数</param>
        public RPCRequest(Type returnType, string method, params object[] args)
        {
            ReturnType = returnType;
            MethodName = method;
            Arguments = args;
        }

        RPCRequest() { }

        /// <summary>
        /// 序列化写出
        /// </summary>
        /// <param name="writer"></param>
        public void Write(NetDataWriter writer)
        {
            writer.Put(ReturnType.FullName);
            writer.Put(MethodName);
            writer.Put(Arguments.Length);
            foreach (object arg in Arguments)
            {
                if (arg != null)
                {
                    writer.Put(arg.GetType().FullName);
                    writer.Put(arg.ToJson());
                }
                else
                    writer.Put(string.Empty);
            }
        }

        /// <summary>
        /// 反序列化读取
        /// </summary>
        /// <param name="reader"></param>
        public void Read(NetDataReader reader)
        {
            string returnTypeName = reader.GetString();
            ReturnType = TypeHelper.getType(returnTypeName);

            MethodName = reader.GetString();
            int argLength = reader.GetInt();
            Arguments = new object[argLength];
            for (int i = 0; i < argLength; i++)
            {
                string typeName = reader.GetString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    string json = reader.GetString();
                    Type objType = TypeHelper.getType(typeName);
                    object obj = BsonSerializer.Deserialize(json, objType);
                    Arguments[i] = obj;
                }
                else
                {
                    Arguments[i] = null;
                }
            }
        }

        /// <summary>
        /// 反序列化读取
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static RPCRequest Parse(NetDataReader reader)
        {
            var req = new RPCRequest();
            req.Read(reader);
            return req;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            return $"function {ReturnType.FullName} {MethodName} ({string.Join(",", Arguments.Select(a => $"{a.GetType().Name} {a}"))})";
        }
    }

    public class RPCRequest<T> : RPCRequest
    {
        /// <summary>
        /// 新建一个请求。注意不要用object代替void。
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public RPCRequest(string method, params object[] args) : base(typeof(T), method, args) { }
    }

    public class TypeHelper
    {
        public static bool tryGetType(string typeName, out Type type)
        {
            type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return true;
                }
                return false;
            }
            else
                return true;
        }

        public static Type getType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        break;
                }
            }
            return type;
        }

        public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
        {
            var member = expression.Body as MethodCallExpression;

            if (member != null)
                return member.Method;

            throw new ArgumentException("Expression is not a method", "expression");
        }
    }

}
