using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class ConsensusBeats : ConsensusVoteBase
    {
        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
        {
            var beatMatrix = ballots.GetBeatMatrix();

            var candidates = Enumerable.Range(0, ballots.CandidateCount);
            var approvalCount = new int[ballots.CandidateCount];
            var firstChoices = new int[ballots.CandidateCount];
            var compromises = new CountedList<(ulong Preferred, int Compromise, ulong Bogeymen)>();
            
            foreach (var (ballot, count) in ballots.Comparers)
            {
                var ranking = ballot.Ranking;
        
                // Approve of the each candidate `c` which is a first choice.
                foreach (var c in ranking[0])
                {
                    firstChoices[c] += count;
                    approvalCount[c] += count;
                }

                // Approve of the each candidate `c` such that
                // For each candidate `a` which one prefers to `c`,
                // There exists a candidate `b` (the 'bogeyman') such that one prefers `c` to `b` and `b` beats `a` one-on-one
                if (ranking.Count >= 3)
                {
                    var preferredCandidates = ranking[0].ToList();
                    var potentialBogeymen = Enumerable.Range(0, ballots.CandidateCount).ToHashSet();
                    potentialBogeymen.ExceptWith(preferredCandidates);

                    foreach (var tier in ranking.Skip(1).Take(ranking.Count - 2))
                    {
                        potentialBogeymen.ExceptWith(tier);

                        if (preferredCandidates.All(a => potentialBogeymen.Any(b => beatMatrix.Beats(b, a))))
                        {
                            var preferredCoalition = GetCoalition(preferredCandidates);
                            var bogeymen = GetCoalition(potentialBogeymen.Where(b => preferredCandidates.Any(a => beatMatrix.Beats(b, a))));

                            foreach (var c in tier)
                            {
                                approvalCount[c] += count;
                                compromises.Add((preferredCoalition, c, bogeymen), count);
                            }
                        }

                        preferredCandidates.AddRange(tier);
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