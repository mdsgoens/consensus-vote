using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Methods
{
    public abstract class VotingMethodBase
    {
        public abstract Dictionary<Strategy, SatisfactionResult> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters);

        public enum Strategy
        {
            Honest,
            Strategic,
            FiftyPercentStrategic,
            RunnerUpStrategic,
            FiftyPercentRunnerUpStrategic,
        }

        public record SatisfactionResult(double Satisfaction, double StrategyRatio);

        protected Func<IEnumerable<int>, double> GetSatisfactionWith(CandidateComparerCollection<Voter> voters)
        {
            var satisfactions = Enumerable.Range(0, voters.CandidateCount)
                .Select(i => voters.Comparers.Sum(v => v.Utilities[i]))
                .ToList();
            var randomSatisfaction = satisfactions.Sum() / (double) voters.CandidateCount;
            var maximumSatisfaction = satisfactions.Max();

            if (randomSatisfaction == maximumSatisfaction)
                return _ => 1d;

           return (IEnumerable<int> winners) => winners.Average(candidate => (satisfactions[candidate] - randomSatisfaction) / (maximumSatisfaction - randomSatisfaction));
        }
    }

    public abstract class VotingMethodBase<TBallot> : VotingMethodBase
        where TBallot : CandidateComparer
    {
        public override Dictionary<Strategy, SatisfactionResult> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var overallSatisfactionWith = GetSatisfactionWith(voters);
            var getHonestBallot = Memoize(GetHonestBallot);
            var honestBallots = voters.Select(getHonestBallot);
            var polling = GetPolling(random, honestBallots);
            var getStrategicBallot = Memoize(v => GetStrategicBallot(polling, v));
            var honestWinners = GetRanking(honestBallots)[0];

            return new Dictionary<Strategy, SatisfactionResult>
            {
                { Strategy.Honest, new SatisfactionResult(overallSatisfactionWith(honestWinners), 0d) },
                { Strategy.Strategic, SatisfactionWithWinnerWhenIsStrategic(_ => true) },
                { Strategy.FiftyPercentStrategic, SatisfactionWithWinnerWhenIsStrategic(_ => random.NextDouble() < .5d) },
                { Strategy.RunnerUpStrategic, SatisfactionWithWinnerWhenIsStrategic(PrefersRunnerUp) },
                { Strategy.FiftyPercentRunnerUpStrategic, SatisfactionWithWinnerWhenIsStrategic(v => PrefersRunnerUp(v) && random.NextDouble() < .5d) },
            };

            SatisfactionResult SatisfactionWithWinnerWhenIsStrategic(Func<Voter, bool> isStrategic)
            {
                var (strategicVoters, honestVoters) = voters.Sample(isStrategic);

                var strategicWinners = GetRanking(
                    CandidateComparerCollection<TBallot>.Concat(
                        strategicVoters.Select(getStrategicBallot),
                        honestVoters.Select(getHonestBallot)))[0];
                    
                var overallSatisfaction = overallSatisfactionWith(strategicWinners);

                var strategicVoterSatisfactionWith = GetSatisfactionWith(strategicVoters);
                var strategicVoterSatisfactionWithStrategicOutcome = strategicVoterSatisfactionWith(strategicWinners);
                var strategicVoterSatisfactionWithHonestOutcome = strategicVoterSatisfactionWith(honestWinners);

                return new SatisfactionResult(overallSatisfaction, (strategicVoterSatisfactionWithStrategicOutcome - strategicVoterSatisfactionWithHonestOutcome) / strategicVoterSatisfactionWithHonestOutcome);
            }

            // Indicates if a voter has incentive to "shake up" the race
            bool PrefersRunnerUp(Voter v) => polling.EV(v) > v.Utilities[polling.Favorite];
        }

        public abstract TBallot GetHonestBallot(Voter v);
        public abstract List<List<int>> GetRanking(CandidateComparerCollection<TBallot> ballots);
        public virtual TBallot GetStrategicBallot(Polling poll, Voter v) => GetHonestBallot(v);

        private static Func<Voter, TBallot> Memoize(Func<Voter, TBallot> map)
        {
            var cache = new Dictionary<Voter, TBallot>();
            return value => cache.TryGetValue(value, out var result) ? result : cache[value] = map(value);
        }

        private Polling GetPolling(Random random, CandidateComparerCollection<TBallot> ballots)
        {
            var pollCount = 100;
            var sampleSize = 10;

            var polls = Enumerable.Range(0, pollCount)
                .Select(_ => ballots.Poll(random, sampleSize))
                .ToCountedList();

            return Polling.FromBallots(polls, GetRanking);
        }
    }
}
