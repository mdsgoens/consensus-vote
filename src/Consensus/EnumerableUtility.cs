using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus
{
    public static class EnumerableUtility
    {
        public static T[] LengthArray<T>(this int length, Func<int, T> getElement)
        {
            var result = new T[length];
            for (var i = 0; i < length; i++)
                result[i] = getElement(i);
            return result;
        }

        public static CountedList<T> ToCountedList<T>(this IEnumerable<T> source)
        {
            var list = new CountedList<T>();
            
            foreach (var item in source)
                list.Add(item, 1);

            return list;
        }

        public static List<int> Favorites(this List<List<int>> ranking)
        {
            if (ranking.Count == 1 || ranking[0].Count > 1)
                return ranking[0];
            else 
                return ranking[0].Concat(ranking[1]).ToList();
        }
      
        public static CountedList<T> ToCountedList<T>(this IEnumerable<(T, int)> source)
        {
            var list = new CountedList<T>();
            
            foreach (var (item, count) in source)
                list.Add(item, count);

            return list;
        }

        public static IEnumerable<int> IndexesWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                    yield return index;
                index++;
            }
        }

        public static IEnumerable<int> IndexesWhere<T>(this IEnumerable<T> source, Func<T, int, bool> predicate)
        {
            int index = 0;
            foreach (var item in source)
            {
                if (predicate(item, index))
                    yield return index;
                index++;
            }
        }

        public static IEnumerable<int> IndexOrderByDescending<T>(this IEnumerable<T> source)
            => source.Select((source, index) => (source, index))
            .OrderByDescending(a => a.source)
            .Select(a => a.index);

        public static List<List<int>> IndexRanking<T>(this IEnumerable<T> source) => source
            .Select((source, index) => (source, index))
            .GroupBy(a => a.source, a => a.index)
            .OrderByDescending(gp => gp.Key)
            .Select(gp => gp.ToList())
            .ToList();
    }

}