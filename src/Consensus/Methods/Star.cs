using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    public sealed class Star : VotingMethodBase<ScoreBallot>
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
        // If two "viable" candidates both meet that condition, knock the least-preferred down one peg.
        // If two "viable" candidates both fail that condition, bump the most-preferred up one peg.
        public override ScoreBallot GetStrategicBallot(Polling polling, Voter v)
        {
            var ev = polling.EV(v);

            var scores = v.Utilities
                .Select(u => u >= ev ? c_scale : 1)
                .ToArray();

            var hasBumpedUp = false;
            var hasBumpedDown = false;
            foreach (var (first, second) in polling.ProbableTopTwos)
            {
                var firstUtility = v.Utilities[first];
                var secondUtility = v.Utilities[second];

                if (firstUtility == secondUtility)
                    continue;

                if (!hasBumpedUp && firstUtility < ev && secondUtility < ev)
                {
                    hasBumpedUp = true;
                    var higherUtility = firstUtility < secondUtility ? secondUtility : firstUtility;
                    for(var i = 0; i < v.CandidateCount; i++)
                    {
                        if (v.Utilities[i] == higherUtility)
                            scores[i] = 2;
                    }
                }

                if (!hasBumpedDown && firstUtility >= ev && secondUtility >= ev)
                {
                    hasBumpedDown = true;
                    var lowerUtility = firstUtility > secondUtility ? secondUtility : firstUtility;
                    for(var i = 0; i < v.CandidateCount; i++)
                    {
                        if (v.Utilities[i] == lowerUtility)
                            scores[i] = c_scale - 1;
                    }
                }

                if (hasBumpedUp && hasBumpedDown)
                    break;
            }
            
            return new ScoreBallot(scores);
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ScoreBallot> ballots)
        {
            // Choose the top two scorers
            // NOTE: No tiebreaking
            var sortedCandidates = ScoreBallot.GetElectionResults(ballots).Ranking;

            int Pop()
            {
                var value = sortedCandidates[0][0];

                sortedCandidates[0].RemoveAt(0);
                if (sortedCandidates[0].Count == 0)
                    sortedCandidates.RemoveAt(0);

                return value;
            }

            var first = Pop();
            var second = Pop();

            // Then compare those two head-to-head
            var compare = ballots.Compare(first, second);

            if (compare == 0)
            {
                sortedCandidates.Insert(0, new List<int> { first, second });
            }
            else if (compare < 0)
            {
                sortedCandidates.Insert(0, new List<int> { first });
                sortedCandidates.Insert(0, new List<int> { second });
            }
            else
            {
                sortedCandidates.Insert(0, new List<int> { second });
                sortedCandidates.Insert(0, new List<int> { first });
            }

            return new ElectionResults(sortedCandidates);
        }

        const int c_scale = 5;
    }
}