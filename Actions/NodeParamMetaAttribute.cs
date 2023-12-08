using System;
namespace TouhouCardEngine
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public abstract class NodeParamMetaAttribute : Attribute
    {
        public NodeParamMetaAttribute()
        {
        }
        public abstract IPortMeta toPortMeta();
    }
}