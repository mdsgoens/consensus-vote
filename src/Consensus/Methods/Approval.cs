using System;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Approval : VotingMethodBase<ApprovalBallot, VotingMethodBase.Tally>
    {
        public override ApprovalBallot GetHonestBallot(Voter v)
        {
            // Approves of all candidates in their top half of utility scale?
            // TODO: Machine Learning?
            var min = v.Utilities.Min();
            var max = v.Utilities.Max();
            var threshold = (max - min) / 2 + min;
            return new ApprovalBallot(v.Utilities.Select(u => u >= threshold));
        }

        public override ApprovalBallot GetStrategicBallot(Tally tally, Voter v)
        {
            // Approves of all candidates they like better or equal to their favorite in the top two
            var threshold = tally.RunnersUp.Select(r => v.Utilities[r])
                .Append(v.Utilities[tally.Winner])
                .Max();
            return new ApprovalBallot(v.Utilities.Select(u => u >= threshold));
        }

        public override Tally GetTally(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            var sortedCandidates = ScoreBallot.SortCandidates(ballots);
            return new Tally(
                sortedCandidates[0].Candidate,
                sortedCandidates.Skip(1).TakeWhile(i => i.Score == sortedCandidates[1].Score).Select(i => i.Candidate).ToArray()
            );
        }
    }
}