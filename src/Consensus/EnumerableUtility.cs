using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus
{
    public static class EnumerableUtility
    {
        public static CountedList<T> ToCountedList<T>(this IEnumerable<T> source)
        {
            var list = new CountedList<T>();
            
            foreach (var item in source)
                list.Add(item, 1);

            return list;
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