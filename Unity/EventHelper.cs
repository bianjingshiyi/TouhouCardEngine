using TouhouCardEngine.Interfaces;

namespace TouhouCardEngine
{
    public static class EventHelper
    {
        public static string getName(IEventArg eventArg)
        {
            string name = eventArg.GetType().Name;
            if (name.EndsWith("EventArg"))
                name = string.Intern(name.Substring(0, name.Length - 3));
            return name;
        }
        public static string getNameBefore(IEventArg eventArg)
        {
            return string.Intern("Before" + getName(eventArg));
        }
        public static string getNameAfter(IEventArg eventArg)
        {
            return string.Intern("After" + getName(eventArg));
        }
    }
}
