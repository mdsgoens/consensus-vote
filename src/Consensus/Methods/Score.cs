using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Score : VotingMethodBase<ScoreBallot, VotingMethodBase.Tally>
    {
        public override ScoreBallot GetHonestBallot(Voter v)
        {
            // Evenly distributes candidates along utility scale
            var min = v.Utilities.Min();
            var max = v.Utilities.Max();
            double scale = c_scale / (double) (max - min);  
            return new ScoreBallot(v.Utilities.Select(u => u > min ? (int) Math.Ceiling((u - min) * scale) : 1));
        }

        public override ScoreBallot GetStrategicBallot(VotingMethodBase.Tally tally, Voter v)
        {
            // Approves of all candidates they like better or equal to their favorite in the top two
            var threshold = tally.RunnersUp.Select(r => v.Utilities[r])
                .Append(v.Utilities[tally.Winner])
                .Max();

            return new ScoreBallot(v.Utilities.Select(u => u >= threshold ? c_scale : 1));
        }

        public override VotingMethodBase.Tally GetTally(CandidateComparerCollection<ScoreBallot> ballots)
        {
            // Choose the top scorer
            var sortedCandidates = ScoreBallot.SortCandidates(ballots);
            var first = sortedCandidates[0].Candidate;
            var second = sortedCandidates[1].Candidate;

            return new Tally(first, new [] { second });
        }

        const int c_scale = 5;
    }
}