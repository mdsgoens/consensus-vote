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

        private RankedBallot(int[] ranksByCandidate)
        {
            m_ranksByCandidate = ranksByCandidate;
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

        public IEnumerable<(Strategy Strategy, RankedBallot AlternateBallot)> GetPotentialStrategicBallots(List<int> winners)
        {
            yield return (Strategy.Honest, this);

            var lowestRank = m_ranksByCandidate.Min();
            var lowestWinnerRank = winners.Select(i => m_ranksByCandidate[i]).Min();

            if (lowestRank < -1)
            {
                // Rank last everyone who's not first.
                yield return (
                    lowestWinnerRank == 0 ? Strategy.PostWinnerTruncation 
                        : lowestWinnerRank == -1 ? Strategy.PreWinnerTruncation
                        : Strategy.CompleteTruncation,
                     NewBallot(i => Math.Max(m_ranksByCandidate[i], -1)));
            }

            if (lowestWinnerRank < 0 && lowestRank + 1 < lowestWinnerRank)
            {
                // Rank last the winners we like least.
                yield return (Strategy.Burying, NewBallot(i => 
                    m_ranksByCandidate[i] > lowestWinnerRank
                        ? m_ranksByCandidate[i]
                    : m_ranksByCandidate[i] == lowestWinnerRank && winners.Contains(i)
                        ? lowestRank + 1
                    : m_ranksByCandidate[i] + 1));
            }

            if (lowestWinnerRank < 0 && lowestRank + 1 < lowestWinnerRank)
            {
                // Truncate everyone we like less than the winner
                yield return (Strategy.PostWinnerTruncation, NewBallot(i => 
                    m_ranksByCandidate[i] >= lowestWinnerRank
                        ? m_ranksByCandidate[i]
                    : lowestWinnerRank - 1));
            }

            if (lowestWinnerRank < -1 && lowestRank < lowestWinnerRank)
            {
                // Rank the winner (and everyone we like less than them) last.
                yield return (Strategy.PreWinnerTruncation, NewBallot(i => Math.Max(m_ranksByCandidate[i], lowestWinnerRank)));
            }

            if (lowestWinnerRank < 0 && lowestRank < lowestWinnerRank)
            {
                // Rank everyone we like **less** than the winner second!
                yield return (Strategy.DarkHorse, NewBallot(i => 
                    m_ranksByCandidate[i] == 0
                        ? 0
                    : m_ranksByCandidate[i] < lowestWinnerRank
                        ? -1
                    : m_ranksByCandidate[i] - 1));
            }

            if (lowestWinnerRank < -1)
            {
                // Artificially raise the ranking of non-favorites we prefer over the winner
                yield return (Strategy.FavoriteBetrayal, NewBallot(i =>
                    m_ranksByCandidate[i] == 0
                        ? lowestWinnerRank + 1
                    : m_ranksByCandidate[i] > lowestWinnerRank
                        ? m_ranksByCandidate[i] + 1
                    : m_ranksByCandidate[i]));
            }

            RankedBallot NewBallot(Func<int, int> getNewRank)
            {
                var newRanks = new int[m_ranksByCandidate.Length];
                for (var i = 0; i < m_ranksByCandidate.Length; i++)
                {
                    newRanks[i] = getNewRank(i);
                }

                return new RankedBallot(newRanks);
            }
        }

        public override string ToString() => m_ranksByCandidate
            .Select((r, i) => (Rank: r, Candidate: i))
            .Where(a => a.Rank != -1 * CandidateCount)
            .GroupBy(a => a.Rank)
            .OrderByDescending(gp => gp.Key)
            .Select(gp => ParsingUtility.EncodeCandidates(gp.Select(a => a.Candidate)))
            .Join(" ");

        public override int CandidateCount => m_ranksByCandidate.Length;

        public enum Strategy
        {
            Honest,
            Burying,
            PostWinnerTruncation,
            PreWinnerTruncation,
            CompleteTruncation,
            DarkHorse,
            FavoriteBetrayal
        }

        protected override int CandidateValue(int candidate) => m_ranksByCandidate[candidate];

        private readonly int[] m_ranksByCandidate;
    }
}