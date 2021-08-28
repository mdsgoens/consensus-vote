using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text;

namespace Consensus
{
    // Polls predict the likelihood that a given candidate will win.
    public sealed class Polling
    {
        public static Polling FromBallots<TBallot>(CountedList<CandidateComparerCollection<TBallot>> polls, Func<CandidateComparerCollection<TBallot>, List<List<int>>> getRanking)
            where TBallot : CandidateComparer
        {
            var pollFactor = 1d / polls.Sum(_ => 1);
            int candidateCount = 0;
            double[, ] placeChanceByCandidate = null;

            foreach (var (poll, count) in polls)
            {
                if (placeChanceByCandidate == null)
                {
                    candidateCount = poll.CandidateCount;
                    placeChanceByCandidate = new double[candidateCount, candidateCount];
                }

                var result = getRanking(poll);
                var placeStart = 0;

                foreach (var tier in result)
                {
                    var teirFactor = count * pollFactor / tier.Count;

                    foreach (var candidate in tier)
                    foreach (var place in Enumerable.Range(placeStart, tier.Count))
                        placeChanceByCandidate[place, candidate] += teirFactor;

                    placeStart += tier.Count;
                }
            }

            return new Polling(candidateCount, placeChanceByCandidate);
        }

        private Polling(int candidateCount, double[,] placeChanceByCandidate)
        {
            CandidateCount = candidateCount;
            m_placeChanceByCandidate = placeChanceByCandidate;
            m_probableTopTwos = new (GetProbableTopTwos);
            m_favorite = new (GetFavorite);
        }

        public int CandidateCount { get; }
        public int Favorite => m_favorite.Value;
        public ReadOnlyCollection<(int First, int Second)> ProbableTopTwos => m_probableTopTwos.Value;
        
        public double VictoryChanceByCandidate(int c) => m_placeChanceByCandidate[0, c];

        // EV of a voter based on their utility for each candidate and their liklihood of victory
        public double EV(Voter v)
        {
            var ev = 0d;
            for (int i = 0; i < CandidateCount; i++)
                ev += v.Utilities[i] * VictoryChanceByCandidate(i);

            return ev;
        }

        // EV of a voter based on their utility for each candidate and their liklihood of victory
        public double RunnerUpEV(Voter v)
        {
            var ev = 0d;
            for (int i = 0; i < CandidateCount; i++)
                ev += v.Utilities[i] * m_placeChanceByCandidate[1, i];

            return ev;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("   ");
            for (var i = 0; i < CandidateCount; i++)
            {
                sb.Append("   ");
                sb.Append(ParsingUtility.EncodeCandidateIndex(i));
            }
            for (var i = 0; i < CandidateCount; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(i.ToString().PadRight(2));
                sb.Append("|");
                for (var j = 0; j < CandidateCount; j++)
                {
                    sb.Append((m_placeChanceByCandidate[i, j] * 100).ToString("N0").PadLeft(4));
                }
            }

            return sb.ToString();
        }

        private int GetFavorite()
        {
            var maximumIndex = 0;
            for (int i = 1; i < CandidateCount; i++)
            {
                if (VictoryChanceByCandidate(i) > VictoryChanceByCandidate(maximumIndex))
                    maximumIndex = i;
            }

            return maximumIndex;
        }
        private ReadOnlyCollection<(int First, int Second)> GetProbableTopTwos()
        {
            return (
                from i in Enumerable.Range(0, CandidateCount - 1)
                from j in Enumerable.Range(i + 1, CandidateCount - i - 1)
                let p = m_placeChanceByCandidate[0,i] * m_placeChanceByCandidate[1,j] + m_placeChanceByCandidate[1,i] * m_placeChanceByCandidate[0,j]
                where p > 0
                orderby p descending
                select (i, j))
            .ToList().AsReadOnly();
        }

        private readonly double[,] m_placeChanceByCandidate;
        private readonly Lazy<ReadOnlyCollection<(int First, int Second)>> m_probableTopTwos;
        private readonly Lazy<int> m_favorite;

    }
}
