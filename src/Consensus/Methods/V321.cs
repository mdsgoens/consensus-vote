using System.Linq;
using System;
using System.Collections.Generic;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class V321 : VotingMethodBase<BucketBallot<V321.Result>, VotingMethodBase.Tally>
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
        public override BucketBallot<V321.Result> GetStrategicBallot(VotingMethodBase.Tally tally, Voter v) => GetHonestBallot(v);

        public override VotingMethodBase.Tally GetTally(CandidateComparerCollection<BucketBallot<V321.Result>> ballots)
        {
            // Find 3 Semifinalists: the candidates with the most “good” ratings.
            // Find 2 Finalists: the semifinalists with the fewest rejections.
            // Find 1 winner: the finalist who is rated above the other on more ballots.
            var bucketCounts = BucketBallot<V321.Result>.GetBucketCounts(ballots);

            var finalists = bucketCounts[Result.Good]
                .Select((c, i) => (Count: c, Candidate: i))
                .OrderByDescending(a => a.Count)
                .Take(3)
                .OrderBy(a => bucketCounts[Result.Bad][a.Candidate])
                .ToList();

            var first = finalists[0].Candidate;
            var second = finalists[1].Candidate;

            return ballots.Compare(first, second) > 0
                ? new Tally(first, new [] { second })
                : new Tally(second, new [] { first });
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