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
    }
}
