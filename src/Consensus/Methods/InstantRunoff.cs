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

        public override List<List<int>> GetRanking(CandidateComparerCollection<RankedBallot> ballots)
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

                    return votesByCandidate
                        .Select((v, i) => (Votes: v, Candidate: i))
                        .Where(a => !eliminatedCandidates.Contains(a.Candidate))
                        .GroupBy(a => a.Votes, a => a.Candidate)
                        .OrderByDescending(gp => gp.Key)
                        .Select(gp => gp.ToList())
                        .Concat(eliminationOrder)
                        .ToList();
                }

                // TODO: Some sort of tiebreaker.
                var minimumVotes = votesByCandidate.Min();
                var last = votesByCandidate.IndexesWhere(i => i == minimumVotes).First();

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
                    var candidate = ballot.RanksByCandidate
                        .Select((r, i) => (Rank: r, Candidate: i))
                        .Where(c => !eliminatedCandidates.Contains(c.Candidate) && c.Rank != ballot.LastRank)
                        .OrderByDescending(a => a.Rank)
                        .Select(a => a.Candidate)
                        .Cast<int?>()
                        .First();

                    if (candidate.HasValue)
                        votesByCandidate[candidate.Value] += count;
                }
                return votesByCandidate;
            }
        }
        
        public override RankedBallot GetStrategicBallot(Polling polling, Voter v)
        {
            // "Favorite Betrayal": sort by marginal EV over the status quo, not utility order. Tiebreak by utility, at least!
            // This will hopefully eliminate less-preferred candidates sooner and help more-preferred candidates survive the runoffs
            var overallEv = polling.EV(v);

            return new RankedBallot(v.CandidateCount, v.Utilities
                .Select((u, c) => (
                    Candidate: c,
                    Utility: u,
                    EV: polling.VictoryChanceByCandidate(c) * (u - overallEv)))
                .GroupBy(a => (a.EV, a.Utility), a => a.Candidate)
                .OrderByDescending(gp => gp.Key.EV)
                .ThenByDescending(gp => gp.Key.Utility)
                .Select(gp => gp.ToList()));
        }
    }
}