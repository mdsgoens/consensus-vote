using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusVote : VotingMethodBase<RankedBallot, VotingMethodBase.Tally>
    {
        public override RankedBallot GetHonestBallot(Voter v) => new RankedBallot(v.Utilities.Count, v.Ranking);

        // TODO, machine learning maybe?
        public override RankedBallot GetStrategicBallot(Tally tally, Voter v) => GetHonestBallot(v);

        public override Tally GetTally(CandidateComparerCollection<RankedBallot> ballots)
        {
            var beatMatrix = ballots.GetBeatMatrix();

            var candidates = Enumerable.Range(0, ballots.CandidateCount);
            var approvalCount = new int[ballots.CandidateCount];
            
            foreach (var ballot in ballots)
            {
                // Approve of the each candidate `c` such that
                // For each candidate `a` which one prefers to `c`,
                // There exists a candidate `b` such that one prefers `c` to `b` and `b` beats `a` one-on-one
                foreach (var c in candidates.Where(c => 
                    candidates
                        .Where(a => ballot.Prefers(a, c))
                        .All(a => candidates.Any(b => ballot.Prefers(c, b) && beatMatrix.Beats(b, a)))))
                {
                    approvalCount[c]++;
                }
            }
            
            var sortedIndices = approvalCount
                .Select((c, i) => (Score: c, Candidate: i))
                .OrderByDescending(a => a.Score)
                .ToList();

            return new Tally(
                sortedIndices[0].Candidate,
                sortedIndices.Skip(1).TakeWhile(i => i.Score == sortedIndices[1].Score).Select(i => i.Candidate).ToArray()
            );
        }
    }
}