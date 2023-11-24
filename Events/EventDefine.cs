namespace TouhouCardEngine
{
    public class EventDefine
    {
        public EventReference getReference()
        {
            return new EventReference(cardPoolId, eventName);
        }
        public bool isReference(EventReference reference)
        {
            return reference.cardPoolId == cardPoolId && reference.eventName == eventName;
        }
        #region 属性字段
        public long cardPoolId;
        public string eventName;
        public string editorName;
        public string[] obsoleteNames;
        public EventVariableInfo[] beforeVariableInfos;
        public EventVariableInfo[] afterVariableInfos;
        public EventReference[] childrenEvents;
        #endregion
    }
    public class EventTriggerTime
    {
        public EventTriggerTime(EventDefine define, EventTriggerTimeType type)
        {
            this.define = define;
            this.type = type;
        }
        public string getTimeName()
        {
            return type == EventTriggerTimeType.Before ? EventHelper.getNameBefore(define.eventName) : EventHelper.getNameAfter(define.eventName);
        }
        public EventVariableInfo[] getVariableInfos()
        {
            return type == EventTriggerTimeType.Before ? define.beforeVariableInfos : define.afterVariableInfos;
        }
        public EventDefine define;
        public EventTriggerTimeType type;
    }
    public enum EventTriggerTimeType
    {
        Before,
        After
    }
}
