using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Consensus
{
    public sealed class CandidateComparerCollection<T>
        where T : CandidateComparer
    {
        public CandidateComparerCollection(int candidateCount, CountedList<T> comparers)
        {
            CandidateCount = candidateCount;
            Comparers = comparers;
        }
  
        public int CandidateCount { get; }
        public CountedList<T> Comparers { get; }

        ///<summary>
        /// Comparers are separated by newlines or semicolons.
        /// Candidates are lowercase letters.
        /// Comparers may be duplicated by suffixing the line with "* nn", where "nn" is a positive integer.
        ///</summary>
        public static CandidateComparerCollection<T> Parse(string source, int? candidateCount = null)
        {
            candidateCount ??= ParsingUtility.CandidateCount(source);
            if (candidateCount < 2)
                throw new InvalidOperationException("Unexpected number of candidates.");

            return new CandidateComparerCollection<T>(
                candidateCount.Value,
                source
                    .Split(new char[] {'\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(line =>
                    {
                        var starSplit = line.Split('*', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        if (starSplit.Length > 2)
                            throw new InvalidOperationException($"Unexpected number of *s on line '{line}'.");

                        return (
                            Comparer: ParseComparer(candidateCount.Value, starSplit[0]),
                            Count: starSplit.Length == 2
                                ? int.Parse(starSplit[1])
                                : 1);
                    })
                    .ToCountedList());

        }

        public override string ToString() => Comparers
            .Select(a => (Comparer: a.Item.ToString(), Count: a.Count))
            .OrderByDescending(a => a.Count)
            .Select(a => a.Comparer + (a.Count > 1 ? " * " + a.Count : ""))
            .Join("; ");

        public static bool operator==(CandidateComparerCollection<T> first, CandidateComparerCollection<T> second)
        {
            if (first is null && second is null)
                return true;

           if (first is null || second is null)
                return false;

            return first.CandidateCount == second.CandidateCount && first.Comparers == second.Comparers;
        }

        public static bool operator!=(CandidateComparerCollection<T> first, CandidateComparerCollection<T> second) => !(first == second);

        public override bool Equals(object obj) => obj as CandidateComparerCollection<T> == this;
        public override int GetHashCode() => Comparers.GetHashCode();

        // For each member of the collection, maps from one comparer to another.
        public CandidateComparerCollection<TResult> Bind<TResult>(Func<T, TResult> map)
            where TResult : CandidateComparer
             => new CandidateComparerCollection<TResult>(CandidateCount, Comparers.Bind(map));

        // For each memeber of the collection, sorts into one of two collections.
        // Prediate need not be deterministic.
        public (CandidateComparerCollection<T> First, CandidateComparerCollection<T> Second) Sample(Func<T, bool> predicate)
        {
            var (first, second) = Comparers.Sample(predicate);

            return (
                new CandidateComparerCollection<T>(CandidateCount, first),
                new CandidateComparerCollection<T>(CandidateCount, second));
        }

        public static CandidateComparerCollection<T> Concat(CandidateComparerCollection<T> first, CandidateComparerCollection<T> second)
        {
            if (first.CandidateCount != second.CandidateCount)
                throw new ArgumentException("Candidate Counts must be equal.");

            return new CandidateComparerCollection<T>(first.CandidateCount, CountedList<T>.Concat(first.Comparers, second.Comparers));
        }

        // Randomly polls a sample of the collection.
        public CandidateComparerCollection<T> Poll(Random random, int sampleSize)
        {
            return new CandidateComparerCollection<T>(CandidateCount, Comparers.Poll(random, sampleSize));
        }
     
        public CandidateComparerCollection<T> Append(T value)
        {
            if (CandidateCount != value.CandidateCount)
                throw new ArgumentException("Candidate Counts must be equal.");

            var second = new CountedList<T>();
            second.Add(value);
            return new CandidateComparerCollection<T>(CandidateCount, CountedList<T>.Concat(Comparers, second));
        }

        public int Compare(int first, int second)
        {
            var result = 0;
            foreach (var (comparer, count) in Comparers)
                AggregateComparison(ref result, comparer.Compare(first, second), count);

            return result;
        }

        public BeatMatrix GetBeatMatrix() => new BeatMatrix(CandidateCount, Comparers);

        private static void AggregateComparison(ref int result, int preference, int count)
        {
            if (preference < 0)
                result -= count;
            else if (preference > 0)
                result += count;
        }

        public sealed class BeatMatrix : IComparer<int>
        {
            public BeatMatrix(int candidateCount, IEnumerable<(T, int)> comparers)
            {
                m_beatMatrix = new int[candidateCount, candidateCount];

                // Top half does the work
                foreach (var (comparer, count) in comparers)
                {
                    for (var i = 0; i < candidateCount - 1; i++)
                    {
                        for (var j = i + 1; j < candidateCount; j++)
                        {
                            AggregateComparison(ref m_beatMatrix[i, j], comparer.Compare(i, j), count);
                        }
                    }
                }

                // Bottom half is mirrored.
                for (var i = 1; i < candidateCount; i++)
                {
                    for (var j = 0; j < i; j++)
                    {
                        m_beatMatrix[i, j] = -m_beatMatrix[j, i];
                    }
                }
            }

            public List<int> GetSchulzeSet()
            {
                var candidateCount = (int) Math.Sqrt(m_beatMatrix.Length);
                var copelandScores = Enumerable.Range(0, candidateCount)
                    .Select(c => Enumerable.Range(0, candidateCount).Sum(o => Normalize(Compare(c, o))))
                    .ToList();

                // The candidates with the highest Copeland score are guaranteed to be in the Condorcet Schulze set
                var maxScore = copelandScores.Max();
                var schulzeSet = copelandScores.IndexesWhere(s => s == maxScore).ToList();
                var remainingSet = Enumerable.Range(0, candidateCount).ToHashSet();
                remainingSet.ExceptWith(schulzeSet);

                // As are all candidates that don't lose an existing member of the set.
                while (true)
                {
                    var added = remainingSet.Where(c => !schulzeSet.All(s => Beats(s, c))).ToList();

                    if (!added.Any())
                        break;

                    schulzeSet.AddRange(added);
                    remainingSet.ExceptWith(added);
                }

                return schulzeSet;
                
                static int Normalize(int comparer) => comparer == 0 ? comparer : comparer < 0 ? -1 : 1;
            }

            public bool Beats(int first, int second) => m_beatMatrix[first, second] > 0;

            public int Compare(int first, int second) => m_beatMatrix[first, second];

            private readonly int[,] m_beatMatrix;
        }

        
        private static T ParseComparer(int candidateCount, string source) => (T) s_parse.Invoke(null, new object[] { candidateCount, source });
        
        private static readonly MethodInfo s_parse = typeof(T).GetMethod("Parse", 0, BindingFlags.Public | BindingFlags.Static, null, new [] { typeof(int), typeof(string) }, null)
            ?? throw new InvalidOperationException($"Could not find a `public static {typeof(T).Name} Parse(int candicateCount, string source)` method on Comparer {typeof(T).FullName}.");
    }
}