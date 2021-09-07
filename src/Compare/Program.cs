using System.Xml;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Consensus;
using Consensus.Ballots;
using Consensus.Methods;
using System;
using System.Reflection;

namespace Compare
{
    class Program
    {
        static void Main(string[] args)
        {
            bool findDifferences = args.Contains("FindDifferences");
            bool findStrategies = args.Contains("FindStrategies");

            var voterCountsList = InterestingVoterCounts().ToList();
            var votersByCandidateCount = new [] { 4, 5 }
                .ToDictionary(c => c, c => GetAllVoters(c).ToList().OrderedAtRandom().ToList());

            var elections = from candidateCount in votersByCandidateCount.Keys
                from voterCounts in voterCountsList
                from permutation in GetUniquePermutations(voterCounts.Length, votersByCandidateCount[candidateCount].Count)
                select new CandidateComparerCollection<Voter>(
                    candidateCount,
                    permutation
                       .Select((rankingIndex, countIndex) => (votersByCandidateCount[candidateCount][rankingIndex], voterCounts[countIndex]))
                       .ToCountedList()
                );

            var votingMethods = Assembly.GetAssembly(typeof(VotingMethodBase))
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(VotingMethodBase)) && !t.IsAbstract && !t.CustomAttributes.Any(a => a.AttributeType == typeof(ObsoleteAttribute)))
                .Select(t => Activator.CreateInstance(t) as VotingMethodBase)
                .ToList();

            if (args.Length > (findDifferences ? 1 : 0) + (findStrategies ? 1 : 0))
            {
                votingMethods = votingMethods.Where(vm => args.Contains(vm.GetType().Name)).ToList();

                if (!votingMethods.Any())
                    throw new InvalidOperationException("Args must contain valid voting systems.");
            }

            var effectiveStrategiesFavorite = votingMethods.Count.LengthArray(_ => new CountedList<string>());
            var effectiveStrategiesUtility = votingMethods.Count.LengthArray(_ => new CountedList<string>());

            int index = 0;
            var examplesFound = 0;
            var random = new Random();

