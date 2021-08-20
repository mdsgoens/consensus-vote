using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public abstract class ConsensusVoteBase : VotingMethodBase<RankedBallot>
    {
        public override RankedBallot GetHonestBallot(Voter v) => new RankedBallot(v.CandidateCount, v.Ranking);

        public override List<List<int>> GetRanking(CandidateComparerCollection<RankedBallot> ballots)
        {
            return GetDetailedTally(ballots).Ranking;
        }

        public abstract (List<List<int>> Ranking, int[] ApprovalCount, int[] FirstChoices, IEnumerable<(Compromise Compromise, int Count)> Compromises) GetDetailedTally(CandidateComparerCollection<RankedBallot> ballots);
        
        public record Compromise(int FirstChoice, int CompromiseChoice, int Bogeyman);
    }
}