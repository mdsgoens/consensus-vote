using System.Linq;
using System;
using System.Collections.Generic;

namespace Consensus.Methods
{
    public sealed class RandomVote : VotingMethodBase
    {
        public override Dictionary<Strategy, decimal> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var firstPreferences = voters.Select(v => v.FirstPreference).ToList();
            var winner = firstPreferences[random.Next(firstPreferences.Count)];
            var satisfaction = GetSatisfactionWith(voters)(winner);

            return new Dictionary<Strategy, decimal>
            {
                { Strategy.Honest, satisfaction },
                { Strategy.Strategic, satisfaction },
                { Strategy.FiftyPercentStrategic, satisfaction },
                { Strategy.RunnerUpStrategic, satisfaction },
                { Strategy.FiftyPercentRunnerUpStrategic, satisfaction },
            };
        }
    }
}