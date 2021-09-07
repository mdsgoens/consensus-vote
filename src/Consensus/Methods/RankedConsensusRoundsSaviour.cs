using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class RankedConsensusRoundsSaviour : RankedConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
        {
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var approvalByBallot = new Dictionary<RankedBallot, ulong>();
            var history = new List<(int[] NewApprovalCount, List<int> Winners, CountedList<(ulong Preferred, int Candidate)> Compromises)>();
            
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
            var previousWinners = approvalCount.IndexesWhere(a => a == winningScore).ToList();
            history.Add((firstChoices, previousWinners, new CountedList<(ulong, int)>()));

            while (true)
            {
                // Test each candidate's "saviour" `s` potential versus each of the previous winners' bogeyman `b`.
                // Each voter who prefers the saviour `s` over any of the bogeymen `b` will approve of the saviour (and each other candidate one prefers to the saviour).
                // If no saviour changes the winner, each ballot approves of every candidate they prefer to the current winner and the election ends.
                // If there exists a saviour who changes the winner, choose the saviour(s) which require the **fewest** voters to change their vote.
                // Lock in the approvals for that saviour, compute a new winner, and repeat.
                var previousWinnerCoalition = GetCoalition(previousWinners);
                var potentialSaviours = Enumerable.Range(0, ballots.CandidateCount).Select(s =>
                {
                    var newApprovalCount = new int[ballots.CandidateCount];
                    var votersRequired = 0;
                    var approvalsRequired = new List<RankedBallot>();

                    foreach (var (ballot, count) in ballots.Comparers)
                    {
                        var saviourRank = ballot.RanksByCandidate[s];

                        var lowestBogeymanRank = previousWinners
                            .Select(b => ballot.RanksByCandidate[b])
                            .Min();
                        
                        if (lowestBogeymanRank < saviourRank && (approvalByBallot[ballot] & GetCoalition(s)) == 0ul)
                        {
                            newApprovalCount[s] += count;
                            approvalsRequired.Add(ballot);
                            votersRequired += count;
                        }
                    }

                    var potentialApprovalCount = ballots.CandidateCount.LengthArray(i => approvalCount[i] + newApprovalCount[i]);
                    var potentialWinningScore = potentialApprovalCount.Max();
                    var potentialWinnerCoalition = GetCoalition(potentialApprovalCount.IndexesWhere(a => a == potentialWinningScore).ToList());

                    return (Saviour: s, Ballots: approvalsRequired, votersRequired, potentialWinnerCoalition);
                })
                    .Where(a => a.potentialWinnerCoalition != previousWinnerCoalition)
                    .ToList();

                var newApprovalCount = new int[ballots.CandidateCount];
                var compromises = new CountedList<(ulong, int)>();

                if (!potentialSaviours.Any())
                {
                    // No-one can beat the current winner, so just support as much as possible.
                    foreach (var (ballot, count) in ballots.Comparers)
                    {
                        var lowestWinnerRank = previousWinners
                            .Select(b => ballot.RanksByCandidate[b])
                            .Min();
                        
                        // No compromise necessary if we rank the winner first or second.
                        if (lowestWinnerRank >= -1)
                            continue;

                        var approvedCoalition = approvalByBallot[ballot];

                        // Approve of each candidate `c` that one likes better than the worst winner
                        // which one has not already approved of
                        var newApprovals = ballot.RanksByCandidate
                            .IndexesWhere((rank, candidate) => (rank > lowestWinnerRank) && (approvedCoalition & GetCoalition(candidate)) == 0ul)
                            .ToList();

                        foreach (var candidate in newApprovals)
                        {
                            approvalCount[candidate] += count;
                            newApprovalCount[candidate] += count;
                            compromises.Add((approvedCoalition, candidate), count);
                        }
                    }
                }
                else
                {
                    // Choosing the maximum `votersRequired` makes this method less satisfactory and more suceptible to tactical voting.
                    // Approving of all candidates we like better than the saviour *at this step* makes us more suceptible to tactical voting without affecting satisfaction.
                    var minimalVotersRequired = potentialSaviours.Select(a => a.votersRequired).Min();

                    foreach (var (ballot, newApprovals) in potentialSaviours
                        .Where(a => a.votersRequired == minimalVotersRequired)
                        .SelectMany(a => a.Ballots.Select(b => (Ballot: b, Saviour: a.Saviour)))
                        .GroupBy(a => a.Ballot, a => a.Saviour)
                        .Select(gp => (gp.Key, gp.ToList())))
                    {
                        ballots.Comparers.TryGetCount(ballot, out var count);

                        var preferredCoalition = approvalByBallot[ballot];

                        foreach (var candidate in newApprovals)
                        {
                            approvalCount[candidate] += count;
                            newApprovalCount[candidate] += count;
                            compromises.Add((preferredCoalition, candidate), count);
                        }

                        approvalByBallot[ballot] = preferredCoalition | GetCoalition(newApprovals);
                    }
                }

                winningScore = approvalCount.Max();
                previousWinners = approvalCount.IndexesWhere(a => a == winningScore).ToList();
                
                if (newApprovalCount.Any(c => c > 0))
                    history.Add((newApprovalCount, previousWinners, compromises));

                if (!potentialSaviours.Any())
                    break;
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