using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus
{
    public abstract class VotingMethodBase
    {
        public abstract Dictionary<Strategy, double> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters);
        public virtual ElectionResults GetElectionResults(string ballots) => throw new NotSupportedException();

        public virtual PossibleResultCollection GetPossibleResults(CandidateComparerCollection<Voter> voters) => throw new NotSupportedException();

        public enum Strategy
        {
            Honest,
            FavoriteStrategic,
            UtilityStrategic,
        }

        protected Func<IEnumerable<int>, double> GetSatisfactionWith(CandidateComparerCollection<Voter> voters)
        {
            var satisfactions = Enumerable.Range(0, voters.CandidateCount)
                .Select(i => voters.Comparers.Sum(v => v.Utilities[i]))
                .ToList();
            var randomSatisfaction = satisfactions.Sum() / (double)voters.CandidateCount;
            var maximumSatisfaction = satisfactions.Max();

            if (randomSatisfaction == maximumSatisfaction)
                return _ => 1d;

            return (winners) => winners.Average(candidate => (satisfactions[candidate] - randomSatisfaction) / (maximumSatisfaction - randomSatisfaction));
        }

        // Coalitions are encoded as bitmasks for quick comparisons.
        public static ulong GetCoalition(IEnumerable<int> candidates) => candidates.Aggregate(0ul, (l, c) => l | GetCoalition(c));
        public static ulong GetCoalition(int candidate) => 1ul << candidate;

        public static IEnumerable<int> GetCandidates(ulong coalition)
        {
            var i = 0;
            var c = 1ul;

            while (c <= coalition)
            {
                if ((coalition & c) == c)
                    yield return i;

                i++;
                c = c << 1;
            }
        }
    }

    public abstract class VotingMethodBase<TBallot> : VotingMethodBase
        where TBallot : CandidateComparer
    {
        public override Dictionary<Strategy, double> CalculateSatisfaction(Random random, CandidateComparerCollection<Voter> voters)
        {
            var overallSatisfactionWith = GetSatisfactionWith(voters);

            var supportsPotentialBallots = GetType().GetMethod(nameof(GetPotentialStrategicBallots), new[] { typeof(List<List<int>>), typeof(Voter) }).DeclaringType != typeof(VotingMethodBase<TBallot>);

            if (!supportsPotentialBallots)
            {
                return new Dictionary<Strategy, double>
                {
                    { Strategy.Honest, overallSatisfactionWith(GetRanking(voters.Bind(GetHonestBallot))[0]) },
                };
            }

            var results = GetPossibleResults(voters);
            var (favorite, utility) = results.GetStrategicResults(random);

            return new Dictionary<Strategy, double>
            {
                { Strategy.Honest, overallSatisfactionWith(results.Honest.Winners) },
                { Strategy.FavoriteStrategic, overallSatisfactionWith(favorite.Winners) },
                { Strategy.UtilityStrategic, overallSatisfactionWith(utility.Winners) },
            };
        }

        public override ElectionResults GetElectionResults(string ballots)
        {
            var parsedBallots = CandidateComparerCollection<TBallot>.Parse(ballots);
            return GetElectionResults(parsedBallots);
        }

        public override PossibleResultCollection GetPossibleResults(CandidateComparerCollection<Voter> voters) => new PossibleResultCollection<TBallot>(voters, this);

        public abstract TBallot GetHonestBallot(Voter v);

        public abstract ElectionResults GetElectionResults(CandidateComparerCollection<TBallot> ballots);

        public virtual IEnumerable<(string Strategy, int Preference, TBallot Ballot)> GetPotentialStrategicBallots(List<List<int>> winners, Voter v) => Enumerable.Empty<(string, int, TBallot)>();

        public List<List<int>> GetRanking(CandidateComparerCollection<TBallot> ballots) => GetElectionResults(ballots).Ranking;
    }
}
