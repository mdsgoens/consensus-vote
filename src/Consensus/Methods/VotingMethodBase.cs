using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Methods
{
    public abstract class VotingMethodBase
    {
        public abstract Dictionary<Strategy, SatisfactionResult> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters);
        public abstract ElectionResults GetElectionResults(string ballots);

        public enum Strategy
        {
            Honest,
            Strategic,
            FiftyPercentStrategic,
            RunnerUpStrategic,
            FiftyPercentRunnerUpStrategic,
        }

        public record SatisfactionResult(double AllVoterSatisfaction, double StrategicVoterSatisfaction, double StrategicVoterSatisfactionWithHonestOutcome);

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
            var honestBallots = voters.Bind(getHonestBallot);
            var honestRanking = GetRanking(honestBallots);
            var honestWinners = honestRanking[0];
            var satisfaction = overallSatisfactionWith(honestWinners);

            var supportsPotentialBallots = GetType().GetMethod(nameof(GetPotentialStrategicBallots), new [] { typeof(List<List<int>>), typeof(Voter) }).DeclaringType != typeof(VotingMethodBase<TBallot>);

            if (!supportsPotentialBallots)
            {
                return new Dictionary<Strategy, SatisfactionResult>
                {
                    { Strategy.Honest, new SatisfactionResult(satisfaction, satisfaction, satisfaction) },
                };
            }

            var pollCount = 100;
            var sampleSize = 10;

            var polls = Enumerable.Range(0, pollCount)
                .Select(_ => voters.Poll(random, sampleSize))
                .ToCountedList()
                .Bind(p => p.Bind(getHonestBallot));

            var getStrategicBallot = GetGetStrategicBallot();
            
            return new Dictionary<Strategy, SatisfactionResult>
            {
                { Strategy.Honest, new SatisfactionResult(satisfaction, satisfaction, satisfaction) },
                { Strategy.Strategic, SatisfactionWithWinnerWhenIsStrategic(_ => true) },
               /*  { Strategy.FiftyPercentStrategic, SatisfactionWithWinnerWhenIsStrategic(_ => random.NextDouble() < .5d) },
               { Strategy.RunnerUpStrategic, SatisfactionWithWinnerWhenIsStrategic(PrefersRunnerUp) },
                { Strategy.FiftyPercentRunnerUpStrategic, SatisfactionWithWinnerWhenIsStrategic(v => PrefersRunnerUp(v) && random.NextDouble() < .5d) }, */
            };

            SatisfactionResult SatisfactionWithWinnerWhenIsStrategic(Func<Voter, bool> isStrategic)
            {
                var (strategicVoters, honestVoters) = voters.Sample(isStrategic);

                var strategicWinners = GetRanking(
                    CandidateComparerCollection<TBallot>.Concat(
                        strategicVoters.Bind(getStrategicBallot),
                        honestVoters.Bind(getHonestBallot)))[0];
                    
                var overallSatisfaction = overallSatisfactionWith(strategicWinners);

                var strategicVoterSatisfactionWith = GetSatisfactionWith(strategicVoters);
                var strategicVoterSatisfactionWithStrategicOutcome = strategicVoterSatisfactionWith(strategicWinners);
                var strategicVoterSatisfactionWithHonestOutcome = strategicVoterSatisfactionWith(honestWinners);

                return new SatisfactionResult(overallSatisfaction, strategicVoterSatisfactionWithStrategicOutcome, strategicVoterSatisfactionWithHonestOutcome);
            }

            // Indicates if a voter has incentive to "shake up" the race
            bool PrefersRunnerUp(Voter v) => honestRanking.Count > 1 && honestRanking[0].Max(w => v.Utilities[w]) < honestRanking[1].Max(w => v.Utilities[w]);

            Func<Voter, TBallot> GetGetStrategicBallot()
            {
                var ballotsByVoter = voters.Comparers
                    .ToDictionary(
                        a => a.Item,
                        a =>
                        {
                            var potentialBallots = GetPotentialStrategicBallots(honestRanking, a.Item)
                                .Prepend(getHonestBallot(a.Item))
                                .ToList();

                            if (potentialBallots.Count == 1)
                                return potentialBallots.Single();
                            
                            var evs = potentialBallots
                                .Select(b => (Ballot: b, Ev: EV(polls, b)))
                                .ToList();

                            var maxEv = evs.Max(p => p.Ev);

                            return evs.First(pb => pb.Ev == maxEv).Ballot;

                            double EV(CountedList<CandidateComparerCollection<TBallot>> polls, TBallot ballot)
                            {
                                return polls.Sum(p => GetRanking(p.Item.Append(ballot))[0].Average(w => a.Item.Utilities[w]));
                            }
                        });

                return (Voter v) => ballotsByVoter[v];
            }
        }

        public override ElectionResults GetElectionResults(string ballots)
        {
            var parsedBallots = CandidateComparerCollection<TBallot>.Parse(ballots);
            return GetElectionResults(parsedBallots);
        }
        
        public abstract TBallot GetHonestBallot(Voter v);

        public abstract ElectionResults GetElectionResults(CandidateComparerCollection<TBallot> ballots);

        public virtual IEnumerable<TBallot> GetPotentialStrategicBallots(List<List<int>> winners, Voter v) => Enumerable.Empty<TBallot>();

        public List<List<int>> GetRanking(CandidateComparerCollection<TBallot> ballots) => GetElectionResults(ballots).Ranking;

        private static Func<Voter, TBallot> Memoize(Func<Voter, TBallot> map)
        {
            var cache = new Dictionary<Voter, TBallot>();
            return value => cache.TryGetValue(value, out var result) ? result : cache[value] = map(value);
        }
    }
}
