using System;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class RankedConsensusCondorcet : RankedConsensusBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
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
                foreach (var c in ballot.Ranking[0])
                {
                    approvalCount[c] += count;
                    firstChoices[c] += count;
                }
                
                // If one prefers all canidates who beat a bogeyman to said bogeyman, do so.
                // (if we don't, neither will the people who prefer the other saviours -- and we won't be able to beat the bogeyman)
                // Otherwise, one approve of *all* candidates one likes better than the bogeyman.
                // Finally, approve the bogeymen we like best, so long as we don't rank them all last.
                var approveUntilRank = 0;
                var preferredBogeymenRank = 2 - ballot.Ranking.Count;
                var preferredBogeymenCoalition = 0ul;
                var saviourCoalition = 0ul;

                foreach (var (bogeyman, saviours) in bogeymen)
                {
                    var bogeymanRank = ballot.RanksByCandidate[bogeyman];

                    if (bogeymanRank > preferredBogeymenRank)
                    {
                        preferredBogeymenCoalition = GetCoalition(bogeyman);
                        preferredBogeymenRank = bogeymanRank;
                    }

                    var saviourRanks = saviours.Select(s => ballot.RanksByCandidate[s]).ToList();

                    if (saviours.Any() && saviourRanks.All(sr => sr > bogeymanRank))
                    {
                        saviourCoalition |= GetCoalition(saviours);
                        approveUntilRank = saviourRanks.Append(approveUntilRank).Min();
                    }
                    else
                    {
                        approveUntilRank = Math.Min(approveUntilRank, bogeymanRank);

                        if (bogeymanRank == preferredBogeymenRank)
                            preferredBogeymenCoalition |= GetCoalition(bogeyman);
                    }
                }

                if (approveUntilRank >= preferredBogeymenRank)
                    approveUntilRank = preferredBogeymenRank;

                var approvalCoalition = saviourCoalition | preferredBogeymenCoalition;
                
                // Shortcut -- we already approve of rank one.
                if (approveUntilRank == 0)
                    continue;

                var preferred = GetCoalition(ballot.Ranking[0]);

                var bogeymenCoalition = GetCoalition(bogeymen
                    .Select(b => b.Bogeyman)
                    .Where(b => ballot.RanksByCandidate[b] <= approveUntilRank && !((approvalCoalition & GetCoalition(b)) > 0)));
   
                foreach (var tier in ballot.Ranking.Skip(1))
                {
                    foreach (var c in tier)
                    {
                        if (ballot.RanksByCandidate[c] > approveUntilRank || (approvalCoalition & GetCoalition(c)) > 0)
                        {
                            approvalCount[c] += count;
                            compromises.Add((preferred, c, bogeymenCoalition), count);
                        }
                    }

                    preferred |= GetCoalition(tier);
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