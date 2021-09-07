using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    
    [Obsolete("Similar to, but not as simple as, ConsensusRoundsSimple")]
    public sealed class RankedConsensusRoundsBeats : RankedConsensusBase
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
            var winners = approvalCount.IndexesWhere(a => a == winningScore).ToList();
            history.Add((firstChoices, winners, new CountedList<(ulong, int)>()));

            var beatMatrix = ballots.GetBeatMatrix();
            var previousWinners = winners.ToDictionary(
                b => b,
                b => Enumerable.Range(0, ballots.CandidateCount)
                    .Where(s => beatMatrix.Beats(s, b))
                    .ToList());

            while (true)
            {
                var newApprovalCount = new int[ballots.CandidateCount];
                var compromises = new CountedList<(ulong, int)>();

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    var approveUntilRank = 0;
                    var approvalCoalition = 0ul;

                    // If one approves of all candidates which beat one of the previous winners, do so (and approve of candidates one likes better than the worst of those)
                    // (if we don't, neither will the people who prefer the other saviours -- and we won't be able to beat the bogeyman)
                    // otherwise, approve of all candidates one likes better than said winner.
                    foreach (var (bogeyman, saviours) in previousWinners)
                    {
                        var bogeymanRank = ballot.RanksByCandidate[bogeyman];
                        var saviourRanks = saviours.Select(s => ballot.RanksByCandidate[s]).ToList();

                        if (saviours.Any() && saviourRanks.All(sr => sr > bogeymanRank))
                        {
                            approvalCoalition |= GetCoalition(saviours);
                            approveUntilRank = saviourRanks.Append(approveUntilRank).Min();
                        }
                        else
                        {
                            approveUntilRank = Math.Min(approveUntilRank, bogeymanRank);
                        }
                    }

                    var approvedCoalition = approvalByBallot[ballot];

                    var newApprovals = ballot.RanksByCandidate
                        .IndexesWhere((rank, candidate) =>
                            (rank > approveUntilRank || (approvalCoalition & GetCoalition(candidate)) > 0) && (approvedCoalition & GetCoalition(candidate)) == 0ul)
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

                var newBogeymen = winners.Where(w => !previousWinners.ContainsKey(w));

                if (!newBogeymen.Any())
                    break;

                foreach (var b in newBogeymen)
                {
                    previousWinners[b] = Enumerable.Range(0, ballots.CandidateCount)
                        .Where(s => beatMatrix.Beats(s, b))
                        .ToList();
                }
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
 
                results.AddHeading("Winners");
                results.AddTable(
                    previousWinners.Select(kvp =>  new ElectionResults.Value[] {
                        (ElectionResults.Candidate) kvp.Key,
                        kvp.Value
                    }),
                    "Beaten By");

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