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
            // This is here for comparison purposes, not analysis.
            var satisfaction = GetSatisfactionWith(voters)(voters.GetBeatMatrix().GetSchulzeSet());

            return new Dictionary<Strategy, SatisfactionResult>
            {
                { Strategy.Honest, new SatisfactionResult(satisfaction, satisfaction, satisfaction) },
            };
        }

        public override ElectionResults GetElectionResults(string ballots)
        {
            throw new NotSupportedException();
        }
    }
}