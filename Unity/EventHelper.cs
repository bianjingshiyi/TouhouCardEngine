using System;

namespace TouhouCardEngine
{
    public static class EventHelper
    {
        public static string getName(Type eventType)
        {
            string name = eventType.Name;
            if (name.EndsWith("EventDefine"))
                name = string.Intern(name.Substring(0, name.Length - 6));
            return name;
        }
        public static string getName(EventDefine eventDefine)
        {
            return getName(eventDefine.GetType());
        }
        public static string getNameBefore(string eventName)
        {
            return string.Intern("Before" + eventName);
        }
        public static string getNameAfter(string eventName)
        {
            return string.Intern("After" + eventName);
        }
        public static string getNameBefore(EventDefine eventDefine)
        {
            return getNameBefore(getName(eventDefine));
        }
        public static string getNameAfter(EventDefine eventDefine)
        {
            return getNameAfter(getName(eventDefine));
        }
    }
}
