using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class BucketConsensusBeats : BucketConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<BucketBallot<Bucket>> ballots)
        {
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var approvalByBallot = new Dictionary<RankedBallot, ulong>();
            
            // Approve of one's first choices.
            foreach (var (ballot, count) in ballots.Comparers)
            {
                foreach (var c in ballot.Buckets.IndexesWhere(a => a == Bucket.Best))
                {
                    approvalCount[c] += count;
                    firstChoices[c] += count;
                }
            }
     
            var beatMatrix = ballots.GetBeatMatrix();

            var candidates = Enumerable.Range(0, ballots.CandidateCount);
            var compromises = new CountedList<(ulong Preferred, int Compromise, ulong Bogeymen)>();
            
            foreach (var (ballot, count) in ballots.Comparers)
            {
                var best = ballot.Buckets.IndexesWhere(a => a == Bucket.Best).ToList();
                var good = ballot.Buckets.IndexesWhere(a => a == Bucket.Good).ToList();
                var bad = ballot.Buckets.IndexesWhere(a => a == Bucket.Bad).ToList();

                // Support each "good" candidate `g` such that 
                // For each "best" candidate `c`
                // There exists a "bad" candidate `b` (the 'bogeyman') such that `b` beats `c` in first-choice votes but loses to `g` one-on-one
                if (good.Any() && bad.Any())
                {
                    var bogeymen = bad
                        .Where(b => best.All(c => firstChoices[b] > firstChoices[c]))
                        .ToList();
                        
                    if (bogeymen.Any())
                    {
                        var bestCoalition = GetCoalition(best);
                        var bogeymenCoalition = GetCoalition(bogeymen);

                        foreach (var c in good.Where(g => bogeymen.Any(b => beatMatrix.Beats(g, b))))
                        {
                            approvalCount[c] += count;
                            compromises.Add((bestCoalition, c, bogeymenCoalition), count);
                        }
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
            results.AddTable(compromises
                .Select(c => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) c.Item.Compromise,
                    c.Item.Preferred,
                    c.Item.Bogeymen,
                    c.Count
                }),
                "Comp.",
                "Pref.",
                "Bogey.",
                "Count");
            
            return results;
        }
    }
}