using System;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class BucketConsensusCondorcetSimple : BucketConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<BucketBallot<Bucket>> ballots)
        {
            var beatMatrix = ballots.GetBeatMatrix();
            var bogeymen = beatMatrix
                .GetSchulzeSet()
                .ToList();

            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromises = new CountedList<(ulong Preferred, int Compromise, ulong Bogeymen)>();

            foreach (var (ballot, count) in ballots.Comparers)
            {
                // Always approve of first choices.
                foreach (var c in ballot.Buckets.IndexesWhere(b => b == Bucket.Best))
                {
                    approvalCount[c] += count;
                    firstChoices[c] += count;
                }
                
                // If one disapproves of any bogeymen, support one's second choices too.
                var bogeymanCoalition = GetCoalition(bogeymen.Where(b => ballot.Buckets[b] == Bucket.Bad));
                if (bogeymanCoalition != 0ul)
                {
                    var best = GetCoalition(ballot.Buckets.IndexesWhere(b => b == Bucket.Best));

                    foreach (var c in ballot.Buckets.IndexesWhere(b => b == Bucket.Good))
                    {
                        approvalCount[c] += count;
                        compromises.Add((best, c, bogeymanCoalition), count);
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

            results.AddHeading("Compromises");
            results.AddTable(
                compromises.Select(c => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) c.Item.Compromise,
                    c.Item.Preferred,
                    c.Item.Bogeymen,
                    c.Count
                }),
                "Comp.",
                "Pref.",
                "Bogey",
                "Count"
            );

            return results;
        }
    }
}