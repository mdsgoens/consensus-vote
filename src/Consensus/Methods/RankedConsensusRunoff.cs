using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    [Obsolete("Candidate elimination makes it more suceptible to truncation and dark-horse")]
    public sealed class RankedConsensusRunoff : RankedConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
        {
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var approvalByBallot = new Dictionary<RankedBallot, ulong>();
            var history = new List<(int[] NewApprovalCount, List<int> Winners, CountedList<(ulong Preferred, int Candidate)> Compromises)>();
            var eliminated = new HashSet<int>();
            
            foreach (var (ballot, count) in ballots.Comparers)
            {
                var firstChoiceCandidates = ballot.RanksByCandidate.IndexesWhere(a => a == 0).ToList();
                approvalByBallot[ballot] = GetCoalition(firstChoiceCandidates);

                // Approve of one's first choices.
                foreach (var c in firstChoiceCandidates)
                {
                    approvalCount[c] += count;
                    firstChoices[c] += count;
                }
            }
     
            var winningScore = approvalCount.Max();
            var winners = approvalCount.IndexesWhere(a => a == winningScore).ToList();
            history.Add((firstChoices, winners, new CountedList<(ulong, int)>()));

            var previousWinners = winners.ToHashSet();
            var previousWinningScore = winningScore;

            while (true)
            {
                var newApprovalCount = new int[ballots.CandidateCount];
                var compromises = new CountedList<(ulong, int)>();

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    var lowestWinnerRank = previousWinners
                        .Select(w => ballot.RanksByCandidate[w])
                        .Min();
                    
                    // If all winners are ranked one or two, no compromise is necessary.
                    if (lowestWinnerRank == 0 || lowestWinnerRank == -1)
                        continue;

                    var approvedCoalition = approvalByBallot[ballot];

                    // Approve of the each candidate `c` that one likes better than any of the previous winners
                    // which one has not already approved of
                    var newApprovals = ballot.RanksByCandidate
                        .IndexesWhere((rank, candidate) => !eliminated.Contains(candidate) && rank > lowestWinnerRank && (approvedCoalition & GetCoalition(candidate)) == 0ul)
                        .ToList();

                    foreach (var candidate in newApprovals)
                    {
                        approvalCount[candidate] += count;
                        newApprovalCount[candidate] += count;
                        compromises.Add((approvedCoalition, candidate), count);
                    }

                    if (newApprovals.Any())
                        approvalByBallot[ballot] = approvedCoalition | GetCoalition(newApprovals);
                }

                winningScore = approvalCount.Max();
                winners = approvalCount.IndexesWhere(a => a == winningScore).ToList();

                if (newApprovalCount.Any(c => c > 0))
                    history.Add((newApprovalCount, winners, compromises));

                var newWinners = winners.Where(w => !previousWinners.Contains(w));

                if (!newWinners.Any())
                    break;

                // Add the new winners to the bogeyman set
                foreach (var w in newWinners)
                    previousWinners.Add(w);

                // Eliminate each candidate who could not surpass the previous winning score
                foreach (var c in approvalCount.IndexesWhere(a => a < previousWinningScore))
                    eliminated.Add(c);

                previousWinningScore = winningScore;
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