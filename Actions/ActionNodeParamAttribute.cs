using System;
namespace TouhouCardEngine
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ActionNodeParamAttribute : Attribute
    {
        public ActionNodeParamAttribute(string paramName, bool isConst = false, bool isParams = false, bool isOut = false)
        {
            this.paramName = paramName;
            this.isConst = isConst;
            this.isParams = isParams;
            this.isOut = isOut;
        }
        public string paramName { get; }
        public bool isConst { get; }
        public bool isParams { get; }
        public bool isOut { get; }
    }
}