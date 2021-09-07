using System.Linq;
using System;
using System.Collections.Generic;

namespace Consensus.Methods
{
    // Calculates satisfaction of selecting a random vote.
    public sealed class RandomVote : VotingMethodBase
    {
        public override Dictionary<Strategy, double> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var randomVoter = voters.Poll(random, 1);
            var winner = randomVoter.Comparers.Single().FirstPreference;            
            var satisfaction = GetSatisfactionWith(voters)(new [] { winner });

            // *literally* strategy-proof!
            return Enum.GetValues<Strategy>().ToDictionary(a => a, _ => satisfaction);
        }
    }
}