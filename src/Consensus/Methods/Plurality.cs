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
        public override IEnumerable<(string, int, ApprovalBallot)> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            var favorites = ranking.Favorites();
            var maxFavoriteUtility = favorites.Max(w => v.Utilities[w]);
            var maxUtility = v.Utilities.Max();
       
            // Vote for the *frontrunner* we like best.
            if (maxUtility != maxFavoriteUtility)
                yield return ("Favorite Betrayal", 1, new ApprovalBallot(v.CandidateCount, v.Utilities.IndexesWhere(u => u == maxFavoriteUtility).First()));
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            return ScoreBallot.GetElectionResults(ballots);
        }
    }
}