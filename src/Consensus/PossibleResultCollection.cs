using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus
{
    public abstract class PossibleResultCollection
    {
        public ElectionResults Honest { get; init; }
        public abstract (ElectionResults Favorite, ElectionResults Utility) GetStrategicResults(Random random);
        public abstract (List<string> Favorite, List<string> Utility) GetPlausibleStrategies();

        public abstract string GetHonestBallot();
    }

    public sealed class PossibleResultCollection<TBallot> : PossibleResultCollection
        where TBallot : CandidateComparer
    {
        public PossibleResultCollection(CandidateComparerCollection<Voter> voters, VotingMethodBase<TBallot> method)
        {
            m_voters = voters;
            m_method = method;
            m_voterCount = voters.Comparers.Count();

            Honest = method.GetElectionResults(voters.Bind(method.GetHonestBallot));

            var possibleStrategies = voters.Comparers
                .SelectToArray(a => method.GetPotentialStrategicBallots(Honest.Ranking, a.Item)
                    .Prepend((null, 0, method.GetHonestBallot(a.Item)))
                    .ToList());

            m_possibleBallots = possibleStrategies
                .SelectToArray(v => v.SelectToArray(s => s.Ballot));

            m_strategyNames = possibleStrategies
                .SelectToArray(v => v.SelectToArray(s => s.Strategy));
                
            m_strategyTiebreakers = possibleStrategies
                .SelectToArray(v => v.SelectToArray(s => s.Preference));
        }

        public override string GetHonestBallot() => m_voters.Bind(m_method.GetHonestBallot).ToString();

        private (double[] Favorite, double[] Utility) GetUtility(int[] permutation)
        {
            var results = GetResults(permutation);

            return (m_voters.Comparers
                .SelectToArray(a =>
                {
                    var maxUtility = a.Item.Utilities.Max();
                    var maxUtilityWinners = results.Winners.Count(w => a.Item.Utilities[w] == maxUtility);

                    return maxUtilityWinners / (double)results.Winners.Count;
                }),
                m_voters.Comparers.SelectToArray(a => results.Winners.Average(w => a.Item.Utilities[w])));
        }

        private ElectionResults GetResults(int[] permutation)
        {
            var ballots = m_voters.Comparers
                .Select((a, i) => (m_possibleBallots[i][permutation[i]], a.Count))
                .ToCountedList();

            return m_method.GetElectionResults(new CandidateComparerCollection<TBallot>(m_voters.CandidateCount, ballots));
        }

        public override (ElectionResults Favorite, ElectionResults Utility) GetStrategicResults(Random random)
        {
            var evenStrategyChance = m_possibleBallots
                .SelectToArray(p => p.Length.LengthArray(_ => 1d / p.Length));

            var favoriteEvs = m_voterCount.LengthArray(i => m_possibleBallots[i].Length.LengthArray(_ => new List<double>()));
            var utilityEvs = m_voterCount.LengthArray(i => m_possibleBallots[i].Length.LengthArray(_ => new List<double>()));

            foreach (var p in GetRandomPermuations(evenStrategyChance, random).Take(99).Append(new int[m_voterCount]))
            {
                var utilities = GetUtility(p);

                for (int i = 0; i < m_voterCount; i++)
                {

                    favoriteEvs[i][p[i]].Add(utilities.Favorite[i]);
                    utilityEvs[i][p[i]].Add(utilities.Utility[i]);
                }
            }

            return (
                GetResults(GetRandomPermuations(GetStrategyChange(favoriteEvs), random).First()),
                GetResults(GetRandomPermuations(GetStrategyChange(utilityEvs), random).First())
                );

            double[][] GetStrategyChange(List<double>[][] evs)
            {
                return m_voterCount.LengthArray(i =>
                {
                    var min = evs[i].SelectMany(e => e).Min(s => s);

                    if (!evs[i].SelectMany(e => e).Any(s => s > min))
                        return m_possibleBallots[i].Length.LengthArray(j => j == 0 ? 1d : 0d);

                    var total = evs[i].SelectMany(e => e).Sum(s => s - min);

                    return m_possibleBallots[i].Length.LengthArray(j => evs[i][j].Sum(s => s - min) / total);
                });
            }
        }

        // Assume: 
        // (a) The entire bloc needs to use the same strategy, and
        // (b) will never use a strategy that leaves them open to a *worse* outcome, and
        // (c) will only use a non-honest strategy with *some* possibility of a better outcome.
        public override (List<string> Favorite, List<string> Utility) GetPlausibleStrategies()
        {
            var possibleOutcomesByFavoriteStrategy = m_possibleBallots.Length.LengthArray(_ => new Dictionary<PermutationGroup, double[]>());
            var possibleOutcomesByUtilityStrategy = m_possibleBallots.Length.LengthArray(_ => new Dictionary<PermutationGroup, double[]>());

            foreach (var permuation in GetAllPermutations(m_possibleBallots.Length.LengthArray(i => m_possibleBallots[i].Length)))
            {
                var utilities = GetUtility(permuation);

                for (int i = 0; i < m_possibleBallots.Length; i++)
                {
                    var key = new PermutationGroup(i, permuation);

                    if (!possibleOutcomesByFavoriteStrategy[i].TryGetValue(key, out var favoriteOutcomes))
                        possibleOutcomesByFavoriteStrategy[i][key] = favoriteOutcomes = new double[m_possibleBallots[i].Length];

                    if (!possibleOutcomesByUtilityStrategy[i].TryGetValue(key, out var utilityOutcomes))
                        possibleOutcomesByUtilityStrategy[i][key] = utilityOutcomes = new double[m_possibleBallots[i].Length];

                    favoriteOutcomes[permuation[i]] = utilities.Favorite[i];
                    utilityOutcomes[permuation[i]] = utilities.Utility[i];
                }
            }

            return (GetPlausibleStrategies(possibleOutcomesByFavoriteStrategy), GetPlausibleStrategies(possibleOutcomesByUtilityStrategy));
        }

        private List<string> GetPlausibleStrategies(Dictionary<PermutationGroup, double[]>[] possibleOutcomesByStrategy)
        {
            var plausibleStrategies = m_possibleBallots
                .SelectToArray(p => Enumerable.Range(0, p.Length).ToHashSet());

            bool TrimPossibleStrategies(Func<IEnumerable<double[]>, int[], IEnumerable<int>, IEnumerable<int>> getDominatedStrategies)
            {
                bool removedAny = false;
                for (int i = 0; i < m_possibleBallots.Length; i++)
                {
                    if (m_possibleBallots[i].Length == 1)
                        continue;

                    foreach (var s in getDominatedStrategies(possibleOutcomesByStrategy[i].Values, m_strategyTiebreakers[i], plausibleStrategies[i]).ToList())
                    {
                        removedAny = true;
                        plausibleStrategies[i].Remove(s);
                    }
                }

                if (removedAny)
                {
                    if (plausibleStrategies.Any(ps => ps.Count == 0))
                        throw new InvalidOperationException("There's a bug!");

                    for (int i = 0; i < m_possibleBallots.Length; i++)
                    {
                        foreach (var key in possibleOutcomesByStrategy[i].Keys.Where(pg => !pg.IsStillValid(plausibleStrategies)).ToList())
                            possibleOutcomesByStrategy[i].Remove(key);
                    }
                }

                return removedAny;
            }

            // Eliminate all strategies which bring no benefit to their employer over a more-preferred strategy
            // Ignore any strategies which are strictly dominated by another.
            while (TrimPossibleStrategies((outcomes, tiebreakers, strategies) =>
                from a in strategies
                from b in strategies
                where a != b
                    && outcomes.All(outcome => outcome[b] >= outcome[a])
                    && (tiebreakers[b] < tiebreakers[a]
                        || outcomes.Any(outcome => outcome[b] != outcome[a]))
                select a));

            // Finally, return all strategies which are part of Nash equilibiria
            // (where no voter can unilaterally improve the outcome)
            var plausibleStrategyNames = new HashSet<string>();
            var plausibleStrategyLists = plausibleStrategies.SelectToArray(a => a.ToList());
            var votersWithMutipleStrategies = Enumerable.Range(0, m_voterCount).Where(i => plausibleStrategies[i].Count > 1).ToList();

            foreach (var strategyPermutation in GetAllPermutations(m_voterCount.LengthArray(i => plausibleStrategies[i].Count)))
            {
                var ballotPermutation = m_voterCount.LengthArray(i => plausibleStrategyLists[i][strategyPermutation[i]]);

                var isNashEquilibria = votersWithMutipleStrategies.All(i =>
                    {
                        var key = new PermutationGroup(i, ballotPermutation);

                        var outcomes = possibleOutcomesByStrategy[i][key];
                        var permutationOutcome = outcomes[ballotPermutation[i]];

                        return !plausibleStrategyLists[i].Any(o => outcomes[o] > permutationOutcome);
                    });

                if (isNashEquilibria)
                {
                    for (int i = 0; i < m_voterCount; i++)
                    {
                        if (ballotPermutation[i] > 0)
                            plausibleStrategyNames.Add(m_strategyNames[i][ballotPermutation[i]]);
                    }
                }
            }

            return plausibleStrategyNames.ToList();
        }

        private static IEnumerable<int[]> GetAllPermutations(int[] possibilities)
        {
            var permutationCount = possibilities.Aggregate(1, (a, b) => a * b);

            var indices = new int[possibilities.Length];
            for (int permutationIndex = 0; permutationIndex < permutationCount; permutationIndex++)
            {
                var permuation = new int[possibilities.Length];
                var remainder = permutationIndex;
                for (int i = 0; i < possibilities.Length; i++)
                {
                    permuation[i] = remainder % possibilities[i];
                    remainder = remainder / possibilities[i];
                }

                yield return permuation;
            }
        }

        private static IEnumerable<int[]> GetRandomPermuations(double[][] weights, Random random)
        {
            while (true)
            {
                yield return weights.Length.LengthArray(i =>
                {
                    var remainder = random.NextDouble();
                    for (int j = 0; ; j++)
                    {
                        remainder -= weights[i][j];
                        if (remainder < 0)
                            return j;
                    }
                });
            }
        }

        // Equality based on all elements in the array *except* the one at the current index
        private sealed class PermutationGroup : IEquatable<PermutationGroup>
        {
            public PermutationGroup(int index, int[] permuation)
            {
                m_index = index;
                m_permuation = permuation;
                m_hashCode = index * 37;

                for (int i = 0; i < m_permuation.Length; i++)
                {
                    m_hashCode *= 31;

                    if (i != m_index)
                        m_hashCode ^= permuation[i].GetHashCode();
                }
            }

            public bool IsStillValid(HashSet<int>[] validStrategies)
            {
                for (int i = 0; i < m_permuation.Length; i++)
                {
                    if (i != m_index && !validStrategies[i].Contains(m_permuation[i]))
                        return false;
                }

                return true;
            }

            public override int GetHashCode() => m_hashCode;

            public bool Equals(PermutationGroup other)
            {
                if (m_hashCode != other.m_hashCode)
                    return false;

                if (m_index != other.m_index)
                    return false;

                for (int i = 0; i < m_permuation.Length; i++)
                {
                    if (i != m_index && m_permuation[i] != other.m_permuation[i])
                        return false;
                }

                return true;
            }

            private readonly int m_hashCode;
            private readonly int m_index;
            private readonly int[] m_permuation;
        }

        private readonly CandidateComparerCollection<Voter> m_voters;
        private readonly int m_voterCount;
        private readonly VotingMethodBase<TBallot> m_method;

        private readonly TBallot[][] m_possibleBallots;
        private readonly string[][] m_strategyNames;
        private readonly int[][] m_strategyTiebreakers;
    }
}
