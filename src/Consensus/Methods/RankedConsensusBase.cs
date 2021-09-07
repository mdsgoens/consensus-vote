using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public abstract class RankedConsensusBase : VotingMethodBase<RankedBallot>
    {

        public override RankedBallot GetHonestBallot(Voter v)
        {
            return new RankedBallot(v.CandidateCount, v.Ranking);
        } 

        public override IEnumerable<(string, int, RankedBallot)> GetPotentialStrategicBallots(List<List<int>> ranking, Voter v)
        {
            return GetHonestBallot(v).GetPotentialStrategicBallots(ranking);
        }
    }
}