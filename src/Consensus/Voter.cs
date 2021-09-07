using System.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Consensus
{
    public sealed class Voter : CandidateComparer
    {
        public Voter(int[] utilitiesByCandidate)
        {
            Utilities = utilitiesByCandidate;
        }

        public int FirstPreference => Ranking[0][0];

        public int[] Utilities { get; }

        public override string ToString()
        {
            var numberOfBuckets =  Ranking.Count - (Utilities.Any(u => u == 0) ? 1 : 0);
            return Ranking
                .Where(candidates => Utilities[candidates[0]] != 0)
                .Select((candidates, bucketNumber) =>
                    BucketDefaultUtility(bucketNumber, numberOfBuckets) == Utilities[candidates[0]]
                        ? ParsingUtility.EncodeCandidates(candidates)
                        : Utilities[candidates[0]].ToString() + ParsingUtility.EncodeCandidates(candidates))
                .Join(" ");
        }
        
        ///<summary>
        /// Candidates are lowercase letters.
        /// Utility may be positional or explicit.
        /// Positional utility groups candidates into space-separated buckets, assigning utility 100 to the first bucket and utility 0 to omitted candidates. Buckets in between are evenly pro-rated.
        /// Buckets prefixed by an integer and a colon assign that integer utility to the bucket, overriding any positional utility.
        ///</summary>
        public static Voter Parse(int candidateCount, string voter)
        {
            var buckets = voter.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var utilities = new int[candidateCount];
            for (var i = 0; i < buckets.Length; i++)
            {
                var match = Regex.Match(buckets[i], @"^(-?\d*):?([a-z]+)$");

                if (!match.Success)
                    throw new InvalidOperationException($"Unexpected bucket format '{buckets[i]}'.");

                var utility = int.TryParse(match.Groups[1].Value, out var parsed)
                    ? parsed
                    : BucketDefaultUtility(i, buckets.Length);

                foreach (var c in match.Groups[2].Value)
                    utilities[ParsingUtility.DecodeCandidateIndex(c)] = utility;
            }

            return new Voter(utilities);
        }

        public override int CandidateCount => Utilities.Length;

        protected override int CandidateValue(int candidate) => Utilities[candidate];

        private static int BucketDefaultUtility(int bucketNumber, int numberOfBuckets) => (int) Math.Floor((numberOfBuckets - bucketNumber) * 100m / numberOfBuckets);
    }
}
