using System;
using System.Collections;
namespace TouhouCardEngine
{
    public static class StringHelper
    {
        public static string propToString(object prop)
        {
            if (prop is string str)
                return str;
            else if (prop is Array a)
            {
                string s = "[";
                for (int i = 0; i < a.Length; i++)
                {
                    s += propToString(a.GetValue(i));
                    if (i != a.Length - 1)
                        s += ",";
                }
                s += "]";
                return s;
            }
            else if (prop is IEnumerable e)
            {
                string s = "{";
                bool isFirst = true;
                foreach (var obj in e)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        s += ",";
                    s += propToString(obj);
                }
                s += "}";
                return s;
            }
            else if (prop == null)
                return "null";
            else
                return prop.ToString();
        }
    }
}