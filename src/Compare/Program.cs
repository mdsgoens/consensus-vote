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
            var elections = from candidateCount in new [] { 3, 4, 5 }
                let rankings = GetAllRankings(candidateCount).ToList()
                from voterCounts in InterestingVoterCounts()
                from permutation in GetPermutations(voterCounts.Length, rankings.Count)
                select new CandidateComparerCollection<RankedBallot>(
                    candidateCount,
                    new CountedList<RankedBallot>(
                        permutation.Select((rankingIndex, countIndex) => 
                            (new RankedBallot(candidateCount, rankings[rankingIndex]), voterCounts[countIndex])))
                );

            var rounds = new ConsensusRounds();
            var naive = new ConsensusNaive();
            var coalition = new ConsensusCoalition();
            var irv = new InstantRunoff();

            int index = 0;
            var examplesFound = 0;
            foreach (var ballots in elections)
            {
                index++;
                Console.Write("\r" + index);
                
                var roundsWinner = rounds.GetElectionResults(ballots).Winner;
                var naiveWinner = naive.GetElectionResults(ballots).Winner;
                var coalitionWinner = coalition.GetElectionResults(ballots).Winner;
                var irvWinner = irv.GetElectionResults(ballots).Winner;

                if (!(roundsWinner == naiveWinner && roundsWinner == coalitionWinner))
                {
                    Console.WriteLine();
                    Console.WriteLine(ballots.ToString());
                    Console.WriteLine("Rounds: " + ParsingUtility.EncodeCandidateIndex(roundsWinner));
                    Console.WriteLine("Naive: " + ParsingUtility.EncodeCandidateIndex(naiveWinner));
                    Console.WriteLine("Coalition: " + ParsingUtility.EncodeCandidateIndex(coalitionWinner));
                    Console.WriteLine("IRV: " + ParsingUtility.EncodeCandidateIndex(irvWinner));
                    Console.WriteLine();

                    examplesFound++;

                    if (examplesFound > 10)
                        return;
                }
            }
        }

        // "Interesting" voter counts for use in generating "interesting" sets of ballots
        // For ease of display, counts add up to 100.
        private static IEnumerable<int[]> InterestingVoterCounts()
        {
            // Two front-runners
            yield return new [] { 51, 49 };
            yield return new [] { 49, 48, 3 };
            yield return new [] { 49, 46, 3, 2 };

            // Three front-runners
            yield return new [] { 35, 33, 32 };
            yield return new [] { 33, 32, 31, 4 };
            yield return new [] { 33, 32, 31, 3, 1 };

            // Four front-runners
            yield return new [] { 27, 26, 24, 23 };
            yield return new [] { 30, 24, 23, 22 };
            yield return new [] { 25, 24, 23, 22, 6 };
            yield return new [] { 25, 24, 23, 22, 4, 2 };
        }

        private static IEnumerable<List<List<int>>> GetAllRankings(int candidateCount)
        {
            if (candidateCount == 0)
            {
                yield return new List<List<int>>();
                yield break;
            }

            var candidate = candidateCount - 1;
            foreach (var previous in GetAllRankings(candidateCount - 1))
            {
                var newList = new List<int> { candidate };
                var before = new List<List<int>>(previous.Count + 1);
                before.Add(newList);
                before.AddRange(previous);

                yield return before;

                for (int i = 0; i < previous.Count; i++)
                {
                    var tied = new List<List<int>>(previous.Count);
                    tied.AddRange(previous);
                    tied[i] = previous[i].Append(candidate).ToList();
                    yield return tied;

                    var after = new List<List<int>>(previous.Count + 1);
                    after.AddRange(previous);
                    after.Insert(i + 1, newList);
                    yield return after;
                }
            }
        }

        private static IEnumerable<int[]> GetPermutations(int chooseCount, int itemCount)
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
    }
}
