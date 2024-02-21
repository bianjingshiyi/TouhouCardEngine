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
            triggerTime = define.triggerTime;
            count = define.count;
        }
        public BuffExistLimitDefine toDefine()
        {
            var triggerTime = this.triggerTime == null ? EventTriggerTime.fromName(0, eventName) : this.triggerTime;
            return new BuffExistLimitDefine()
            {
                triggerTime = triggerTime,
                count = count
            };
        }
        [Obsolete]
        public string eventName;
        public EventTriggerTime triggerTime;
        public int count;
    }
}
