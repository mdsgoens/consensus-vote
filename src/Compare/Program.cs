using System.Xml;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Consensus;
using Consensus.Ballots;
using Consensus.Methods;
using System;

namespace Compare
{
    class Program
    {
        static void Main(string[] args)
        {
            bool findDifferences = args.Length > 0 && args[0] == "FindDifferences";

            var voterCountsList = InterestingVoterCounts().ToList();
            var rankingsByCandidateCount = new [] { 3, 4, 5 }
                .ToDictionary(c => c, c => GetAllRankings(c, allowTies: false).ToList());

            var elections = from candidateCount in rankingsByCandidateCount.Keys
                from voterCounts in voterCountsList
                from permutation in GetUniquePermutations(voterCounts.Length, rankingsByCandidateCount[candidateCount].Count)
                select new CandidateComparerCollection<RankedBallot>(
                    candidateCount,
                    permutation.Select((rankingIndex, countIndex) => 
                        (new RankedBallot(candidateCount, rankingsByCandidateCount[candidateCount][rankingIndex]), voterCounts[countIndex]))
                       .ToCountedList()
                );

            var methods = new VotingMethodBase<RankedBallot> [] {
                new ConsensusBeats(),
                new ConsensusRoundsBeats(),
                new ConsensusRoundsSimple(),
                new ConsensusRoundsMinimalChange(),
                new ConsensusCoalition(),
                new ConsensusCondorcet(),
                new InstantRunoff(),
            };
            var effectiveStrategies = Enumerable.Range(0, methods.Length)
                .Select(_ => new CountedList<RankedBallot.Strategy>())
                .ToArray();

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
                    var results = methods.Select(m => m.GetElectionResults(ballots)).ToList();

                    if (findDifferences)
                        FindDifferences(ballots, results);

                    for (int i = 0; i < methods.Length; i++)
                    {
                        foreach (var strategy in FindEffectiveStrategies(ballots, methods[i], results[i]))
                            effectiveStrategies[i].Add(strategy);
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
                    var sb = new StringBuilder();
                    var strategies = effectiveStrategies.SelectMany(m => m.Select(a => a.Item)).Distinct().ToList();
                    ElectionResults.AppendTable(sb,
                        Enumerable.Range(0, methods.Length).Select(i =>
                            strategies.Select(s => (ElectionResults.Value) (effectiveStrategies[i].TryGetCount(s, out var count) ? (int) Math.Ceiling(count * 100d / index) : 0))
                                .Prepend(methods[i].GetType().Name)
                                .ToArray()),
                            strategies.Select(s => (ElectionResults.Value) s.ToString()).ToArray());

                    Console.Clear();
                    Console.WriteLine(sb.ToString());
                }
            }

            void FindDifferences(CandidateComparerCollection<RankedBallot> ballots, List<ElectionResults> results)
            {
                if (results.Select(r => ConsensusVoteBase.GetCoalition(r.Winners)).Distinct().Count() > 1)
                {
                    if (random.Next((examplesFound + 10) * (examplesFound + 10)) == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Ballots: " + ballots);
                        Console.WriteLine();

                        for (int i = 0; i < methods.Length; i++)
                        {
                            Console.Write(methods[i].GetType().Name + " ");
                            Console.WriteLine(results[i].Details);
                            Console.WriteLine();
                        }
                        
                        examplesFound++;
                    }
                }
            }

