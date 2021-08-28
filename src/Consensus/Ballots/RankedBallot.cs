using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Ballots
{
    public sealed class RankedBallot : CandidateComparer
    {
        public RankedBallot(int candidateCount, List<List<int>> candidateRanking)
            : base (candidateRanking)
        {
            m_ranksByCandidate = GetRanksByCandidate(candidateCount, candidateRanking);
        }

        public RankedBallot(int candidateCount, IEnumerable<IEnumerable<int>> candidateRanking)
        {
            m_ranksByCandidate = GetRanksByCandidate(candidateCount, candidateRanking);
        }

        private static int[] GetRanksByCandidate(int candidateCount, IEnumerable<IEnumerable<int>> candidateRanking)
        {
            var ranksByCandidate = new int[candidateCount];

            // Initialize to a sentinel in case we don't have a full ranking
            for (int i = 0; i < candidateCount; i++)
                ranksByCandidate[i] = -1 * candidateCount;

            // Ranks go negative, such that higher integers are better.
            int rank = 0;
            foreach (var candidates in candidateRanking)
            {
                foreach (var candidate in candidates)
                    ranksByCandidate[candidate] = rank;
                rank--;
            }

            return ranksByCandidate;
        }

        public static RankedBallot Parse(int candidateCount, string source)
        {            
            return new RankedBallot(candidateCount, source.Split(' ').Select(rank => rank.Select(ParsingUtility.DecodeCandidateIndex)));
        }

        public int[] RanksByCandidate => m_ranksByCandidate;

        public override string ToString() => m_ranksByCandidate
            .Select((r, i) => (Rank: r, Candidate: i))
            .Where(a => a.Rank != -1 * CandidateCount)
            .GroupBy(a => a.Rank)
            .OrderByDescending(gp => gp.Key)
            .Select(gp => ParsingUtility.EncodeCandidates(gp.Select(a => a.Candidate)))
            .Join(" ");

        public override int CandidateCount => m_ranksByCandidate.Length;

        protected override int CandidateValue(int candidate) => m_ranksByCandidate[candidate];

        private readonly int[] m_ranksByCandidate;
    }
}