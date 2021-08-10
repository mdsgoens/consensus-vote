using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Consensus
{
    public sealed class CandidateComparerCollection<T> : IEnumerable<T>
        where T : CandidateComparer
    {
        public CandidateComparerCollection(int candidateCount, IEnumerable<T> comparers)
        {
            var (countsByComparer, add) = GroupBuilder<T>();

            CandidateCount = candidateCount;
            m_countsByComparer = countsByComparer;

            foreach (var comparer in comparers)
                add(comparer, 1);
        }

        public CandidateComparerCollection(int candidateCount, Dictionary<T, int> countsByComparer)
        {
            CandidateCount = candidateCount;
            m_countsByComparer = countsByComparer;
        }

        public int CandidateCount { get; }

        ///<summary>
        /// Comparers are separated by newlines.
        /// Candidates are lowercase letters.
        /// Comparers may be duplicated by suffixing the line with "* nn", where "nn" is a positive integer.
        ///</summary>
        public static CandidateComparerCollection<T> Parse(string source, Func<int, string, T> parser)
        {
            var numberOfCandidates = ParsingUtility.NumberOfCandidates(source);
            if (numberOfCandidates < 2)
                throw new InvalidOperationException("Unexpected number of candidates.");
            
            return new CandidateComparerCollection<T>(
                numberOfCandidates, 
                source
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(line => {
                        var starSplit = line.Split('*', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        if (starSplit.Length > 2)
                            throw new InvalidOperationException($"Unexpected number of *s on line '{line}'.");

                        return (
                            Comparer: parser(numberOfCandidates, starSplit[0]),
                            Count: starSplit.Length == 2
                                ? int.Parse(starSplit[1])
                                : 1);
                    })
                    .ToDictionary(a => a.Comparer, a => a.Count));

        }

        public override string ToString() => m_countsByComparer
            .Select(p => (Comparer: p.Key.ToString(), Count: p.Value))
            .OrderByDescending(a => a.Count)
            .ThenBy(a => a.Comparer)
            .Select(a => a.Comparer + (a.Count > 1 ? " * " + a.Count : ""))
            .Join("\r\n");

        public CandidateComparerCollection<TResult> Select<TResult>(Func<T, TResult> map)
            where TResult : CandidateComparer
        {
            var (countsByComparer, add) = GroupBuilder<TResult>();
            
            foreach (var (comparer, count) in m_countsByComparer)
                add(map(comparer), count);

            return new CandidateComparerCollection<TResult>(CandidateCount, countsByComparer);
        }

        public CandidateComparerCollection<TResult> Sample<TResult>(Func<T, bool> predicate, Func<T, TResult> firstMap, Func<T, TResult> secondMap)
            where TResult : CandidateComparer
        {
            var (countsByComparer, add) = GroupBuilder<TResult>();
            foreach (var (comparer, count) in m_countsByComparer)
            {
                var firstCount = Enumerable.Range(0, count).Count(_ => predicate(comparer));
                var secondCount = count - firstCount;

                if (firstCount > 0)
                    add(firstMap(comparer), firstCount);

                if (secondCount > 0)
                    add(secondMap(comparer), secondCount);
            }

            return new CandidateComparerCollection<TResult>(CandidateCount, countsByComparer);
        }

        private (Dictionary<TResult,int> CountsByComparer, Action<TResult, int> Add) GroupBuilder<TResult>()
        {
            var cache = new Dictionary<TResult,TResult>();
            var countsByComparer = new Dictionary<TResult,int>();

            return (countsByComparer, (comparer, count) => {
                if (cache.TryGetValue(comparer, out var existingInstance))
                {
                    countsByComparer[existingInstance] += count;
                }
                else
                {
                    cache[comparer] = comparer;
                    countsByComparer[comparer] = count;
                }
            });
        }

        public int Sum(Func<T, int> selector) => m_countsByComparer.Sum(p => p.Value * selector(p.Key));

        public int Compare(int first, int second)
        {
            var result = 0;
            foreach (var (comparer, count) in m_countsByComparer)
                AggregateComparison(ref result, comparer.Compare(first, second), count);

            return result;
        }

        public BeatMatrix GetBeatMatrix() => new BeatMatrix(CandidateCount, m_countsByComparer);

        private static void AggregateComparison(ref int result, int preference, int count)
        {
            if (preference < 0)
                result -= count;
            else if (preference > 0)
                result += count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var (comparer, count) in m_countsByComparer)
            {
                for (int i = 0; i < count; i++)
                    yield return comparer;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public sealed class BeatMatrix : IComparer<int>
        {
            public BeatMatrix(int candidateCount, IEnumerable<KeyValuePair<T, int>> comparers)
            {
                using (var enumerator = comparers.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                        throw new InvalidOperationException("Sequence contains no elephants.");

                    m_beatMatrix = new int[candidateCount, candidateCount];

                    do
                    {
                        for (var i = 0; i < candidateCount; i++)
                        for (var j = i + 1; j < candidateCount; j++)
                        {
                            AggregateComparison(ref m_beatMatrix[i,j], enumerator.Current.Key.Compare(i, j), enumerator.Current.Value);
                        }                        
                    } while (enumerator.MoveNext());            
                }

                for (var i = 0; i < candidateCount; i++)
                for (var j = 0; j < i; j++)
                {
                    m_beatMatrix[i,j] = -m_beatMatrix[j,i];
                }
            }

            public bool Beats(int first, int second) => m_beatMatrix[first, second] > 0;

            int IComparer<int>.Compare(int first, int second) => m_beatMatrix[first, second];

            private readonly int[,] m_beatMatrix;
        }
        
        private readonly Dictionary<T, int> m_countsByComparer;
    }
}