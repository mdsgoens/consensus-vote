using System.Linq;
using System.Collections.Generic;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class InstantRunoff : VotingMethodBase<RankedBallot, InstantRunoff.RankedTally>
    {
        public override RankedBallot GetHonestBallot(Voter v){
            return new RankedBallot(v.Utilities.Count, v.Ranking.SelectMany(x => x).Select(x => new [] { x }));
        }

        public override InstantRunoff.RankedTally GetTally(CandidateComparerCollection<RankedBallot> ballots)
        {
            HashSet<int> eliminatedCandidates = new HashSet<int>();
            List<int> eliminationOrder = new List<int>();
            while (true)
            {
                var count = GetCount();

                var totalVotes = count.Sum(c => c.Value);

                var sortedIndices = count.OrderByDescending(a => a.Value).ToList();

                if (sortedIndices[0].Value * 2 >= totalVotes)
                {
                    return new RankedTally(
                        sortedIndices[0].Key,
                        sortedIndices.Skip(1).TakeWhile(i => i.Value == sortedIndices[1].Value).Select(i => i.Key).ToArray(),
                        eliminationOrder
                    );
                }

                var last = sortedIndices.Last().Key;
                eliminatedCandidates.Add(last);
                eliminationOrder.Add(last);
            }
            
            Dictionary<int, int> GetCount()
            {
                var count = new Dictionary<int, int>();
                foreach (var ballot in ballots)
                {
                    var candidate = ballot.CandidateOrder
                        .Cast<int?>()
                        .FirstOrDefault(c => !eliminatedCandidates.Contains(c.Value));

                    if (candidate.HasValue)
                    {
                        if (!count.ContainsKey(candidate.Value))
                            count[candidate.Value] = 0;
                        count[candidate.Value]++;
                    }
                }
                return count;
            }
        }
        
        public override RankedBallot GetStrategicBallot(RankedTally tally, Voter v)
        {
            var candidateRanks = v.Ranking.SelectMany(x => x).ToList();
            
            var eliminatedPreferences = tally.EliminationOrder
                .Where(r => v.Prefers(r, tally.Winner))
                .ToList();

            if (eliminatedPreferences.Any())
            {
                // Sort the candidates we prefer to the winner in order of elimination, not order of preference,
                // In hopes that this eliminates the winner earlier.
                // Rankings after the winner do not matter.
                // TODO: Maybe something smarter involving the beat matrix?
               int rank = 0;
               foreach (var c in eliminatedPreferences)
               {
                   SetRank(c, rank);
                   rank++;
               }
            }
            else if (!v.Ranking[0].Contains(tally.Winner))
            {
                // Protect the winner by ranking her first.
                SetRank(tally.Winner, 0);
            }
          
            return new RankedBallot(v.Utilities.Count, candidateRanks.Select(x => new [] { x }));

            void SetRank(int targetCandidate, int targetRank)
            {
                candidateRanks.Remove(targetCandidate);
                candidateRanks.Insert(targetRank, targetCandidate);
            }
        }

        public record RankedTally(int Winner, int[] RunnersUp, List<int> EliminationOrder) : VotingMethodBase.Tally(Winner, RunnersUp);
    }
}