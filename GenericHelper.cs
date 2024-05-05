using System;

namespace TouhouCardEngine
{
    public static class GenericHelper
    {
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic, out Type[] genericArgs)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    genericArgs = toCheck.GetGenericArguments();
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            genericArgs = null;
            return false;
        }
        public static bool HasInterfaceOfRawGeneric(this Type toCheck, Type generic, out Type[] genericArgs)
        {
            var interfaces = toCheck.GetInterfaces();
            foreach (var i in interfaces)
            {
                var interfaceType = i.IsGenericType ? i.GetGenericTypeDefinition() : i;
                if (generic == interfaceType)
                {
                    genericArgs = i.GetGenericArguments();
                    return true;
                }
            }
            genericArgs = null;
            return false;
        }
    }
}
