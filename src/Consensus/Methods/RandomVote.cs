using System.Linq;
using System;
using System.Collections.Generic;

namespace Consensus.Methods
{
    // Calculates satisfaction of selecting a random vote.
    public sealed class RandomVote : VotingMethodBase
    {
        public override Dictionary<Strategy, SatisfactionResult> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var randomVoter = voters.Poll(random, 1);
            var winner = randomVoter.Comparers.Single().FirstPreference;
            
            var satisfaction = GetSatisfactionWith(voters)(new [] { winner });
            var satisfactionResult = new SatisfactionResult(satisfaction, satisfaction, satisfaction);

            // *literally* strategy-proof!
            return new Dictionary<Strategy, SatisfactionResult>
            {
                { Strategy.Honest, satisfactionResult },
                { Strategy.Strategic, satisfactionResult },
                { Strategy.FiftyPercentStrategic, satisfactionResult },
                { Strategy.RunnerUpStrategic, satisfactionResult },
                { Strategy.FiftyPercentRunnerUpStrategic, satisfactionResult },
            };
        }

        public override ElectionResults GetElectionResults(string ballots)
        {
            throw new NotSupportedException();
        }
    }
}