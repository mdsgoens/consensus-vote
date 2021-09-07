using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class BucketConsensusSimple : BucketConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<BucketBallot<Bucket>> ballots)
        {
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromiseBallots = new HashSet<BucketBallot<Bucket>>();
            var history = new List<(int[] NewApprovalCount, List<int> Winners, CountedList<(ulong Preferred, int Candidate)> Compromises)>();
            
            // Approve of one's first choices.
            foreach (var (ballot, count) in ballots.Comparers)
            {
                foreach (var c in ballot.Buckets.IndexesWhere(a => a == Bucket.Best))
                {
                    approvalCount[c] += count;
                    firstChoices[c] += count;
                }
            }
     
            var winningScore = approvalCount.Max();
            var winners = approvalCount.IndexesWhere(a => a == winningScore).ToList();
            history.Add((firstChoices, winners, new CountedList<(ulong, int)>()));

            var previousWinners = winners.ToHashSet();

            while (true)
            {
                var newApprovalCount = new int[ballots.CandidateCount];
                var compromises = new CountedList<(ulong, int)>();

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    // Can only compromise once
                    if (compromiseBallots.Contains(ballot))
                        continue;
                    
                    // Compromise is only necessary if a winner is bad.
                    if (previousWinners.All(w => ballot.Buckets[w] != Bucket.Bad))
                        continue;

                    compromiseBallots.Add(ballot);

                    var bestCoalition = GetCoalition(ballot.Buckets.IndexesWhere(a => a == Bucket.Best));

                    foreach (var candidate in ballot.Buckets.IndexesWhere(a => a == Bucket.Good))
                    {
                        approvalCount[candidate] += count;
                        newApprovalCount[candidate] += count;
                        compromises.Add((bestCoalition, candidate), count);
                    }
                }

                winningScore = approvalCount.Max();
                winners = approvalCount.IndexesWhere(a => a == winningScore).ToList();

                if (newApprovalCount.Any(c => c > 0))
                    history.Add((newApprovalCount, winners, compromises));

                var newWinners = winners.Where(w => !previousWinners.Contains(w));

                if (!newWinners.Any())
                    break;

                foreach (var w in newWinners)
                    previousWinners.Add(w);

            }

            var results = new ElectionResults(approvalCount.IndexRanking());

            if (history.Count == 1)
            {
                results.AddHeading("Approval");
                results.AddCandidateTable(approvalCount);
            }
            else
            {
                results.AddHeading("Rounds");
                results.AddTable(history.Select((h, i) => new ElectionResults.Value[] {
                        i + 1,
                        h.Winners,
                    }.Concat(h.NewApprovalCount.Select(a => (ElectionResults.Value) a))
                    .ToArray())
                    .Append(approvalCount.Select(a => (ElectionResults.Value) a).Prepend(results.Ranking[0]).Prepend("Total").ToArray() ),
                    Enumerable.Range(0, ballots.CandidateCount).Select(c => (ElectionResults.Candidate) c).Prepend<ElectionResults.Value>("Winner").ToArray());

                var roundNumber = 1;
                foreach (var round in history.Skip(1))
                {
                    roundNumber++;
                    results.AddHeading("Round " + roundNumber + " Compromises");

                    results.AddTable(round.Compromises.Select(a => new ElectionResults.Value[] {
                        (ElectionResults.Candidate) a.Item.Candidate,
                        a.Item.Preferred,
                        a.Count
                     } ),
                        "Comp.",
                        "Pref.",
                        "Count");
                }
            }

            return results;
        }
    }
}