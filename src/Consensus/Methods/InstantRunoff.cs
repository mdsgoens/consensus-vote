using System.Linq;
using System.Collections.Generic;
using Consensus.Ballots;
using System;

namespace Consensus.Methods
{
    public sealed class InstantRunoff : VotingMethodBase<RankedBallot>
    {
        public override RankedBallot GetHonestBallot(Voter v)
        {
            return new RankedBallot(v.CandidateCount, v.Ranking.SelectMany(x => x).Select(x => new [] { x }));
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
            
            int[] GetCount()
            {
                var votesByCandidate = new int[ballots.CandidateCount];

                foreach (var (ballot, count) in ballots.Comparers)
                {
                    // Cast one's ballot for the highest-ranked candidate which is neither eliminated nor ranked last.
                    // NOTE, no tiebreakers here.
                    var candidate = ballot.Ranking
                        .Take(ballot.Ranking.Count - 1)
                        .SelectMany(tier => tier)
                        .Where(c => !eliminatedCandidates.Contains(c))
                        .Cast<int?>()
                        .FirstOrDefault();

                    if (candidate.HasValue)
                        votesByCandidate[candidate.Value] += count;
                }
                return votesByCandidate;
            }
        }
        
        public override IEnumerable<RankedBallot> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            // "Favorite Betrayal": sort those preferred to winner by reverse elimination order, not utility order. Tiebreak by utility, at least!
            // This will hopefully eliminate less-preferred candidates sooner and help more-preferred candidates survive the runoffs
            var winnerEv = ranking[0].Min(w => v.Utilities[w]);

            if (v.Utilities.Count(u => u > winnerEv) < 2)
                yield break;

            var candidates = v.Utilities
                .Select((u, c) => (
                    Candidate: c,
                    Utility: u,
                    Rank: ranking.FindIndex(tier => tier.Contains(c))))
                .ToLookup(a => a.Utility > winnerEv);

            yield return new RankedBallot(v.CandidateCount, 
                candidates[true]
                    .GroupBy(c => c.Rank)
                    .OrderBy(gp => gp.Key)
                .Concat(
                    candidates[false]
                    .GroupBy(c => c.Utility)
                    .OrderByDescending(gp => gp.Key)
                )
                .Select(gp => gp.Select(a => a.Candidate).ToList())
                .ToList());
        }
    }
}