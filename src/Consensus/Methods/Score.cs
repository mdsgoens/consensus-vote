using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Score : VotingMethodBase<ScoreBallot>
    {
        // Evenly distributes candidates along utility scale
        public override ScoreBallot GetHonestBallot(Voter v)
        {
            var min = v.Utilities.Min();
            var max = v.Utilities.Max();
            double scale = c_scale / (double) (max - min);  
            return new ScoreBallot(v.Utilities.Select(u => u > min ? (int) Math.Ceiling((u - min) * scale) : 1));
        }

        public override IEnumerable<(string, int, ScoreBallot)> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            var favorites = ranking.Favorites();
            var maxFavoriteUtility = favorites.Max(w => v.Utilities[w]);
            var maxUtility = v.Utilities.Max();

            // Approves of all candidates they like better or equal to the polling EV
            yield return ("Exaggeration", 1, new ScoreBallot(v.Utilities.Select(u => u >= maxFavoriteUtility ? c_scale : 1)));

            // Maximizes chances for favorite (mostly)
            if (maxUtility != maxFavoriteUtility)
                yield return ("Truncation", 1, new ScoreBallot(v.Utilities.Select(u => u == maxUtility ? c_scale : 1)));
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ScoreBallot> ballots)
        {
            return ScoreBallot.GetElectionResults(ballots);
        }

        const int c_scale = 5;
    }
}