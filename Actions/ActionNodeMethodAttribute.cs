using System;
namespace TouhouCardEngine
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ActionNodeMethodAttribute : Attribute
    {
        public ActionNodeMethodAttribute(string defineName, string category, string editorName, params string[] obsoleteNames)
        {
            this.defineName = defineName;
            this.editorName = editorName;
            this.category = category;
            this.obsoleteNames = obsoleteNames;
        }
        public string defineName { get; }
        public string editorName { get; }
        public string category { get; }
        public string[] obsoleteNames { get; }
    }
}