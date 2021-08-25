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
            
            var satisfaction = new SatisfactionResult(
                    GetSatisfactionWith(voters)(new [] { winner }),
                    0d);

            return new Dictionary<Strategy, SatisfactionResult>
            {
                { Strategy.Honest, satisfaction },
                { Strategy.Strategic, satisfaction },
                { Strategy.FiftyPercentStrategic, satisfaction },
                { Strategy.RunnerUpStrategic, satisfaction },
                { Strategy.FiftyPercentRunnerUpStrategic, satisfaction },
            };
        }

        public override ElectionResults GetElectionResults(string ballots)
        {
            throw new NotSupportedException();
        }
    }
}