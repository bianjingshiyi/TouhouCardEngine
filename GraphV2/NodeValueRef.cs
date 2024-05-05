using System;
using System.Threading.Tasks;

namespace TouhouCardEngine
{
    public abstract class NodeValueRef
    {
        public NodeValueRef(ValueOutput output)
        {
            outputPort = output;
        }
        public abstract Task<object> getValueObject(Flow flow, FlowScope scope = null);
        public static NodeValueRef<T> FromType<T>(ValueOutput output)
        {
            return FromType(typeof(T), output) as NodeValueRef<T>;
        }
        public static NodeValueRef FromType(Type type, ValueOutput output)
        {
            Type genericType;
            Type defType = typeof(NodeValueRef<>);
            if (type == defType)
            {
                genericType = typeof(NodeValueRef<object>);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == defType)
            {
                genericType = type;
            }
            else
            {
                genericType = defType.MakeGenericType(type);
            }
            var constructor = genericType.GetConstructor(new Type[] { typeof(ValueOutput) });
            var result = constructor.Invoke(new object[] { output });
            return result as NodeValueRef;
        }
        public ValueOutput outputPort;
    }
    public class NodeValueRef<T> : NodeValueRef
    {
        public NodeValueRef(ValueOutput output) : base(output)
        {
        }
        public async override Task<object> getValueObject(Flow flow, FlowScope scope = null)
        {
            return await getValue(flow, scope);
        }
        public async Task<T> getValue(Flow flow, FlowScope scope = null)
        {
            return await flow.getValue<T>(outputPort, scope);
        }
    }
}
