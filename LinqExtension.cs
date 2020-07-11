using System.Collections.Generic;
using System.Linq;
using System;
namespace TouhouCardEngine
{
    public static class LinqExtension
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
        public static IEnumerable<T> randomTake<T>(this IEnumerable<T> enumrable, CardEngine game, int count)
        {
            List<T> list = enumrable.ToList();
            if (list.Count <= count)
                return list;
            T[] results = new T[count];
            for (int i = 0; i < results.Length; i++)
            {
                int index = game.randomInt(0, list.Count - 1);
                results[i] = list[index];
                list.RemoveAt(index);
            }
            return results;
        }
        public static IEnumerable<T> randomTake<T>(this IEnumerable<T> enumrable, Random random, int count)
        {
            List<T> list = enumrable.ToList();
            if (list.Count <= count)
                return list;
            T[] results = new T[count];
            for (int i = 0; i < results.Length; i++)
            {
                int index = random.Next(0, list.Count);
                results[i] = list[index];
                list.RemoveAt(index);
            }
            return results;
        }
    }
}