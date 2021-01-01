using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Methods
{
    public abstract class VotingMethodBase
    {
        public abstract Dictionary<Strategy, decimal> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters);

        public enum Strategy
        {
            Honest,
            Strategic,
            FiftyPercentStrategic,
            RunnerUpStrategic,
            FiftyPercentRunnerUpStrategic,
        }

        protected Func<int, decimal> GetSatisfactionWith(CandidateComparerCollection<Voter> voters)
        {
            var satisfactions = Enumerable.Range(0, voters.CandidateCount)
                .Select(i => voters.Sum(v => v.Utilities[i]))
                .ToList();
            var randomSatisfaction = satisfactions.Sum() / (decimal) voters.CandidateCount;
            var maximumSatisfaction = satisfactions.Max();

            if (randomSatisfaction == maximumSatisfaction)
                return _ => 1m;

           return (int candidate) => (satisfactions[candidate] - randomSatisfaction) / (maximumSatisfaction - randomSatisfaction);
        }

        public record Tally(int Winner, int[] RunnersUp);
    }

    public abstract class VotingMethodBase<TBallot, TTally> : VotingMethodBase
        where TTally : VotingMethodBase.Tally
        where TBallot : CandidateComparer
    {
        public override Dictionary<Strategy, decimal> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var satisfactionWith = GetSatisfactionWith(voters);
            var getHonestBallot = Memoize(GetHonestBallot);
            var honestBallots = voters.Select(getHonestBallot);
            var honestTally = GetTally(honestBallots);
            var getStrategicBallot = Memoize(v => GetStrategicBallot(honestTally, v));

            return new Dictionary<Strategy, decimal>
            {
                { Strategy.Honest, satisfactionWith(honestTally.Winner) },
                { Strategy.Strategic, SatisfactionWithWinnerWhenIsStrategic(_ => true) },
                { Strategy.FiftyPercentStrategic, SatisfactionWithWinnerWhenIsStrategic(_ => random.NextDouble() < .5d) },
                { Strategy.RunnerUpStrategic, SatisfactionWithWinnerWhenIsStrategic(PrefersRunnerUp) },
                { Strategy.FiftyPercentRunnerUpStrategic, SatisfactionWithWinnerWhenIsStrategic(v => PrefersRunnerUp(v) && random.NextDouble() < .5d) },
            };

            decimal SatisfactionWithWinnerWhenIsStrategic(Func<Voter, bool> isStrategic) => satisfactionWith(GetTally(voters.Sample(isStrategic, getStrategicBallot, getHonestBallot)).Winner);

            bool PrefersRunnerUp(Voter v) => honestTally.RunnersUp.Any(r => v.Prefers(r, honestTally.Winner));
        }

        public abstract TBallot GetHonestBallot(Voter v);
        public abstract TTally GetTally(CandidateComparerCollection<TBallot> ballots);
        public abstract TBallot GetStrategicBallot(TTally tally, Voter v);

        private static Func<Voter, TBallot> Memoize(Func<Voter, TBallot> map)
        {
            var cache = new Dictionary<Voter, TBallot>();
            return value => cache.TryGetValue(value, out var result) ? result : cache[value] = result;
        }
    }
}
