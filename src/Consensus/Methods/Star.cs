using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Star : VotingMethodBase<ScoreBallot>
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
            var minFavoriteUtility = favorites.Min(w => v.Utilities[w]);
            var maxUtility = v.Utilities.Max();
            var minUtility = v.Utilities.Max();

            // Approves of all candidates they like better or equal to the polling EV
            // If two "viable" candidates both meet that condition, knock the least-preferred down one peg.
            // If two "viable" candidates both fail that condition, bump the most-preferred up one peg.
            yield return ("Exaggeration", 1, new ScoreBallot(v.Utilities.Select(u =>
                u == maxUtility || u > maxFavoriteUtility ? c_scale
                : u == maxFavoriteUtility ? c_scale - 1
                : u == minUtility || u < minFavoriteUtility ? 1
                : 2)));

            if (maxUtility != maxFavoriteUtility)
            {
                // Maximizes chances for favorite (mostly)
                yield return ("Truncation", 1, new ScoreBallot(v.Utilities.Select(u => 
                    u == maxUtility ? c_scale
                    : u >= maxFavoriteUtility ? 2
                    : 1)));
            }
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ScoreBallot> ballots)
        {
            // Choose the top two scorers
            // NOTE: No tiebreaking
            var sortedCandidates = ScoreBallot.GetElectionResults(ballots).Ranking;

            int Pop()
            {
                var value = sortedCandidates[0][0];

                sortedCandidates[0].RemoveAt(0);
                if (sortedCandidates[0].Count == 0)
                    sortedCandidates.RemoveAt(0);

                return value;
            }

            var first = Pop();
            var second = Pop();

            // Then compare those two head-to-head
            var compare = ballots.Compare(first, second);

            if (compare == 0)
            {
                sortedCandidates.Insert(0, new List<int> { first, second });
            }
            else if (compare < 0)
            {
                sortedCandidates.Insert(0, new List<int> { first });
                sortedCandidates.Insert(0, new List<int> { second });
            }
            else
            {
                sortedCandidates.Insert(0, new List<int> { second });
                sortedCandidates.Insert(0, new List<int> { first });
            }

            return new ElectionResults(sortedCandidates);
        }

        const int c_scale = 5;
    }
}