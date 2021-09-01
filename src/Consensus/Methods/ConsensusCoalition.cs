using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    [Obsolete("Just to complicated to live")]
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
                    .Where(b => beatMatrix.Beats(coalition, b))
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
                    .Select(_ => new Dictionary<ulong, int>())
                    .ToArray();
                m_cycles = Enumerable.Range(0, ballots.CandidateCount)
                    .Select(_ => new List<HashSet<ulong>>())
                    .ToArray();
            }

            public bool Beats(ulong coalition, int bogeyman)
            {
                if (coalition == 0ul || (coalition & GetCoalition(bogeyman)) > 0ul)
                    throw new InvalidOperationException("There's a bug: We should never ask if a candidate can beat themselves.");

                // Try the cache first.
                if (m_beatMatrix[bogeyman].TryGetValue(coalition, out var value))
                    return value > 0;

                // TODO: Ensure there are no infintie loops, or results which depend on the order of ballots, or whatnot.
                // If any of the "calculating" coalitions form a cycle with the current coalition, return `false` for the purposes of determining if the
                // calculating coalition can rely on the fact that the current coalition can beat the bogeyman -- because it cannot.
                // Add the cycle to the list, so we'll *also* return `false` all the way back up to the cycle initiator.
                HashSet<ulong> cycle = null;
                foreach (var calculating in m_calculatingCoalitions)
                {
                    if (cycle != null)
                    {
                        cycle.Add(calculating);
                    }
                    else if (calculating == coalition)
                    {
                        cycle = new HashSet<ulong> { calculating };
                    }
                    else if (m_cycles[bogeyman].Any(c => c.Contains(calculating) && c.Contains(coalition)))
                    {
                        return false;
                    }
                }

                if (cycle != null)
                {
                    m_cycles[bogeyman].Add(cycle);
                    return false;
                }

                m_calculatingCoalitions.Push(coalition);
            
                // A coalition beats a candidate if any sub-coalition beats that candidate.
                // This ensures we can just check "maximal" coalitions and be ensured we're not thrown off by spoiler candidates
                foreach (var subCoalition in GetImmediateSubCoalitions(coalition))
                {
                    if (Beats(subCoalition, bogeyman))
                        return (m_beatMatrix[bogeyman][coalition] = m_beatMatrix[bogeyman][subCoalition]) > 0; 
                }

                var support = m_ballots.Comparers.Sum(ballot => GetSupportForCoalition(ballot, coalition, bogeyman));

                // Prefer the support value calculated after we realized there was a cycle.
                if (!m_beatMatrix[bogeyman].ContainsKey(coalition))
                    m_beatMatrix[bogeyman][coalition] = support;

                m_calculatingCoalitions.Pop();

                return m_beatMatrix[bogeyman][coalition] > 0;
            }

            // A ballot supports a bogeyman over a coalition if there are any candidates in the coalition one likes worse than the bogeyman.
            // A ballot remains neutral between a bogeyman and a coalition if:
            //  (a) it considers the bogeyman equivalent to any of the coalition-membmers, or
            //  (b) There exists a smaller coalition consisting solely of candidates one prefers to those of the coalition which beats the bogeyman.
            // Oterwise, the ballot supports the coalition over the bogeyman.
            private int GetSupportForCoalition(RankedBallot ballot, ulong coalition, int bogeyman)
            {
                var ranking = ballot.Ranking;
                var bogeymanTier = ranking.IndexesWhere(tier => tier.Contains(bogeyman)).Single();

                // Detract support for any coalition containing someone we like less than the bogeyman.
                if ((GetCoalition(ranking.Skip(bogeymanTier + 1).SelectMany(tier => tier)) & coalition) > 0ul)
                    return -1;

                // Remain neutral on any coalition containing someone we like the same as the bogeyman.
                if ((GetCoalition(ranking[bogeymanTier]) & coalition) > 0ul)
                    return 0;

                var preferredCoalition = 0ul;
                foreach (var tier in ranking.Take(bogeymanTier))
                {
                    // Support each coalition which is a subset of our preferred candidates.
                    preferredCoalition |= GetCoalition(tier);
                    if ((preferredCoalition | coalition) == preferredCoalition)
                        return 1;

                    // Remain neutral on the coalition if there exists a sub-coalition (without cycles) consisting of only preferred candidates which will work as well             
                    if (Beats(preferredCoalition, bogeyman))
                        return 0;
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
 
            private readonly CandidateComparerCollection<RankedBallot> m_ballots;
            private readonly Dictionary<ulong, int>[] m_beatMatrix;
            private readonly List<HashSet<ulong>>[] m_cycles;
            private Stack<ulong> m_calculatingCoalitions = new Stack<ulong>();
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