using System;
using System.Threading.Tasks;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public abstract class EventDefine
    {
        public virtual EventReference getReference()
        {
            return new EventReference(cardPoolId, eventName);
        }
        public bool isReference(EventReference reference)
        {
            return reference.cardPoolId == cardPoolId && reference.eventName == eventName;
        }
        public EventVariableInfo[] getVariableInfos(EventTriggerTimeType type)
        {
            return type == EventTriggerTimeType.Before ? beforeVariableInfos : afterVariableInfos;
        }
        [Obsolete]
        public abstract void Record(CardEngine game, EventArg arg, EventRecord record);
        public abstract Task execute(IEventArg arg);
        public virtual string toString(EventArg arg) => ToString();
        #region 属性字段
        public long cardPoolId;
        public string eventName;
        public string[] obsoleteNames;
        public EventVariableInfo[] beforeVariableInfos;
        public EventVariableInfo[] afterVariableInfos;
        public EventReference[] childrenEvents;
        #endregion
    }
}
