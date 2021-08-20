using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusRounds : ConsensusVoteBase
    {
        public override (List<List<int>> Ranking, int[] ApprovalCount, int[] FirstChoices, IEnumerable<(Compromise Compromise, int Count)> Compromises) GetDetailedTally(CandidateComparerCollection<RankedBallot> ballots)
        {
            var previousWinners = new HashSet<int>();
            int[] firstChoices = null;

            while (true)
            {
                var approvalCount = new int[ballots.CandidateCount];
                var compromises = new CountedList<Compromise>();

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    var firstChoiceCandidates = ballot.RanksByCandidate.IndexesWhere(a => a == 0).ToList();

                    // Approve of one's first choices.
                    foreach (var c in firstChoiceCandidates)
                        approvalCount[c] += count;

                    if (previousWinners.Any())
                    {
                        var threshold = previousWinners
                            .Select(w => ballot.RanksByCandidate[w])
                            .Min();
                        
                        if (threshold == 0 || threshold == -1)
                            continue;

                        var bogeyman = previousWinners.First(w => ballot.RanksByCandidate[w] == threshold);

                        // Approve of the each candidate `c` that one likes better than any of the previous winners
                        foreach (var (rank, candidate) in ballot.RanksByCandidate.Select((r, i) => (r, i)))
                        {
                            if (rank < 0 && rank > threshold)
                            {
                                approvalCount[candidate] += count;
                                compromises.Add(new Compromise(firstChoiceCandidates[0], candidate, bogeyman), count);
                            }
                        }
                    }   
                }
 
                if (!previousWinners.Any())
                    firstChoices = approvalCount;

                var addedAny = false;
                var winningScore = approvalCount.Max();
                foreach (var winner in approvalCount.IndexesWhere(a => a == winningScore))
                    addedAny |= previousWinners.Add(winner);

                if (!addedAny)
                    return (approvalCount.IndexRanking(), approvalCount, firstChoices, compromises);
            }
        }
    }
}