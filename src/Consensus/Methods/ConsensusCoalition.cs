using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusCoalition : ConsensusVoteBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
        {
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromises = new CountedList<(int Compromise, ulong Preferred, ulong Bogeymen)>();

            // First, compute "coalition" beat matrix
            var beatMatrix = new CoalitionBeatMatrix(ballots);
    
            // Then, use it to determine which candidates each ballot approves of.
            foreach (var (ballot, count) in ballots.Comparers)
            {
                var (approvedCandidates, bogeymen) = GetApprovedCandidates(beatMatrix, ballot);
                foreach (var (candidate, preferred) in approvedCandidates)
                {
                    approvalCount[candidate] += count;

                    if (preferred.HasValue)
                        compromises.Add((candidate, preferred.Value, bogeymen), count);
                    else
                        firstChoices[candidate] += count;
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
  
        private static (List<(int candidate, ulong? preferred)> Candidates, ulong Bogeyman) GetApprovedCandidates(CoalitionBeatMatrix beatMatrix, RankedBallot ballot)
        {
            var candidates = new List<(int candidate, ulong? preferred)>();
            var ranking = ballot.Ranking;

            // Approve of the each candidate `c` which is a first choice.
            foreach (var c in ranking[0])
                candidates.Add((c, null));

            // With fewer than three ranks, no bogeymen can chance one's preferences.
            if (ranking.Count < 3)
                return (candidates, 0ul);

            // Then, for each candidate `b` (the "bogeyman") ranked third or below, 
            // compute the smallest "coalition" of candidates `c` which can beat each potential "bogeyman" `b`,
            // or includes all candidates one likes better than `b`, whichever comes first.
            // Approve of each member of the smallest coalition to beat all bogeymen. 
            var coalition = GetCoalition(ranking[0]);
            var potentialBogeymen = ranking
                .Skip(2)
                .SelectMany(b => b)
                .Where(b => beatMatrix.Beats(coalition, b) != true)
                .ToList();

            if (!potentialBogeymen.Any())
                return (candidates, 0ul);

            foreach (var tier in ranking.Skip(1).Take(ranking.Count - 2))
            {
                var greaterBogeymen = potentialBogeymen.Where(b => !tier.Contains(b)).ToList();

                // If this tier contains all the bogeymen, the previous tier is where our approval stops.
                if (!greaterBogeymen.Any())
                    break;

                // Otherwise, enlist the help of the lesser bogeymen to beat the greater bogeymen.
                foreach (var c in tier)
                    candidates.Add((c, coalition));

                coalition |= GetCoalition(tier);

                var remainingBogeymen = greaterBogeymen
                    .Where(b => beatMatrix.Beats(coalition, b) != true)
                    .ToList();

                // Huzzah! We have beaten all the bogeymen!
                if (!remainingBogeymen.Any())
                    break;

                // Otherwise, check the next teir for potential coalition-mates.
                potentialBogeymen = remainingBogeymen;
            }

            return (candidates, GetCoalition(potentialBogeymen));
        }

        private sealed class CoalitionBeatMatrix
        {
            public CoalitionBeatMatrix(CandidateComparerCollection<RankedBallot> ballots)
            {
                m_ballots = ballots;

                m_beatMatrix = Enumerable.Range(0, ballots.CandidateCount)
                    .Select(_ => new Dictionary<ulong, int?>())
                    .ToArray();
            }

            public bool? Beats(ulong coalition, int bogeyman)
            {
                if (coalition == 0ul || (coalition & GetCoalition(bogeyman)) > 0ul)
                    throw new InvalidOperationException("There's a bug: We should never ask if a candidate can beat themselves.");

                // Try the cache first.
                if (m_beatMatrix[bogeyman].TryGetValue(coalition, out var value))
                    return value > 0;

                // TODO: Verify this doesn't add any weird non-deterministic ballot-count-order-ness.
                if (!m_calculatingCoalitions.Add(coalition))
                    return null;

                m_beatMatrix[bogeyman][coalition] = GetSupportForCoalition(coalition, bogeyman);

                m_calculatingCoalitions.Remove(coalition);

                return m_beatMatrix[bogeyman][coalition] > 0;
            }

            private int? GetSupportForCoalition(ulong coalition, int bogeyman)
            {
                // A coalition beats a candidate if any sub-coalition beats that candidate.
                // This ensures we can just check "maximal" coalitions and be ensured we're not thrown off by spoiler candidates
                foreach (var subCoalition in GetImmediateSubCoalitions(coalition))
                {
                    if (Beats(subCoalition, bogeyman) == true)
                        return m_beatMatrix[bogeyman][subCoalition]; 
                }

                var supporters = 0;
                var conditionalSupporters = 0;
                var detractors = 0;
                foreach (var (ballot, count) in m_ballots.Comparers)
                {
                    switch (GetSupportForCoalition(ballot, coalition, bogeyman))
                    {
                        case SupportResult.Supporter:
                            supporters += count;
                            break;
                        case SupportResult.Detractor:
                            detractors += count;
                            break;
                        case SupportResult.ConditionalSupporter:
                            conditionalSupporters += count;
                            break;
                    };
                }

                if (conditionalSupporters == 0)
                    return supporters - detractors;

                // The coalition wins even if all conditional supporters remain neutral.
                if (supporters > detractors)
                    return supporters - detractors;
                
                // The coalition loses even if all conditional supporters support it.
                if (detractors > supporters + conditionalSupporters)
                    return supporters + conditionalSupporters - detractors;
                
                // We don't know yet :(
                return null;
            }

            // A ballot supports a bogeyman over a coalition if there are any candidates in the coalition one likes worse than the bogeyman.
            // A ballot remains neutral between a bogeyman and a coalition if:
            //  (a) it considers the bogeyman equivalent to any of the coalition-membmers, or
            //  (b) There exists a smaller coalition consisting solely of candidates one prefers to those of the coalition which beats the bogeyman.
            // Oterwise, the ballot supports the coalition over the bogeyman.
            private SupportResult GetSupportForCoalition(RankedBallot ballot, ulong coalition, int bogeyman)
            {
                var ranking = ballot.Ranking;
                var bogeymanTier = ranking.IndexesWhere(tier => tier.Contains(bogeyman)).Single();

                // Detract support for any coalition containing someone we like less than the bogeyman.
                if ((GetCoalition(ranking.Skip(bogeymanTier + 1).SelectMany(tier => tier)) & coalition) > 0ul)
                    return SupportResult.Detractor;

                // Remain neutral on any coalition containing someone we like the same as the bogeyman.
                if ((GetCoalition(ranking[bogeymanTier]) & coalition) > 0ul)
                    return SupportResult.Neutral;

                var preferredCoalition = 0ul;
                foreach (var tier in ranking.Take(bogeymanTier))
                {
                    // Support each coalition which is a subset of our preferred candidates.
                    preferredCoalition |= GetCoalition(tier);
                    if ((preferredCoalition | coalition) == preferredCoalition)
                        return SupportResult.Supporter;

                    // Remain neutral on the coalition if there exists a sub-coalition consisting of only preferred candidates which will work as well
                    var beats = Beats(preferredCoalition, bogeyman);
                    if (beats == true)
                        return SupportResult.Neutral;
                    if (beats == null)
                        return SupportResult.ConditionalSupporter;
                }

                throw new InvalidOperationException("There is a bug: We ought to have found a coalition which is a subset by now.");
            }

            public void AddDetails(ElectionResults results)
            {
                results.AddHeading("Beat Matrix");
                results.AddTable(m_beatMatrix.SelectMany((d, i) => d.Select(kvp => new ElectionResults.Value[] {
                    (ElectionResults.Candidate) i,
                    kvp.Key,
                    kvp.Value,
                })),
                    "Cand.",
                    "Coa.",
                    "Support"
                    );
            }

            private enum SupportResult
            {
                Neutral,
                Detractor,
                Supporter,
                ConditionalSupporter
            }

 
            private readonly CandidateComparerCollection<RankedBallot> m_ballots;
            private readonly Dictionary<ulong, int?>[] m_beatMatrix;
            private HashSet<ulong> m_calculatingCoalitions = new HashSet<ulong>();
        }

        private static IEnumerable<ulong> GetAllSubCoalitions(ulong coalition)
        {
            yield return coalition;
            foreach (var subCoalition in GetImmediateSubCoalitions(coalition).SelectMany(GetAllSubCoalitions))
                yield return subCoalition;
        }

        private static IEnumerable<ulong> GetImmediateSubCoalitions(ulong coalition)
        {
            for (var mask = 1ul; mask < coalition; mask = mask << 1)
            {
                if ((mask & coalition) > 0)
                    yield return mask ^ coalition;
            }
        }
    }
}