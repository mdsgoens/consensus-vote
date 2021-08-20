using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusCoalition : ConsensusVoteBase
    {
        public override (List<List<int>> Ranking, int[] ApprovalCount, int[] FirstChoices, IEnumerable<(Compromise Compromise, int Count)> Compromises) GetDetailedTally(CandidateComparerCollection<RankedBallot> ballots)
        {
            // first, construct the "coalition" beat matrix.
            var beatMatrix = new CoalitionBeatMatrix(ballots);

            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromises = new CountedList<Compromise>();
            
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

                    var compromiseCandidates = new List<int>();
                    foreach (var tier in ranking.Skip(1).Take(ranking.Count - 2))
                    {
                        var greaterBogeymen = potentialBogeymen.Where(b => !tier.Contains(b)).ToList();

                        // If this tier contains all the bogeymen, the previous tier is where our approval stops.
                        if (!greaterBogeymen.Any())
                            break;

                        // Otherwise, enlist the help of the lesser bogeymen to beat the greater bogeymen.
                        coalition |= GetCoalition(tier);
                        compromiseCandidates.AddRange(tier);
                          
                        var remainingBogeymen = greaterBogeymen
                            .Where(b => !beatMatrix.Beats(coalition, b))
                            .ToList();

                        // Huzzah! We have beaten all the bogeymen!
                        if (!remainingBogeymen.Any())
                            break;

                        // Otherwise, check the next teir for potential coalition-mates.
                        potentialBogeymen = remainingBogeymen;
                    }

                    var bogeyman = potentialBogeymen.First();

                    foreach(var c in compromiseCandidates)
                    {
                        approvalCount[c] += count;
                        compromises.Add(new Compromise(ranking[0].First(), c, bogeyman), count);
                    }
                }
            }
            
            return
            (
                approvalCount.IndexRanking(),
                approvalCount,
                firstChoices,
                compromises
            );
        }

        // Coalitions are encoded as bitmasks for quick comparisons.
        private static ulong GetCoalition(IEnumerable<int> candidates) => candidates.Aggregate(0ul, (l, c) => l | GetCoalition(c));
        private static ulong GetCoalition(int candidate) => 1ul << candidate;

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
                    var dispreferredCoalition = 1ul << m_beatMatrix.Length - 1;
                    var dispreferredCandidateSet = Enumerable.Range(0, m_beatMatrix.Length).ToHashSet();
                    var ranking = ballot.GetRanking();

                    foreach (var tier in ranking.Take(ranking.Count - 1))
                    {
                        var teirCoalition = GetCoalition(tier);
                        preferredCoalition |= teirCoalition;
                        dispreferredCoalition ^= teirCoalition;

                        dispreferredCandidateSet.ExceptWith(tier);

                        // Count this ballot once per (coalition, candidate) pair for each coalition and set of candidates one prefers that coalition over.
                        foreach (var candidate in dispreferredCandidateSet)
                            m_coalitionSupporters[candidate].Add(preferredCoalition, count);

                        // Count this ballot once per candidate for the largest coalition one preferrs that candidate over.
                        foreach (var candidate in tier)
                            m_coalitionDetractors[candidate].Add(dispreferredCoalition, count);
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
                // the number of people who:
                //   (1) support **every** member of coalition `a` over candidate `b`, and
                //   (2) rank no candidates `c` not in the coalition higher than any member of the coalition `a`,
                // is greater than the number of people who support candidate `b` over **any** member of coalition `a`.
                m_coalitionSupporters[candidate].TryGetCount(coalition, out var support);

                foreach (var (detractorCoalition, count) in m_coalitionDetractors[candidate])
                {
                    // There is at least one candidate in `coalition` and also in `detractorCoalition`
                    if ((coalition & detractorCoalition) > 0)
                        support -= count;
                }

                return m_beatMatrix[candidate][coalition] = support > 0;
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