using System;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class BucketConsensusCondorcet : BucketConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<BucketBallot<Bucket>> ballots)
        {
            var beatMatrix = ballots.GetBeatMatrix();
            var bogeymen = beatMatrix
                .GetSchulzeSet()
                .Select(b => (
                    Bogeyman: b,
                    Saviours: Enumerable.Range(0, ballots.CandidateCount)
                        .Where(s => beatMatrix.Beats(s, b))
                        .ToList()))
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
                
                // If one prefers all canidates who beat a bogeyman to said bogeyman, support them all.
                // (if we don't, neither will the people who prefer the other saviours -- and we won't be able to beat the bogeyman)
                // Otherwise, support of *all* candidates one likes better than the bogeyman.
                var bogeymanCoalition = 0ul;
                var saviourCoalition = 0ul;

                foreach (var (bogeyman, saviours) in bogeymen)
                {
                    var bogeymanRank = ballot.Buckets[bogeyman];

                    if (ballot.Buckets[bogeyman] != Bucket.Bad)
                        continue;

                    if (saviours.Any() && saviours.All(s => ballot.Buckets[s] == Bucket.Best))
                        continue;
                        
                    if (saviours.Any() && saviours.All(s => ballot.Buckets[s] != Bucket.Bad))
                    {
                        bogeymanCoalition |= GetCoalition(bogeyman);
                        saviourCoalition |= GetCoalition(saviours.Where(s => ballot.Buckets[s] == Bucket.Good));
                    }
                    else
                    {
                        bogeymanCoalition |= GetCoalition(bogeyman);
                        saviourCoalition = GetCoalition(ballot.Buckets.IndexesWhere(b => b == Bucket.Good));
                    }
                }

                if (saviourCoalition == 0ul)
                    continue;

                var best = GetCoalition(ballot.Buckets.IndexesWhere(b => b == Bucket.Best));

                foreach (var c in GetCandidates(saviourCoalition))
                {
                    approvalCount[c] += count;
                    compromises.Add((best, c, bogeymanCoalition), count);
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

            results.AddHeading("Bogeymen");
            results.AddTable(
                bogeymen.Select(b =>  new ElectionResults.Value[] {
                    (ElectionResults.Candidate) b.Bogeyman,
                    b.Saviours
                }),
                "Saviours");

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