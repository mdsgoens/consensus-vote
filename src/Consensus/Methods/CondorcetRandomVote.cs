using System.Linq;
using System;
using System.Collections.Generic;

namespace Consensus.Methods
{
    // Calculates satisfaction based on choosing a random member of the Condorcet set
    public sealed class CondorcetRandomVote : VotingMethodBase
    {
        public override Dictionary<Strategy, SatisfactionResult> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var beatMatrix = voters.GetBeatMatrix();

            var copelandScores = Enumerable.Range(0, voters.CandidateCount)
                .Select(c => Enumerable.Range(0, voters.CandidateCount).Sum(o => Normalize(beatMatrix.Compare(c, o))))
                .ToList();

            // The candidates with the highest Copeland score are guaranteed to be in the Condorcet Schulze set
            var maxScore = copelandScores.Max();
            var schulzeSet = copelandScores.IndexesWhere(s => s == maxScore).ToList();
            var remainingSet = Enumerable.Range(0, voters.CandidateCount).ToHashSet();
            remainingSet.ExceptWith(schulzeSet);

            // As are all candidates that don't lose an existing member of the set.
            while (true)
            {
                var added = remainingSet.Where(c => !schulzeSet.All(s => beatMatrix.Beats(s, c))).ToList();

                if (!added.Any())
                    break;

                schulzeSet.AddRange(added);
                remainingSet.ExceptWith(added);
            }           

            // Not quite true: There are *definitely* strategic voting incentives in any Condorcet method.
            // This is here for comparison purposes, not analysis.
            var satisfaction = new SatisfactionResult(GetSatisfactionWith(voters)(schulzeSet), 0d);

            return new Dictionary<Strategy, SatisfactionResult>
            {
                { Strategy.Honest, satisfaction },
                { Strategy.Strategic, satisfaction },
                { Strategy.FiftyPercentStrategic, satisfaction },
                { Strategy.RunnerUpStrategic, satisfaction },
                { Strategy.FiftyPercentRunnerUpStrategic, satisfaction },
            };
        }

        private int Normalize(int comparer) => comparer == 0 ? comparer : comparer < 0 ? -1 : 1;
    }
}