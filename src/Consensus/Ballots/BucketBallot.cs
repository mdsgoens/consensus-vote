using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Ballots
{
    ///<summary>
    /// T must be an enum such that larger values are better.
    /// default(T) ought to be the worst ranking.
    ///</summary>
    public sealed class BucketBallot<T> : CandidateComparer
        where T : struct, Enum
    {
        public BucketBallot(T[] bucketsByCandidate)
        {
            m_bucketsByCandidate = bucketsByCandidate;
        }

        public static IReadOnlyDictionary<T, int[]> GetBucketCounts(CandidateComparerCollection<BucketBallot<T>> ballots)
        {
            int candidateCount = ballots.CandidateCount;
            var result = System.Enum.GetValues<T>()
                .ToDictionary(x => x, _ => new int[candidateCount]);

            foreach (var (ballot, count) in ballots.Comparers)
            {
                for (var c = 0; c < candidateCount; c++)
                {
                    var bucket = ballot.m_bucketsByCandidate[c];
                    result[bucket][c] += count;
                }
            }

            return result;
        }

        public static BucketBallot<T> Parse(int candidateCount, string source)
        {
            var bucketsByCandidate = new T[candidateCount];
            foreach (var bucket in source.Split(' '))
            {
                var splits = bucket.Split(':');
                var bucketValue = Enum.Parse<T>(splits[0]);
                foreach (var c in splits[1])
                    bucketsByCandidate[ParsingUtility.DecodeCandidateIndex(c)] = bucketValue;
            }
            
            return new BucketBallot<T> (bucketsByCandidate);
        }


        public override string ToString() =>  m_bucketsByCandidate
            .Select((b, i) => (Bucket: b, Candidate: i))
            .GroupBy(a => a.Bucket)
            .OrderByDescending(gp => gp.Key)
            .Select(gp => gp.Key.ToString() + ":" + ParsingUtility.EncodeCandidates(gp.Select(a => a.Candidate)))
            .Join(" ");

        public override int CandidateCount => m_bucketsByCandidate.Length;

        protected override int CandidateValue(int candidate) => Convert.ToInt32(m_bucketsByCandidate[candidate]);

        private readonly T[] m_bucketsByCandidate;
    }
}
