using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class BucketConsensusTopTwo : BucketConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<BucketBallot<Bucket>> ballots)
        {
            var firstChoices = new int[ballots.CandidateCount];
            var maximumApprovalCount = new int[ballots.CandidateCount];
            
            foreach (var (ballot, count) in ballots.Comparers)
            {
                foreach (var c in ballot.Buckets.IndexesWhere(a => a == Bucket.Best))
                {
                    firstChoices[c] += count;
                    maximumApprovalCount[c] += count;
                }

                foreach (var c in ballot.Buckets.IndexesWhere(a => a == Bucket.Good))
                {
                    maximumApprovalCount[c] += count;
                }
            }
     
            var firstChoiceWinners = firstChoices.MaxIndexes().ToList();
            var maximumApprovalWinners = maximumApprovalCount.MaxIndexes().ToList();

            var approvalCount = firstChoices.SelectToArray(c => c);

            var winners = ballots.Compare(firstChoiceWinners[0], maximumApprovalWinners[0]) > 0 ? firstChoiceWinners : maximumApprovalWinners;

            var results = new ElectionResults(new List<List<int>>{ winners });

            results.AddHeading("Votes");
            results.AddTable(
                approvalCount.IndexOrderByDescending()
                .Select(c => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) c,
                    approvalCount[c],
                    firstChoices[c],
                    approvalCount[c] - firstChoices[c],
                }),
                "Votes",
                "First",
                "Comp.");

            return results;
        }
    }
}