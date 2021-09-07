using System.Linq;
using System;
using System.Collections.Generic;

namespace Consensus.Methods
{
    // Calculates satisfaction based on choosing a random member of the Condorcet set
    public sealed class CondorcetRandomVote : VotingMethodBase
    {
        public override Dictionary<Strategy, double> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            // This is here for comparison purposes, not analysis.
            var satisfaction = GetSatisfactionWith(voters)(voters.GetBeatMatrix().GetSchulzeSet());

            return new Dictionary<Strategy, double>
            {
                { Strategy.Honest, satisfaction },
            };
        }
    }
}