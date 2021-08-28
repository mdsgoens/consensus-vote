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
            var voterCountsList = InterestingVoterCounts().ToList();
            var rankingsByCandidateCount = new [] { 3, 4, 5 }
                .ToDictionary(c => c, c => GetAllRankings(c, allowTies: false).ToList());

            var elections = from candidateCount in rankingsByCandidateCount.Keys
                from voterCounts in voterCountsList
                from permutation in GetPermutations(voterCounts.Length, rankingsByCandidateCount[candidateCount].Count)
                select new CandidateComparerCollection<RankedBallot>(
                    candidateCount,
                    permutation.Select((rankingIndex, countIndex) => 
                        (new RankedBallot(candidateCount, rankingsByCandidateCount[candidateCount][rankingIndex]), voterCounts[countIndex]))
                       .ToCountedList()
                );

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
                    FindDifferences<ConsensusRoundsBeats, ConsensusRoundsSimple>(ballots);

                    if (examplesFound > 5)
                        return;
                }
                catch (Exception)
                {
                    Console.WriteLine();
                    Console.WriteLine(ballots.ToString());
                    throw;
                }
            }

            void FindDifferences<T1, T2>(CandidateComparerCollection<RankedBallot> ballots)
                where T1 : VotingMethodBase<RankedBallot>, new()
                where T2 : VotingMethodBase<RankedBallot>, new()
            {
                var firstResults = new T1().GetElectionResults(ballots);
                var secondResults = new T2().GetElectionResults(ballots);

                if (ConsensusVoteBase.GetCoalition(firstResults.Winners) != ConsensusVoteBase.GetCoalition(secondResults.Winners))
                {
                    if (random.Next(examplesFound * examplesFound) == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Ballots: " + ballots);
                        Console.WriteLine();
                        Console.Write(typeof(T1).Name + " ");
                        Console.WriteLine(firstResults.Details);
                        Console.WriteLine();
                        Console.Write(typeof(T2).Name + " ");
                        Console.WriteLine(secondResults.Details);
                        Console.WriteLine();
                        
                        examplesFound++;
                    }
                }
            }

            void FindStrategicBallots<T>(CandidateComparerCollection<RankedBallot> ballots)
                where T : VotingMethodBase<RankedBallot>, new()
            {
                var method = new T();
                var honestResults = method.GetElectionResults(ballots);
                var honestWinners = honestResults.Winners;

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    // Assuming the ballot cast was honest, was there another ballot we could have cast which resulted in a better outcome?
                    // Assume: The entire bloc needs to use the same strategy, we do not consider any "response" strategies.
                    var honestWinnerRank = honestWinners.Select(c => ballot.RanksByCandidate[c]).Min();

                    if (honestWinnerRank == 0)
                        continue;

                    var bogeymen = honestWinners.Where(c => ballot.RanksByCandidate[c] < 0).ToHashSet();

                    var alternateBallots = new List<(string Strategy, RankedBallot Ballot)>();

                    // "Truncation" drops support for everyone not your favorite.
                    alternateBallots.Add(("Truncation", new RankedBallot(ballot.CandidateCount, new List<List<int>> { ballot.Ranking[0] })));

                    if (honestWinnerRank > 1)
                    {
                        // A "favorite betrayal" strategy raises the ranking of those we prefer to the bogeyman in hopes of a better outcome
                        var ranking = ballot.Ranking
                                .Skip(1)
                                .ToList();
                        ranking.Insert(honestWinnerRank, ballot.Ranking[0]);

                        alternateBallots.Add(("Favorite Betrayal", new RankedBallot(ballot.CandidateCount, ranking)));
                    }

                    if (honestWinnerRank < ballot.Ranking.Count - 1)
                    {
                        // A "burial" strategy ranks the winner at the bottom in hopes of a better outcome.
                        alternateBallots.Add(("Burial", new RankedBallot(
                            ballot.CandidateCount,
                            ballot.Ranking
                                .Select(tier => tier.Except(bogeymen).ToList())
                                .Where(tier => tier.Any())
                                .Append(bogeymen.ToList()))));
                    }


                    foreach (var alternateRanking in rankingsByCandidateCount[ballots.CandidateCount])
                    {
                        var alternateBallot = new RankedBallot(ballots.CandidateCount, alternateRanking);

                        var newWinners = method.GetElectionResults(ballots.Replace(ballot, alternateBallot)).Winners;

                        var newWinnerRank = newWinners.Select(c => ballot.RanksByCandidate[c]).Min();

                        if (newWinnerRank > honestWinnerRank)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Ballots: " + ballots);
                            Console.WriteLine("Original Winner: " + (ElectionResults.Value) honestWinners);
                            Console.WriteLine("Subect: " + ballot);
                            Console.WriteLine("Strategic: " + alternateBallot);
                            Console.WriteLine("New Winner: " +  (ElectionResults.Value) newWinners);
                            Console.WriteLine();
                            
                            examplesFound++;

                        }
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
