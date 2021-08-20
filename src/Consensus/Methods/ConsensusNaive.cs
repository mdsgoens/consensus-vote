using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusNaive : ConsensusVoteBase
    {
        public override (List<List<int>> Ranking, int[] ApprovalCount, int[] FirstChoices, IEnumerable<(Compromise Compromise, int Count)> Compromises) GetDetailedTally(CandidateComparerCollection<RankedBallot> ballots)
        {
            var beatMatrix = ballots.GetBeatMatrix();

            var candidates = Enumerable.Range(0, ballots.CandidateCount);
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromises = new CountedList<Compromise>();
            
            foreach (var (ballot, count) in ballots.Comparers)
            {
                var ranking = ballot.GetRanking();
        
                // Approve of the each candidate `c` which is a first choice.
                foreach (var c in ranking[0])
                {
                    firstChoices[c] += count;
                    approvalCount[c] += count;
                }

                // Approve of the each candidate `c` such that
                // For each candidate `a` which one prefers to `c`,
                // There exists a candidate `b` (the 'bogeyman') such that one prefers `c` to `b` and `b` beats `a` one-on-one
                if (ranking.Count >= 3)
                {
                    var preferredCandidates = ranking[0].ToList();
                    var potentialBogeymen = Enumerable.Range(0, ballots.CandidateCount).ToHashSet();
                    potentialBogeymen.ExceptWith(preferredCandidates);

                    foreach (var tier in ranking.Skip(1).Take(ranking.Count - 2))
                    {
                        potentialBogeymen.ExceptWith(tier);

                        if (preferredCandidates.All(a => potentialBogeymen.Any(b => beatMatrix.Beats(b, a))))
                        {
                            var bogeyman = potentialBogeymen.First(b => preferredCandidates.Any(a => beatMatrix.Beats(b, a)));
                            foreach (var c in tier)
                            {
                                approvalCount[c] += count;
                                compromises.Add(new Compromise(ranking[0][0], c, bogeyman), count);
                            }
                        }

                        preferredCandidates.AddRange(tier);
                    }
                }
            }
            
            return (approvalCount.IndexRanking(), approvalCount, firstChoices, compromises);
        }
    }
}