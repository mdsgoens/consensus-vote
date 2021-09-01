using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Approval : VotingMethodBase<ApprovalBallot>
    {
        public override ApprovalBallot GetHonestBallot(Voter v)
        {
            // Approves of all candidates in their top half of utility scale?
            // TODO: determine what value here maximizes overall satisfaction?
            var min = v.Utilities.Min();
            var max = v.Utilities.Max();
            var threshold = v.Utilities.Average();
            return new ApprovalBallot(v.Utilities.Select(u => u >= threshold));
        }

        // Approves of all candidates they like better or equal to the polling EV
        // https://www.rangevoting.org/RVstrat7.html
        public override IEnumerable<ApprovalBallot> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            var maxFavoriteUtility = ranking.Favorites().Max(w => v.Utilities[w]);

            yield return new ApprovalBallot(v.Utilities.Select(u => u >= maxFavoriteUtility));
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            return ScoreBallot.GetElectionResults(ballots);
        }
    }
}