using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    // Voting method wherein one casts an Approval ballot for zero or one candidates.
    public sealed class Plurality : VotingMethodBase<ApprovalBallot>
    {
        // Vote for one's first preference
        public override ApprovalBallot GetHonestBallot(Voter v) => new ApprovalBallot(v.CandidateCount, v.FirstPreference);

        // Vote for the frontrunner we like best.
        public override IEnumerable<ApprovalBallot> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            var maxFavoriteUtility = ranking.Favorites().Max(w => v.Utilities[w]);

            if (maxFavoriteUtility != v.Utilities.Max())
                yield return new ApprovalBallot(v.CandidateCount, v.Utilities.IndexesWhere(u => u == maxFavoriteUtility).First());
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            return ScoreBallot.GetElectionResults(ballots);
        }
    }
}