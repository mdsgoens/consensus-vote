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

        public static IReadOnlyList<(int Candidate, int Score)> SortCandidates(IEnumerable<ScoreBallot> ballots)
        {
            using (var enumerator = ballots.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elephants.");

                var totals = new int[enumerator.Current.m_scoresByCandidate.Length];

                do
                {
                    for (int i = 0; i < totals.Length; i++)
                        totals[i] += enumerator.Current.m_scoresByCandidate[i];
                }
                while (enumerator.MoveNext());

                return totals
                    .Select((s, i) => (Candidate: i, Score: s))
                    .OrderByDescending(a => a.Score)
                    .ToList();
            }
        }

        public static ScoreBallot Parse(int numberOfCandidates, string source)
        {
            var scoresByCandidate = new int[numberOfCandidates];
            foreach (var bucket in source.Split(' '))
            {
                var splits = bucket.Split(':');
                var bucketValue = int.Parse(splits[0]);
                foreach (var c in splits[1])
                    scoresByCandidate[ParsingUtility.DecodeCandidateIndex(c)] = bucketValue;
            }
            
            return new ScoreBallot(scoresByCandidate);
        }

        public override string ToString() => CandidateScores
            .GroupBy(a => a.Score)
            .OrderByDescending(gp => gp.Key)
            .Select(gp => gp.Key.ToString() + ":" + ParsingUtility.EncodeCandidates(gp.Select(a => a.Candidate)))
            .Join(" ");

        public override int CandidateCount => m_scoresByCandidate.Length;
        protected override int CandidateValue(int candidate) => m_scoresByCandidate[candidate];

        protected IEnumerable<(int Candidate, int Score)> CandidateScores => m_scoresByCandidate
            .Select((s, i) => (Candidate: i, Score: s))
            .Where(a => a.Score > 0);

        private readonly int[] m_scoresByCandidate;
    }
}