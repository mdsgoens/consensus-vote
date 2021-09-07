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

        public RankedBallot(int[] ranksByCandidate)
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

        public IEnumerable<(string Strategy, int, RankedBallot AlternateBallot)> GetPotentialStrategicBallots(List<List<int>> ranking)
        {
            var lowestRank = m_ranksByCandidate.Min();
            var lowestFavoriteRank = ranking.Favorites().Select(i => m_ranksByCandidate[i]).Min();
            var lowestWinnerRank = ranking[0].Select(i => m_ranksByCandidate[i]).Min();

            if (lowestRank < -1)
            {
                // Rank last everyone who's not first.
                yield return (
                    lowestWinnerRank == 0 ? "Post-Winner Truncation"
                        : lowestWinnerRank == -1 ? "Pre-Winner Truncation"
                        : "Truncation",
                    1,
                    NewBallot(i => Math.Max(m_ranksByCandidate[i], -1)));
            }

            if (lowestWinnerRank < 0 && lowestRank + 1 < lowestWinnerRank)
            {
                // Rank last the winners we like least.
                yield return ("Burying", 1, NewBallot(i => 
                    m_ranksByCandidate[i] > lowestWinnerRank
                        ? m_ranksByCandidate[i]
                    : m_ranksByCandidate[i] == lowestWinnerRank && ranking[0].Contains(i)
                        ? lowestRank + 1
                    : m_ranksByCandidate[i] + 1));
            }

            if (lowestWinnerRank < 0 && lowestRank + 1 < lowestWinnerRank)
            {
                // Truncate everyone we like less than the winner
                yield return ("Post-Winner Truncation", 1, NewBallot(i => 
                    m_ranksByCandidate[i] >= lowestWinnerRank
                        ? m_ranksByCandidate[i]
                    : lowestWinnerRank - 1));
            }

            if (lowestWinnerRank < -1 && lowestRank < lowestWinnerRank)
            {
                // Rank the winner (and everyone we like less than them) last.
                yield return ("Pre-Winner Truncation", 1, NewBallot(i => Math.Max(m_ranksByCandidate[i], lowestWinnerRank)));
            }

            if (lowestWinnerRank < 0 && lowestRank < lowestWinnerRank)
            {
                // Rank everyone we like **less** than the winner second!
                yield return ("Dark Horse", 3, NewBallot(i => 
                    m_ranksByCandidate[i] == 0
                        ? 0
                    : m_ranksByCandidate[i] < lowestWinnerRank
                        ? -1
                    : m_ranksByCandidate[i] - 1));
            }

            if (lowestWinnerRank < -1)
            {
                // Artificially raise the ranking of non-favorites we prefer over the winner
                yield return ("Favorite Betrayal", 2, NewBallot(i =>
                    m_ranksByCandidate[i] == 0
                        ? lowestWinnerRank + 1
                    : m_ranksByCandidate[i] > lowestWinnerRank
                        ? m_ranksByCandidate[i] + 1
                    : m_ranksByCandidate[i]));
            }
            
            // Vote for *no-one*.
            yield return ("Abstain", 4, NewBallot(u => 0));

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