using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public abstract class ConsensusVoteBase : VotingMethodBase<RankedBallot>
    {
        public override RankedBallot GetHonestBallot(Voter v) => new RankedBallot(v.CandidateCount, v.Ranking);

        public override IEnumerable<RankedBallot> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
            => GetHonestBallot(v).GetPotentialStrategicBallots(ranking[0]).Where(a => a.Strategy != RankedBallot.Strategy.Honest).Select(a => a.AlternateBallot);

        // Coalitions are encoded as bitmasks for quick comparisons.
        public static ulong GetCoalition(IEnumerable<int> candidates) => candidates.Aggregate(0ul, (l, c) => l | GetCoalition(c));
        public static ulong GetCoalition(int candidate) => 1ul << candidate;
    }
}