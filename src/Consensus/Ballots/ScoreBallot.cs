using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Ballots
{
    public class ScoreBallot : CandidateComparer
    {
        public ScoreBallot(IEnumerable<int> scoresByCandidate)
        {
            m_scoresByCandidate = scoresByCandidate.ToArray();
        }

        public static IReadOnlyList<(int Candidate, int Score)> SortCandidates<T>(CandidateComparerCollection<T> ballots)
            where T : ScoreBallot
        {
            var totals = new int[ballots.CandidateCount];

            foreach (var (ballot, count) in ballots.Comparers)
            {
                for (int i = 0; i < totals.Length; i++)
                    totals[i] += ballot.m_scoresByCandidate[i] * count;
            }

            return totals
                .Select((s, i) => (Candidate: i, Score: s))
                .OrderByDescending(a => a.Score)
                .ToList();
        }

        public static List<List<int>> GetRanking<T>(CandidateComparerCollection<T> ballots)
            where T : ScoreBallot
        {
            return SortCandidates(ballots)
                .GroupBy(a => a.Score, a => a.Candidate)
                .Select(gp => gp.ToList())
                .ToList();
        }

        public static ScoreBallot Parse(int candidateCount, string source)
        {
            var scoresByCandidate = new int[candidateCount];
            foreach (var bucket in source.Split(' '))
            {
                var splits = bucket.Split(':');

                if (splits.Length != 2)
                    throw new InvalidOperationException($"Expected exactly one `:`, got '{bucket}' instead.");

                var bucketValue = int.Parse(splits[0]);
                foreach (var c in splits[1])
                    scoresByCandidate[ParsingUtility.DecodeCandidateIndex(c)] = bucketValue;
            }
            
            return new ScoreBallot(scoresByCandidate);
        }

        public override string ToString() => CandidateScores
            .GroupBy(a => a.Score)
            .OrderByDescending(gp => gp.Key)
            .Select(gp => gp.Key.ToString() + ":" + ParsingUtility.EncodeCandidates(gp.Select(a => a.Candidate).OrderBy(x => x)))
            .Join(" ");

        public override int CandidateCount => m_scoresByCandidate.Length;
        protected override int CandidateValue(int candidate) => m_scoresByCandidate[candidate];

        protected IEnumerable<(int Candidate, int Score)> CandidateScores => m_scoresByCandidate
            .Select((s, i) => (Candidate: i, Score: s))
            .Where(a => a.Score > 0);

        private readonly int[] m_scoresByCandidate;
    }
}