using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class V321 : VotingMethodBase<BucketBallot<V321.Result>>
    {
        public override BucketBallot<V321.Result> GetHonestBallot(Voter v)
        {
            var candidateCount = v.CandidateCount;
            var ballot = new Result[candidateCount];
            var goodCount = Math.Max(1, (int) Math.Floor(candidateCount * c_goodPercentile));
            var adequateCount = Math.Max(0, (int) Math.Floor(candidateCount * c_adequatePercentile) - goodCount);

            foreach (var candidates in v.Ranking)
            {
                var result = goodCount > 0 ? Result.Good
                    : adequateCount > 0 ? Result.Adequate
                    : Result.Bad;
                
                foreach (var c in candidates)
                {
                    ballot[c] = result;

                    if (goodCount > 0)
                        goodCount--;
                    else if (adequateCount > 0)
                        adequateCount--;
                }
            }

            return new BucketBallot<Result>(ballot);
        }

        // TODO
        //https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/methods.py#L652
        public override BucketBallot<V321.Result> GetStrategicBallot(Polling polling, Voter v) => GetHonestBallot(v);

        public override List<List<int>> GetRanking(CandidateComparerCollection<BucketBallot<V321.Result>> ballots)
        {
            // Find 3 Semifinalists: the candidates with the most “good” ratings.
            // Find 2 Finalists: the semifinalists with the fewest "bad" ratings".
            // Find 1 winner: the finalist who is rated above the other on more ballots.
            var bucketCounts = BucketBallot<V321.Result>.GetBucketCounts(ballots);

            var finalists = bucketCounts[Result.Good]
                .IndexOrderByDescending()
                .Take(3)
                .OrderBy(c => bucketCounts[Result.Bad][c])
                .ToList();

            var first = finalists[0];
            var second = finalists[1];
            var compare = ballots.Compare(first, second);

            return (compare == 0 ? new List<List<int>> { new List<int> { first, second } }
                : compare < 0 ? new List<List<int>> { new List<int> { second }, new List<int> { first } }
                : new List<List<int>> { new List<int> { first }, new List<int> { second } })
                .Concat(bucketCounts[Result.Good]
                    .IndexOrderByDescending()
                    .Skip(3)
                    .Select(c => new List<int> { c }))
                .ToList();
        }

        public enum Result
        {
            Bad = 0,
            Adequate = 1,
            Good = 2,
        }

        private const decimal c_goodPercentile = .25m;
        private const decimal c_adequatePercentile = .55m;
    }
}