            Console.WriteLine();
            foreach (var ballots in elections)
            {
                index++;
                Console.Write("\r" + index);
                
                try
                {
                    var possibleOutcomes = votingMethods.Select(m => m.GetPossibleResults(ballots)).ToList();

                    if (findDifferences)
                        FindDifferences(ballots, possibleOutcomes.Select(m => m.Honest).ToList());

                    if (findStrategies)
                    {
                        for (int i = 0; i < votingMethods.Count; i++)
                        {        
                            var (favorite, utility) = possibleOutcomes[i].GetPlausibleStrategies();
                
                            if (!favorite.Any())
                                effectiveStrategiesFavorite[i].Add("Honesty");

                            foreach (var strategy in favorite)
                                effectiveStrategiesFavorite[i].Add(strategy);
                                
                            if (!utility.Any())
                                effectiveStrategiesUtility[i].Add("Honesty");

                            foreach (var strategy in utility)
                                effectiveStrategiesUtility[i].Add(strategy);

                            if (votingMethods[i].GetType().Name == "BucketConsensusSimple" && utility.Contains("Abstain"))
                                Console.WriteLine(ballots);
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine();
                    Console.WriteLine(ballots.ToString());
                    throw;
                }

                if (index % 1000 == 0)
                {
                    GC.Collect();

                    var sb = new StringBuilder();
                    sb.AppendLine("Favorite");
                    AppendTable(effectiveStrategiesFavorite);
                    
                    sb.AppendLine();
                    sb.AppendLine("Utility");
                    AppendTable(effectiveStrategiesUtility);

                    Console.Clear();
                    Console.WriteLine(sb.ToString());

                    void AppendTable(CountedList<string>[] effectiveStrategies)
                    {
                        var strategies = effectiveStrategies.SelectMany(m => m.Select(a => a.Item)).Distinct().ToList();
                        ElectionResults.AppendTable(sb,
                            votingMethods.SelectToArray((m, i) =>
                                strategies
                                    .Select(s => (ElectionResults.Value) (effectiveStrategies[i].TryGetCount(s, out var count) ? (count * 100d / index).ToString("N2") : ""))
                                    .Prepend(m.GetType().Name)
                                    .ToArray()),
                                strategies.SelectToArray(s => (ElectionResults.Value) s.ToString()));
                    }
                }
            }

            void FindDifferences(CandidateComparerCollection<Voter> voters, List<ElectionResults> results)
            {
                if (results.Select(r => RankedConsensusBase.GetCoalition(r.Winners)).Distinct().Count() > 1)
                {
                    if (random.Next((examplesFound + 10) * (examplesFound + 10)) == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Voters: " + voters);
                        Console.WriteLine();

                        for (int i = 0; i < votingMethods.Count; i++)
                        {
                            Console.Write(votingMethods[i].GetType().Name + " ");
                            Console.WriteLine(results[i].Details);
                            Console.WriteLine();
                        }
                        
                        examplesFound++;
                    }
                }
            }
        }

        // "Interesting" voter counts for use in generating "interesting" sets of ballots
        // For ease of display, counts add up to 100.
        private static IEnumerable<int[]> InterestingVoterCounts()
        {
            // Two front-runners
            yield return new [] { 49, 48, 3 };

            // front-runner and two runners-up
            yield return new [] { 49, 26, 25 };
            yield return new [] { 49, 25, 23, 3 };
            yield return new [] { 46, 26, 25, 3 };

            // Three front-runners
            yield return new [] { 35, 33, 32 };
            yield return new [] { 33, 32, 31, 4 };

            // four front-runners
            yield return new [] { 28, 25, 24, 23 };
            yield return new [] { 25, 24, 23, 22, 6 };
        }
  
        private static IEnumerable<Voter> GetAllVoters(int candidateCount)
        {
            foreach (var permuation in GetAllPermutations(candidateCount.LengthArray(_ => candidateCount)).Where(p => p.Distinct().Count() > 2))
                yield return new Voter(permuation);
        }

        private static IEnumerable<List<List<int>>> GetAllRankings(int candidateCount, bool allowTies)
        {
            if (candidateCount == 0)
            {
                yield return new List<List<int>>();
                yield break;
            }

            var candidate = candidateCount - 1;
            foreach (var previous in GetAllRankings(candidateCount - 1, allowTies))
            {
                var newList = new List<int> { candidate };
                var before = new List<List<int>>(previous.Count + 1);
                before.Add(newList);
                before.AddRange(previous);

                yield return before;

                for (int i = 0; i < previous.Count; i++)
                {
                    if (allowTies)
                    {
                        var tied = new List<List<int>>(previous.Count);
                        tied.AddRange(previous);
                        tied[i] = previous[i].Append(candidate).ToList();
                        yield return tied;
                    }

                    var after = new List<List<int>>(previous.Count + 1);
                    after.AddRange(previous);
                    after.Insert(i + 1, newList);
                    yield return after;
                }
            }
        }

        private static IEnumerable<int[]> GetUniquePermutations(int chooseCount, int itemCount)
        {
            var indices = new int[chooseCount];

            while (true)
            {
                var didIncrement = false;
                for (int i = chooseCount - 2; !didIncrement && i >= 0; i--)
                {
                    for (int j = i + 1; !didIncrement && j < chooseCount; j++)
                    {
                        if (indices[i] == indices[j])
                        {
                            if (!TryIncrement(i))
                                yield break;
                            
                            didIncrement = true;
                        }
                    }
                }

                if (!didIncrement)
                {
                    yield return indices.Clone() as int[];

                    if (!TryIncrement(0))
                        yield break;
                }
            }

            bool TryIncrement(int largestDuplicateIndex)
            {
                for (int i = largestDuplicateIndex; ; i++)
                {
                    if (i == chooseCount)
                        return false;

                    var next = indices[i] + 1;

                    if (next < itemCount)
                    {
                        indices[i] = next;
                        return true;
                    }

                    indices[i] = 0;
                }
            }
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
            public PermutationGroup(int index, RankedBallot.Strategy[] permuation)
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

            public bool IsStillValid(HashSet<RankedBallot.Strategy>[] validStrategies)
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
            private readonly RankedBallot.Strategy[] m_permuation;
        }
    }
}
