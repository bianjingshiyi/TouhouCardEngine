using System;
namespace TouhouCardEngine
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ActionNodeParamAttribute : Attribute
    {
        public ActionNodeParamAttribute(string paramName, bool isConst = false, bool isParams = false, bool isOut = false, Type eventType = null)
        {
            this.paramName = paramName;
            this.isConst = isConst;
            this.isParams = isParams;
            this.isOut = isOut;
            this.eventType = eventType;
        }
        public string paramName { get; }
        public bool isConst { get; }
        public bool isParams { get; }
        public bool isOut { get; }
        public Type eventType { get; }
    }
}