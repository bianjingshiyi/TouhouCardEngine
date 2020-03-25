using System.Collections.Generic;
using System.Linq;

namespace TouhouCardEngine
{
    public static class ShuffleExtension
    {
        public static IEnumerable<T> shuffle<T>(this IEnumerable<T> enumrable, CardEngine engine)
        {
            T[] array = enumrable.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                int index = engine.randomInt(i, array.Length - 1);
                T t = array[i];
                array[i] = array[index];
                array[index] = t;
            }
            return array;
        }
    }
}