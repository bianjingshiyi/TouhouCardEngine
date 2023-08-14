using System;
namespace TouhouCardEngine
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ActionNodeMethodAttribute : Attribute
    {
        public ActionNodeMethodAttribute(int defineId, string category, string editorName, NodeDefineType actionType = NodeDefineType.Action, params string[] obsoleteNames)
        {
            this.defineId = defineId;
            this.actionType = actionType;
            this.editorName = editorName;
            this.category = category;
            this.obsoleteNames = obsoleteNames;
        }
        public int defineId { get; }
        public string editorName { get; }
        public NodeDefineType actionType { get; }
        public string category { get; }
        public string[] obsoleteNames { get; }
    }
}