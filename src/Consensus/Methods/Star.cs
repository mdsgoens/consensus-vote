using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Star : VotingMethodBase<ScoreBallot, VotingMethodBase.Tally>
    {
        public override ScoreBallot GetHonestBallot(Voter v)
        {
            // Evenly distributes candidates along utility scale
            var min = v.Utilities.Min();
            var max = v.Utilities.Max();
            double scale = c_scale / (max - min);
            return new ScoreBallot(v.Utilities.Select(u => (int) Math.Ceiling((u - min) * scale)));
        }

        public override ScoreBallot GetStrategicBallot(VotingMethodBase.Tally tally, Voter v)
        {
            // Approves of all candidates they like better or equal to their favorite in the top two
            // If more than one candidate meets that criterion, notch the least-favorite down a peg
            var threshold = tally.RunnersUp.Select(r => v.Utilities[r])
                .Append(v.Utilities[tally.Winner])
                .Max();

            var approvalCount = v.Utilities.Count(u => u >= threshold);

            return new ScoreBallot(v.Utilities
                .Select(u => u > threshold || (u == threshold && approvalCount > 1) ? c_scale 
                    : u == threshold ? c_scale - 1
                    : 1));
        }

        public override VotingMethodBase.Tally GetTally(CandidateComparerCollection<ScoreBallot> ballots)
        {
            // Choose the top two scorers
            var sortedCandidates = ScoreBallot.SortCandidates(ballots);
            var first = sortedCandidates[0].Candidate;
            var second = sortedCandidates[1].Candidate;

            // Then compare those two head-to-head
            var firstCount = 0;
            var secondCount = 0;
            foreach (var ballot in ballots)
            {
                if (ballot.Prefers(first, second))
                    firstCount++;
                else if (ballot.Prefers(second, first))
                    secondCount++;
            }

            return firstCount > secondCount
                ? new Tally(first, new [] { second })
                : new Tally(second, new [] { first });
        }

        const int c_scale = 5;
    }
}