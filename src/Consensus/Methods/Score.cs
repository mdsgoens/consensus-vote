using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Score : VotingMethodBase<ScoreBallot>
    {
        // Evenly distributes candidates along utility scale
        public override ScoreBallot GetHonestBallot(Voter v)
        {
            var min = v.Utilities.Min();
            var max = v.Utilities.Max();
            double scale = c_scale / (double) (max - min);  
            return new ScoreBallot(v.Utilities.Select(u => u > min ? (int) Math.Ceiling((u - min) * scale) : 1));
        }

        // Approves of all candidates they like better or equal to the polling EV
        public override ScoreBallot GetStrategicBallot(Polling polling, Voter v)
        {
            var ev = polling.EV(v);
            return new ScoreBallot(v.Utilities.Select(u => u >= ev ? c_scale : 1));
        }

        public override List<List<int>> GetRanking(CandidateComparerCollection<ScoreBallot> ballots)
        {
            return ScoreBallot.GetRanking(ballots);
        }

        const int c_scale = 5;
    }
}