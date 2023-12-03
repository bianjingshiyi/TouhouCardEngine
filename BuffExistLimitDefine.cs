using System;

namespace TouhouCardEngine
{
    public class BuffExistLimitDefine
    {
        public EventTriggerTime triggerTime;
        public int count;
    }
    [Serializable]
    public class SerializableBuffExistLimitDefine
    {
        public SerializableBuffExistLimitDefine(BuffExistLimitDefine define)
        {
            triggerTime = new SerializableEventTriggerTime(define.triggerTime);
            count = define.count;
        }
        public BuffExistLimitDefine toDefine()
        {
            var triggerTime = this.triggerTime == null ? EventTriggerTime.fromName(0, eventName) : this.triggerTime.toTriggerTime();
            return new BuffExistLimitDefine()
            {
                triggerTime = triggerTime,
                count = count
            };
        }
        [Obsolete]
        public string eventName;
        public SerializableEventTriggerTime triggerTime;
        public int count;
    }
}
