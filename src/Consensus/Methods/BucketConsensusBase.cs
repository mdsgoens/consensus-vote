using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public abstract class BucketConsensusBase : VotingMethodBase<BucketBallot<BucketConsensusBase.Bucket>>
    {
        public enum Bucket
        {
            Best = 0,
            Good = -1,
            Bad = -2,
        }

        public override BucketBallot<Bucket> GetHonestBallot(Voter v)
        {
            var (best, bad) = GetThresholds(v.Utilities);

            return new BucketBallot<Bucket>(v.Utilities
                .SelectToArray(u =>
                    u > best ? Bucket.Best
                    : u < bad ? Bucket.Bad
                    : Bucket.Good));
        }

        private (double best, double bad) GetThresholds(int[] utilities)
        {
            var min = utilities.Min();
            var max = utilities.Max();
            var oneSextile = (max - min) / 6d;

            var potentialApprovals = utilities
                .Where(u => u != min && u != max)
                .ToList();

            if (!potentialApprovals.Any())
                return (max - oneSextile, min + oneSextile);

            var midpoint = (max - min) / 2d + min;

            // Add some nuance so there's at least one candidate in each bucket
            var goodRange = potentialApprovals
                .Select(u => Math.Max(Math.Abs(u - midpoint), oneSextile))
                .Min();

            return (midpoint + goodRange, midpoint - goodRange);
        }

        public override IEnumerable<(string, int, BucketBallot<Bucket>)> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            var favorites = ranking.Favorites();
            var winnerUtility = ranking[0].Max(w => v.Utilities[w]);
            var maxFavoriteUtility = favorites.Max(w => v.Utilities[w]);
            var maxUtility = v.Utilities.Max();
            var minUtility = v.Utilities.Min();
            var (best, bad) = GetThresholds(v.Utilities);

            // Explicitly approve of all candidates one likes better or equal to the favored frontrunner
            yield return ("Exaggeration", 1, NewBallot(u => u >= maxFavoriteUtility ? Bucket.Best : Bucket.Bad));

            if (maxUtility != maxFavoriteUtility)
            {
                // Maximizes chances for favorite alone
                yield return ("Truncation", 1, NewBallot(u => u == maxUtility ? Bucket.Best : Bucket.Bad));
            }

            if (winnerUtility >= bad && winnerUtility != maxUtility)
            {
                // Artificially rank the winner lower.
                yield return ("Burying", 1, NewBallot(u => u == winnerUtility ? Bucket.Bad : null));
            }

            // Maybe our favorite was someone else's bogeyman?
            yield return ("Favorite Betrayal Mild", 1, NewBallot(u => u == maxUtility ? Bucket.Good : null));

            yield return ("Favorite Betrayal", 2, NewBallot(u => u == maxUtility ? Bucket.Bad : null));

            if (winnerUtility == maxFavoriteUtility && maxUtility != maxFavoriteUtility)
            {
               yield return ("Post-Winner Truncation", 1, NewBallot(u => 
                    u > winnerUtility ? Bucket.Best
                    : u == winnerUtility ? Bucket.Good
                    : Bucket.Bad));
            }

            if (winnerUtility <= best && minUtility != winnerUtility)
            {
                // Support everyone we like **worse* than winner in hopes that more people will rally around the favored frontrunner
                yield return ("Dark Horse", 3, NewBallot(u => 
                    u < winnerUtility ? Bucket.Good
                    : u == winnerUtility ? Bucket.Bad
                    : null));

                yield return ("Dark Horse", 3, NewBallot(u => 
                    u < winnerUtility ? Bucket.Best
                    : null));
            }

            // Vote for *no-one*.
            yield return ("Abstain", 3, NewBallot(u => Bucket.Bad));

            BucketBallot<Bucket> NewBallot(Func<int, Bucket?> getBucket) => new BucketBallot<Bucket>(v.Utilities.SelectToArray(u => getBucket(u) ?? (
                u > best ? Bucket.Best
                : u < bad ? Bucket.Bad
                : Bucket.Good)));
        }
    }
}