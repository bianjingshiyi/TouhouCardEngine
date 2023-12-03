using System;

namespace TouhouCardEngine
{
    public class EventTriggerTime
    {
        public EventTriggerTime(EventReference defineRef, EventTriggerTimeType type)
        {
            this.defineRef = defineRef;
            this.type = type;
        }
        public EventTriggerTime(EventDefine define, EventTriggerTimeType type) : this(define.getReference(), type)
        {
        }
        public string getTimeName()
        {
            return type == EventTriggerTimeType.Before ? EventHelper.getNameBefore(defineRef.eventName) : EventHelper.getNameAfter(defineRef.eventName);
        }
        public static EventTriggerTime fromName(long cardPoolId, string name)
        {
            var type = EventTriggerTimeType.Before;
            var eventName = name;
            if (name.StartsWith("Before"))
            {
                type = EventTriggerTimeType.Before;
                eventName = name.Substring(6);
            }
            else if (name.StartsWith("After"))
            {
                type = EventTriggerTimeType.After;
                eventName = name.Substring(5);
            }
            return new EventTriggerTime(new EventReference(cardPoolId, eventName), type);
        }
        public override bool Equals(object obj)
        {
            if (obj is EventTriggerTime triggerTime)
            {
                return this == triggerTime;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return 37 * defineRef.GetHashCode() + type.GetHashCode();
        }
        public override string ToString()
        {
            return $"{type}({defineRef})";
        }
        public static bool operator ==(EventTriggerTime lhs, EventTriggerTime rhs)
        {
            if (lhs is null || rhs is null)
                return lhs is null && rhs is null;
            return lhs.defineRef == rhs.defineRef && lhs.type == rhs.type;
        }
        public static bool operator !=(EventTriggerTime lhs, EventTriggerTime rhs)
        {
            return !(lhs == rhs);
        }
        public EventReference defineRef;
        public EventTriggerTimeType type;
    }
    [Serializable]
    public class SerializableEventTriggerTime
    {
        public SerializableEventTriggerTime(EventTriggerTime time)
        {
            defineRef = time.defineRef;
            type = (int)time.type;
        }
        public EventTriggerTime toTriggerTime()
        {
            return new EventTriggerTime(defineRef, (EventTriggerTimeType)type);
        }
        public EventReference defineRef;
        public int type;
    }
    public enum EventTriggerTimeType
    {
        Before,
        After
    }
}
