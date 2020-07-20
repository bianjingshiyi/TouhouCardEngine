using System.Collections.Generic;
using System.Linq;
using System;
namespace TouhouCardEngine
{
    public static class LinqHelper
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
        public static T random<T>(this IEnumerable<T> e, CardEngine game)
        {
            int count = e.Count();
            if (count < 1)
                return default;
            return e.ElementAt(game.randomInt(0, count - 1));
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
        public static IEnumerable<T> skipUntil<T>(this IEnumerable<T> c, Func<T, bool> func)
        {
            return c.SkipWhile(e => !func(e));
        }
        public static IEnumerable<T> takeUntil<T>(this IEnumerable<T> c, Func<T, bool> func)
        {
            return c.TakeWhile(e => !func(e));
        }
        public static bool isSubset<T>(this IEnumerable<T> set, IEnumerable<T> subset)
        {
            return subset.All(e => set.Contains(e));
        }
    }
}