using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusCoalition : ConsensusVoteBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
        {
            // first, construct the "coalition" beat matrix.
            var beatMatrix = new CoalitionBeatMatrix(ballots);

            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromises = new CountedList<(int Compromise, ulong Preferred, ulong Bogeymen)>();
            
            foreach (var (ballot, count) in ballots.Comparers)
            {
                var ranking = ballot.GetRanking();
        
                // Approve of the each candidate `c` which is a first choice.
                foreach (var c in ranking[0])
                {
                    firstChoices[c] += count;
                    approvalCount[c] += count;
                }

                // Then, for each candidate `b` (the "bogeyman") ranked third or below, 
                // compute the smallest "coalition" of candidates `c` which can beat each potential "bogeyman" `b`,
                // or includes all candidates one likes better than `b`, whichever comes first.
                // Approve of each member of the smallest coalition to beat all bogeymen.
                // NOTE: This means that there exists no smaller coalition of "more preferred" candidates one could vote for which would ensure a more preferred outcome.
                if (ranking.Count >= 3)
                {
                    var coalition = GetCoalition(ranking[0]);
                    var potentialBogeymen = ranking
                        .Skip(2)
                        .SelectMany(b => b)
                        .Where(b => !beatMatrix.Beats(coalition, b))
                        .ToList();

                    if (!potentialBogeymen.Any())
                        continue;

                    var compromiseCandidates = new List<(int Candidate, ulong Preferred)>();
                    foreach (var tier in ranking.Skip(1).Take(ranking.Count - 2))
                    {
                        var greaterBogeymen = potentialBogeymen.Where(b => !tier.Contains(b)).ToList();

                        // If this tier contains all the bogeymen, the previous tier is where our approval stops.
                        if (!greaterBogeymen.Any())
                            break;

                        // Otherwise, enlist the help of the lesser bogeymen to beat the greater bogeymen.
                        foreach (var c in tier)
                            compromiseCandidates.Add((c, coalition));

                        coalition |= GetCoalition(tier);
                          
                        var remainingBogeymen = greaterBogeymen
                            .Where(b => !beatMatrix.Beats(coalition, b))
                            .ToList();

                        // Huzzah! We have beaten all the bogeymen!
                        if (!remainingBogeymen.Any())
                            break;

                        // Otherwise, check the next teir for potential coalition-mates.
                        potentialBogeymen = remainingBogeymen;
                    }

                    var finalBogeymen = GetCoalition(potentialBogeymen);

                    foreach (var (candidate, preferred) in compromiseCandidates)
                    {
                        approvalCount[candidate] += count;
                        compromises.Add((candidate, preferred, finalBogeymen), count);
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

            beatMatrix.AddDetails(results);

            return results;
        }

        private sealed class CoalitionBeatMatrix
        {
            public CoalitionBeatMatrix(CandidateComparerCollection<RankedBallot> ballots)
            {
                m_beatMatrix = Enumerable.Range(0, ballots.CandidateCount)
                    .Select(_ => new Dictionary<ulong, bool>())
                    .ToArray();

                m_coalitionSupporters = Enumerable.Range(0, ballots.CandidateCount)
                    .Select(_ => new CountedList<ulong>())
                    .ToArray();

                // TODO: Some data-structure more performant than a list.
                // Probably a tree based on coalition-subsumation?
                m_coalitionDetractors = Enumerable.Range(0, ballots.CandidateCount)
                    .Select(_ => new CountedList<ulong>())
                    .ToArray();
                    
                foreach (var (ballot, count) in ballots.Comparers)
                {
                    var preferredCoalition = 0ul;
                    var dispreferredCoalition = (1ul << m_beatMatrix.Length) - 1;
                    var ranking = ballot.GetRanking();

                    foreach (var tier in ranking.Take(ranking.Count))
                    {
                        var teirCoalition = GetCoalition(tier);
                        dispreferredCoalition ^= teirCoalition;

                        if (preferredCoalition != 0ul)
                        {
                            foreach (var candidate in tier)
                                m_coalitionSupporters[candidate].Add(preferredCoalition, count);
                        }

                        if (dispreferredCoalition != 0ul)
                        {
                            foreach (var candidate in tier)
                                m_coalitionDetractors[candidate].Add(dispreferredCoalition, count);
                        }

                        preferredCoalition |= teirCoalition;
                    }
                }
            }

            public bool Beats(ulong coalition, int candidate)
            {
                // Try the cache first.
                if (m_beatMatrix[candidate].TryGetValue(coalition, out var value))
                    return value;

                // A coalition beats a candidate if any sub-coalition beats that candidate.
                foreach (var subCoalition in GetImmediateSubCoalitions(coalition))
                {
                    if (Beats(subCoalition, candidate))
                        return m_beatMatrix[candidate][coalition] = true; 
                }

                // a coalition `a` beats a candidate `b` if:
                // the number of people who support **every** member of coalition `a` over candidate `b`
                // is greater than the number of people who support candidate `b` over **any** member of coalition `a`.
                var support = 0;

                foreach (var (supporterCoalition, count) in m_coalitionSupporters[candidate])
                {
                    // There every candidate in `coalition` and also in `supporterCoalition`
                    if ((coalition & supporterCoalition) == coalition)
                        support += count;
                }

                foreach (var (detractorCoalition, count) in m_coalitionDetractors[candidate])
                {
                    // There is at least one candidate in `coalition` and also in `detractorCoalition`
                    if ((coalition & detractorCoalition) > 0)
                        support -= count;
                }

                return m_beatMatrix[candidate][coalition] = support > 0;
            }

            public void AddDetails(ElectionResults results)
            {
                results.AddHeading("Beat Matrix");
                results.AddTable(m_beatMatrix.SelectMany((d, i) => d.Select(kvp => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) i,
                    kvp.Key,
                    kvp.Value ? "y" : "",
                })),
                    "Cand.",
                    "Coa.",
                    "Beats?"
                    );

                results.AddHeading("Supporters");
                results.AddTable(m_coalitionSupporters.SelectMany((list, candidate) => list.Select(a => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) candidate,
                    a.Item,
                    a.Count,
                })),
                    "Cand.",
                    "Coa.",
                    "Count"
                    );

                results.AddHeading("Detractors");
                results.AddTable(m_coalitionDetractors.SelectMany((list, candidate) => list.Select(a => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) candidate,
                    a.Item,
                    a.Count,
                })),
                    "Cand.",
                    "Coa.",
                    "Count"
                    );
            }

            private static IEnumerable<ulong> GetImmediateSubCoalitions(ulong coalition)
            {
                for (var mask = 1ul; mask < coalition; mask = mask << 1)
                {
                    if ((mask & coalition) > 0)
                        yield return mask ^ coalition;
                }
            }           

            private readonly Dictionary<ulong, bool>[] m_beatMatrix;
            private readonly CountedList<ulong>[] m_coalitionSupporters;
            private readonly CountedList<ulong>[] m_coalitionDetractors;
        }
    }
}