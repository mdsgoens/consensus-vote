using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Plurality : VotingMethodBase<ApprovalBallot, VotingMethodBase.Tally>
    {
        public override ApprovalBallot GetHonestBallot(Voter v) => new ApprovalBallot(v.CandidateCount, v.FirstPreference);

        public override ApprovalBallot GetStrategicBallot(VotingMethodBase.Tally tally, Voter v) => new ApprovalBallot(v.CandidateCount, v.Ranking
            .SelectMany(r => r)
            .First(c => c == tally.Winner || tally.RunnersUp.Any(r => r == c)));

        public override VotingMethodBase.Tally GetTally(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            var sortedCandidates = ScoreBallot.SortCandidates(ballots);
            return new Tally(
                sortedCandidates[0].Candidate,
                sortedCandidates.Skip(1).TakeWhile(i => i.Score == sortedCandidates[1].Score).Select(i => i.Candidate).ToArray()
            );
        }
    }
}