            IEnumerable<RankedBallot.Strategy> FindEffectiveStrategies(CandidateComparerCollection<RankedBallot> ballots, VotingMethodBase<RankedBallot> method, ElectionResults honestResults)
            {
                // Assuming the ballot cast was honest, was there another ballot we could have cast which resulted in a better outcome?
                // Assume: 
                // (a) The entire bloc needs to use the same strategy, and
                // (b) will never use a strategy that leaves them open to a *worse* outcome, and
                // (c) will only use a non-honest strategy with *some* possibility of a better outcome.
                var honestWinners = honestResults.Winners;

                var possibilities = ballots.Comparers
                    .Select((b, index) => (Ballot: b.Item, Count: b.Count, Alternates: b.Item.GetPotentialStrategicBallots(honestWinners).ToList()))
                    .ToList();

                var possiblePermutations = GetAllPermutations(possibilities.Select(p => p.Alternates.Count).ToArray())
                    .Select(permutation =>
                    {
                        var permutationWinners = method.GetElectionResults(new CandidateComparerCollection<RankedBallot>(
                            ballots.CandidateCount,
                            Enumerable.Range(0, possibilities.Count)
                                .Select(i => (possibilities[i].Alternates[permutation[i]].AlternateBallot, possibilities[i].Count))
                                .ToCountedList()))
                            .Winners;

                        return Enumerable.Range(0, possibilities.Count)
                            .Select(i => (possibilities[i].Alternates[permutation[i]].Strategy, WinnerRank: Rank(possibilities[i].Ballot, permutationWinners)))
                            .ToArray();
                    })
                    .ToList();
                
                var possibleStrategies = possibilities
                    .Select(p => p.Alternates.Select(a => a.Strategy).Where(s =>  s != RankedBallot.Strategy.Honest).ToHashSet())
                    .ToList();

                // First, eliminate all strategies which bring no benefit to their employer over honesty
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if (!possibleStrategies[i].Any())
                        continue;

                    // for each possible combination of other voters' strategies, what are the outcomes for each of our possible strategies?
                    var possibleOutcomesByStrategy = possiblePermutations
                        .GroupBy(s => new PermutationGroup(i, s.Select(a => a.Strategy).ToArray()))
                        .Select(gp => gp.Select(s => s[i]).ToDictionary(a => a.Strategy, a => a.WinnerRank))
                        .ToList();

                    // Honesty dominates a strategy which never produces a better outcome.
                    foreach (var s in possibleStrategies[i].Where(s => !possibleOutcomesByStrategy.Any(ps => ps[s] > ps[RankedBallot.Strategy.Honest])).ToList())
                        possibleStrategies[i].Remove(s);
                }

                // Don't even consider them in the next round
                possiblePermutations.RemoveAll(pp => 
                    pp.IndexesWhere((r, i) => r.Strategy != RankedBallot.Strategy.Honest && !possibleStrategies[i].Contains(r.Strategy)).Any());

                var effectiveStrategies = new HashSet<RankedBallot.Strategy>();            
                for (int i = 0; i < possibilities.Count; i++)
                {
                    if (!possibleStrategies[i].Any(s => !effectiveStrategies.Contains(s)))
                        continue;

                    // for each possible combination of other voters' strategies, what are the outcomes for each of our possible strategies?
                    var possibleOutcomesByStrategy = possiblePermutations
                        .GroupBy(s => new PermutationGroup(i, s.Select(a => a.Strategy).ToArray()))
                        .Select(gp => gp.Select(s => s[i]).ToDictionary(a => a.Strategy, a => a.WinnerRank))
                        .ToList();

                    // In the remaining set, strategies *still* need to produce **some** better outcome
                    // But now they also need to never produce a *worse* outcome, too.
                    foreach (var s in possibleStrategies[i])
                    {
                        if (!possibleOutcomesByStrategy.Any(ps => ps[s] < ps[RankedBallot.Strategy.Honest])
                            && possibleOutcomesByStrategy.Any(ps => ps[s] > ps[RankedBallot.Strategy.Honest]))
                        {
                            effectiveStrategies.Add(s);
                        }
                    }
                }
               
                if (effectiveStrategies.Any())
                    return effectiveStrategies;
                else
                    return new [] { RankedBallot.Strategy.Honest };
            }

            static int Rank(RankedBallot ballot, List<int> winners) => winners.Select(c => ballot.RanksByCandidate[c]).Min();
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
