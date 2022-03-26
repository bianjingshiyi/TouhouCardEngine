using System;
namespace TouhouCardEngine
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ActionNodeMethodAttribute : Attribute
    {
        public ActionNodeMethodAttribute(string methodName, string category, params string[] obsoleteNames)
        {
            this.methodName = methodName;
            this.category = category;
            this.obsoleteNames = obsoleteNames;
        }
        public string methodName { get; }
        public string category { get; }
        public string[] obsoleteNames { get; }
    }
}