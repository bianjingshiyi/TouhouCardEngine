using System;
using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public static class EventHelper
    {
        public static string getName(Type eventType)
        {
            string name = eventType.Name;
            if (name.EndsWith("EventArg"))
                name = string.Intern(name.Substring(0, name.Length - 3));
            return name;
        }
        public static string getName(IEventArg eventArg)
        {
            return getName(eventArg.GetType());
        }
        public static string getNameBefore(string eventName)
        {
            return string.Intern("Before" + eventName);
        }
        public static string getNameAfter(string eventName)
        {
            return string.Intern("After" + eventName);
        }
        public static string getNameBefore(IEventArg eventArg)
        {
            return getNameBefore(getName(eventArg));
        }
        public static string getNameAfter(IEventArg eventArg)
        {
            return getNameAfter(getName(eventArg));
        }
    }
}
