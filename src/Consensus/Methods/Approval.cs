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
            var threshold = v.Utilities.Average();
            return new ApprovalBallot(v.Utilities.SelectToArray(u => u >= threshold));
        }

        public override IEnumerable<(string, int, ApprovalBallot)> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            var favorites = ranking.Favorites();
            var maxFavoriteUtility = favorites.Max(w => v.Utilities[w]);
            var maxUtility = v.Utilities.Max();
            var averageUtility = v.Utilities.Average();
            var honestApprovalCount = v.Utilities.Count(u => u >= averageUtility);

            // Approves of all candidates they like better or equal to the polling EV
            // https://www.rangevoting.org/RVstrat7.html
            if (v.Utilities.Count(u => u >= maxFavoriteUtility) != honestApprovalCount) 
                yield return ("Exaggeration", 1, new ApprovalBallot(v.Utilities.SelectToArray(u => u >= maxFavoriteUtility)));

            // Maximizes chances for favorite (mostly)
            if (maxUtility != maxFavoriteUtility && v.Utilities.Count(u => u == maxUtility) != honestApprovalCount)
                yield return ("Truncation", 1, new ApprovalBallot(v.Utilities.SelectToArray(u => u == maxUtility)));
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            return ScoreBallot.GetElectionResults(ballots);
        }
    }
}