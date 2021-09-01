using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus
{
    public sealed class StrategyUtility
    {
        public static List<TStrategy>[] GetPlausibleStrategies<TKey, TStrategy>(
            IEnumerable<TKey> source,   
            Func<TKey, IEnumerable<TStrategy>> getPossibleStrategies,
            Func<(TKey Key, TStrategy Strategy)[], int[]> getPreferences)
        {
            const int honest = 0;

            var possibilities = source
                .Select(key => (Key: key, Strategies: getPossibleStrategies(key).ToList()))
                .ToList();

            var possibleStrategies = possibilities   
                .Select(p => Enumerable.Range(0, p.Strategies.Count).ToHashSet())
                .ToArray();

            var possibleOutcomesByStrategy = possibilities.Count.LengthArray(_ => new Dictionary<PermutationGroup, int[]>());

            foreach (var permuation in GetAllPermutations(possibilities.Count.LengthArray(i => possibilities[i].Strategies.Count)))
            {
                var preferences = getPreferences(possibilities.Count.LengthArray(i => (
                    possibilities[i].Key,
                    possibilities[i].Strategies[permuation[i]]
                )));

                for (int i = 0; i < possibilities.Count; i++)
                {
                    var key = new PermutationGroup(i, permuation);

                    if (!possibleOutcomesByStrategy[i].TryGetValue(key, out var outcomes))
                        possibleOutcomesByStrategy[i][key] = outcomes = new int[possibilities[i].Strategies.Count];

                    outcomes[permuation[i]] = preferences[i];
                }
            }

            bool TrimPossibleStrategies(Func<IEnumerable<int[]>, IEnumerable<int>, IEnumerable<int>> getDominatedStrategies)
            {
                bool removedAny = false;
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if (possibleStrategies[i].Count == 1)
                        continue;

                    foreach (var s in getDominatedStrategies(possibleOutcomesByStrategy[i].Values, possibleStrategies[i]).ToList())
                    {
                        removedAny = true;
                        possibleStrategies[i].Remove(s);
                    }
                }

                if (removedAny)
                {
                    for (int i = 0; i < possibilities.Count; i++)
                    {
                        foreach (var key in possibleOutcomesByStrategy[i].Keys.Where(pg => !pg.IsStillValid(possibleStrategies)).ToList())
                            possibleOutcomesByStrategy[i].Remove(key);
                    }
                }

                return removedAny;
            }
            
            // First, eliminate all strategies which bring no benefit to their employer over honesty
            while (TrimPossibleStrategies((outcomes, strategies) => strategies.Where(strategy => 
                strategy != honest && !outcomes.Any(outcome => outcome[strategy] > outcome[honest]))));


            // Second, they also need to never produce a *worse* outcome, either (unless they have some chance of electing the favorite when honesty has none).
            TrimPossibleStrategies((outcomes, strategies) => strategies.Where(strategy =>
                strategy != honest
                && outcomes.Any(outcome => outcome[strategy] < outcome[honest])
                && !(
                    outcomes.All(outcome => outcome[honest] < 0)
                    && outcomes.Any(outcome => outcome[strategy] == 0))));


            // Lastly, ignore any strategies which are strictly dominated by another.
            TrimPossibleStrategies((outcomes, strategies) =>
                from a in strategies
                from b in strategies
                where a != b
                    && outcomes.All(outcome => outcome[b] >= outcome[a])
                    && outcomes.Any(outcome => outcome[b] > outcome[a])
                select a);

            if (possibleStrategies.Any(ps => ps.Count == 0))
                throw new InvalidOperationException("There's a bug!");

            return possibilities.Count.LengthArray(i => possibleStrategies[i].Select(s => possibilities[i].Strategies[s]).ToList());
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
    }
}