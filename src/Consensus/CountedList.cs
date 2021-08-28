using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Consensus
{
    // Stores a list likely to have many duplicates.
    public sealed class CountedList<T> : IEnumerable<(T Item, int Count)>, IEquatable<CountedList<T>>
    {
        public void Add(T item) => Add(item, 1);

        public void Add(T item, int count)
        {
            m_counts[item] = m_counts.TryGetValue(item, out var value)
                ? value + count
                : count;
        }

        // For each member of the collection, maps from one comparer to another.
        public CountedList<TResult> Select<TResult>(Func<T, TResult> map)
            where TResult : CandidateComparer
        {
            var counter = new CountedList<TResult>();

            foreach (var (item, count) in m_counts)
                counter.Add(map(item), count);

            return counter;
        }

        // For each memeber of the collection, sorts into one of two collections.
        // Prediate need not be deterministic.
        public (CountedList<T> First, CountedList<T> Second) Sample(Func<T, bool> predicate)
        {
            var first = new CountedList<T>();
            var second = new CountedList<T>();
            foreach (var (comparer, count) in m_counts)
            {
                var firstCount = Enumerable.Range(0, count).Count(_ => predicate(comparer));
                var secondCount = count - firstCount;

                if (firstCount > 0)
                   first.m_counts[comparer] = firstCount;

                if (secondCount > 0)
                   second.m_counts[comparer] = firstCount;
            }

            return (first, second);
        }

        public static CountedList<T> Concat(CountedList<T> first, CountedList<T> second)
        {
            var counter = new CountedList<T>();

            foreach (var (comparer, count) in first.m_counts)
                counter.Add(comparer, count);
            foreach (var (comparer, count) in second.m_counts)
                counter.Add(comparer, count);

            return counter;
        }

        // Randomly polls a sample of the collection.
        public CountedList<T> Poll(Random random, int sampleSize)
        {
            if (sampleSize < 1)
                throw new ArgumentException("Must be positive", nameof(sampleSize));

            var count = m_counts.Sum(a => a.Value);
            
            if (sampleSize > count)
                throw new ArgumentException("Must be smaller than collection size", nameof(sampleSize));

            if (sampleSize == count)
                return this;

            var audience = new HashSet<int>(sampleSize);
            while (audience.Count < sampleSize)
                audience.Add(random.Next(count));

            var currentLimit = 0;
            var isComplete = false;
            var pollResult = new CountedList<T>();

            using var comparerEnumerator = m_counts.GetEnumerator();
            using var audienceEnumerator = audience.OrderBy(x => x).GetEnumerator();

            if (!audienceEnumerator.MoveNext())
                throw new InvalidOperationException("There's a bug!");
            
            while (!isComplete)
            {
                if (!comparerEnumerator.MoveNext())
                    throw new InvalidOperationException("There's a bug!");

                currentLimit += comparerEnumerator.Current.Value;

                var currentCount = 0;
                while (audienceEnumerator.Current < currentLimit)
                {
                    currentCount++;
                    if (!audienceEnumerator.MoveNext())
                    {
                        isComplete = true;
                        break;
                    }
                }

                if (currentCount > 0)
                    pollResult.m_counts[comparerEnumerator.Current.Key] = currentCount;
            }

            return pollResult;
        }
        
        public CountedList<T> Replace(T original, T replacement)
        {
            if (original.Equals(replacement) || !m_counts.ContainsKey(original))
                return this;
                
            var result = new CountedList<T>();

            foreach (var (item, count) in m_counts)
                result.Add(original.Equals(item) ? replacement : item, count);

            return result;
        }

        public int Sum(Func<T, int> selector) => m_counts.Sum(p => p.Value * selector(p.Key));

        public T Single() => m_counts.Single().Value == 1
            ? m_counts.Single().Key 
            : throw new InvalidOperationException("Sequence does not contain exactly one elephant.");

        public bool TryGetCount(T item, out int count) => m_counts.TryGetValue(item, out count);

        public static bool operator==(CountedList<T> first, CountedList<T> second)
        {
            if (first is null && second is null)
                return true;

           if (first is null || second is null)
                return false;

            if (first.GetHashCode() != second.GetHashCode())
                return false;

            foreach (var (comparer, firstCount) in first.m_counts)
            {
                if (!second.m_counts.TryGetValue(comparer, out var secondCount) || firstCount != secondCount)
                    return false;
            }

            return true;
        }

        public static bool operator!=(CountedList<T> first, CountedList<T> second) => !(first == second);

        public override bool Equals(object obj) => obj as CountedList<T> == this;

        public bool Equals(CountedList<T>  obj) => obj as CountedList<T> == this;
        public override int GetHashCode() => m_hashCode ??= m_counts.Aggregate(0x2D2816FE, (res, c) => res * 31 + c.Value.GetHashCode() * c.Value);

        public IReadOnlyDictionary<T, int> AsReadOnly() => new ReadOnlyDictionary<T, int>(m_counts);

        public IEnumerator<(T Item, int Count)> GetEnumerator()
        {
            foreach (var pair in m_counts)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        private readonly Dictionary<T, int> m_counts = new Dictionary<T, int>();
        private int? m_hashCode = null;
    }
}