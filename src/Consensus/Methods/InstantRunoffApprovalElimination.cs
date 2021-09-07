using System.Linq;
using System.Collections.Generic;
using Consensus.Ballots;
using System;

namespace Consensus.Methods
{
    public sealed class InstantRunoffApprovalElimination : VotingMethodBase<RankedBallot>
    {
        public override RankedBallot GetHonestBallot(Voter v)
        {
            var threshold = v.Utilities.Average();
            return new RankedBallot(v.CandidateCount, v.Ranking
                .TakeWhile(x => v.Utilities[x[0]] >= threshold));
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<RankedBallot> ballots)
        {
            HashSet<int> eliminatedCandidates = new HashSet<int>();
            List<List<int>> eliminationOrder = new List<List<int>>();
            while (true)
            {
                var votesByCandidate = GetCount();

                var totalVotes = votesByCandidate.Sum();
                var maximumVotes = votesByCandidate.Max();

                if (maximumVotes * 2 >= totalVotes)
                {
                    eliminationOrder.Reverse();

                    return new ElectionResults(votesByCandidate
                        .Select((v, i) => (Votes: v, Candidate: i))
                        .Where(a => !eliminatedCandidates.Contains(a.Candidate))
                        .GroupBy(a => a.Votes, a => a.Candidate)
                        .OrderByDescending(gp => gp.Key)
                        .Select(gp => gp.ToList())
                        .Concat(eliminationOrder)
                        .ToList());
                }

                // TODO: Some sort of tiebreaker.
                var minimumVotes = votesByCandidate.Where((_, c) => !eliminatedCandidates.Contains(c)).Min();
                var last = votesByCandidate.IndexesWhere(v => v == minimumVotes).First(c => !eliminatedCandidates.Contains(c));

                eliminatedCandidates.Add(last);
                eliminationOrder.Add(new List<int>{ last });
            }
            
            double[] GetCount()
            {
                var votesByCandidate = new double[ballots.CandidateCount];

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    // Cast one's ballot for the highest-ranked candidate which is neither eliminated nor ranked last.
                    // Ties of `n` candidates are counted as if there were `1/n`th of the vote listed that canidate first
                    var candidates = ballot.Ranking
                        .Select(tier => tier.Where(c => !eliminatedCandidates.Contains(c)).ToList())
                        .Where(tier => tier.Any())
                        .FirstOrDefault();

                    if (candidates != null)
                    {
                        foreach (var c in candidates)
                            votesByCandidate[c] += count / (double) candidates.Count;
                    }
                }

                return votesByCandidate;
            }
        }
        
        public override IEnumerable<(string, int, RankedBallot)> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            // "Favorite Betrayal": sort those preferred to winner by reverse elimination order, not utility order. Tiebreak by utility, at least!
            // This will hopefully eliminate less-preferred candidates sooner and help more-preferred candidates survive the runoffs
            var winnerEv = ranking[0].Min(w => v.Utilities[w]);
            var max = v.Utilities.Max();
            
            if (winnerEv != max)
            {
                yield return ("Pre-Winner Truncation", 1, new RankedBallot(v.CandidateCount, v.Ranking
                    .TakeWhile(x => v.Utilities[x[0]] > winnerEv)));
            }

            yield return ("Post-Winner Truncation", 1, new RankedBallot(v.CandidateCount, v.Ranking
                .TakeWhile(x => v.Utilities[x[0]] >= winnerEv)));

            if (v.Utilities.Count(u => u > winnerEv) < 2)
                yield break;

            var candidates = v.Utilities
                .Select((u, c) => (
                    Candidate: c,
                    Utility: u,
                    Rank: ranking.FindIndex(tier => tier.Contains(c))))
                .ToLookup(a => a.Utility > winnerEv);

            yield return ("Favorite Betrayal", 1, new RankedBallot(v.CandidateCount, 
                candidates[true]
                    .GroupBy(c => c.Rank)
                    .OrderByDescending(gp => gp.Key)
                .Concat(
                    candidates[false]
                    .GroupBy(c => c.Utility)
                    .OrderByDescending(gp => gp.Key)
                )
                .Select(gp => gp.Select(a => a.Candidate).ToList())
                .ToList()));
        }
    }
}