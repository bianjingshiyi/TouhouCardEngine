using System;

namespace TouhouCardEngine
{
    public class EventReference
    {
        public EventReference(long cardPoolId, string eventName)
        {
            this.cardPoolId = cardPoolId;
            this.eventName = eventName;
        }
        public override bool Equals(object obj)
        {
            if (obj is EventReference other)
            {
                return cardPoolId == other.cardPoolId && eventName == other.eventName;
            }
            return false;
        }
        public override int GetHashCode()
        {
            //不知道从哪里抄的异或哈希算法，使用了质数。
            int hashCode = 17;
            hashCode = hashCode * 31 + cardPoolId.GetHashCode();
            hashCode = hashCode * 31 + eventName.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            return $"Event({eventName})From({cardPoolId})";
        }
        public static bool operator ==(EventReference r1, EventReference r2)
        {
            if (r1 is null)
            {
                return r2 is null;
            }
            return r1.Equals(r2);
        }
        public static bool operator !=(EventReference r1, EventReference r2)
        {
            if (r1 is null)
            {
                return !(r2 is null);
            }
            return !r1.Equals(r2);
        }

        public long cardPoolId;
        public string eventName;
    }

    [Serializable]
    public class SerializableEventReference
    {
        public SerializableEventReference(EventReference eventRef)
        {
            cardPoolId = eventRef.cardPoolId;
            eventName = eventRef.eventName;
        }
        public EventReference toEventRef()
        {
            return new EventReference(cardPoolId, eventName);
        }
        public long cardPoolId;
        public string eventName;
    }
}
