using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    // approval is granted *porportionally* to how often the support is reciprocated
    public sealed class BucketConsensusReciprocal : BucketConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<BucketBallot<Bucket>> ballots)
        {
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromiseBallots = new HashSet<BucketBallot<Bucket>>();
            var history = new List<(int[] NewApprovalCount, List<int> Winners, CountedList<(ulong Preferred, int Candidate)> Compromises)>();
            
            // Approve of one's first choices.
            foreach (var (ballot, count) in ballots.Comparers)
            {
                foreach (var c in ballot.Buckets.IndexesWhere(a => a == Bucket.Best))
                {
                    approvalCount[c] += count;
                    firstChoices[c] += count;
                }
            }

            for (int first = 0; first < ballots.CandidateCount; first++)
            {
                for (int second = 0; second < ballots.CandidateCount; second++)
                {
                    if (first != second)
                    {
                        int firstApproved = 0;
                        int firstApprovedAndSecondApproved = 0;
                        double secondPreferredCompromisers = 0;

                        foreach (var (ballot, count) in ballots.Comparers)
                        {
                            if (ballot.Buckets[first] != Bucket.Bad)
                                firstApproved += count;

                            if (ballot.Buckets[first] != Bucket.Bad && ballot.Buckets[second] != Bucket.Bad)
                                firstApprovedAndSecondApproved += count;
                            
                            // We'll evaluate this ballot's contribution to `first` once for each of the candidates `second` it prefers.
                            if (ballot.Buckets[first] == Bucket.Good && ballot.Buckets[second] == Bucket.Best)
                                secondPreferredCompromisers += count / ballot.Buckets.Count(b => b == Bucket.Best);
                        }

                        approvalCount[first] += (int) (firstApproved > 0 ? secondPreferredCompromisers * firstApprovedAndSecondApproved / firstApproved : secondPreferredCompromisers);
                    }
                }
            }
     
            var results = new ElectionResults(approvalCount.IndexRanking());

            results.AddHeading("Votes");
            results.AddTable(
                approvalCount.IndexOrderByDescending()
                .Select(c => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) c,
                    approvalCount[c],
                    firstChoices[c],
                    approvalCount[c] - firstChoices[c],
                }),
                "Total",
                "First",
                "Comp.");

            return results;
        }
    }
}