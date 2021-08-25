using System.Collections.Generic;
using System.Linq;
using Consensus.Ballots;

namespace Consensus.Methods
{
    // Voting method wherein one casts an Approval ballot for zero or one candidates.
    public sealed class Plurality : VotingMethodBase<ApprovalBallot>
    {
        // Vote for one's first preference
        public override ApprovalBallot GetHonestBallot(Voter v) => new ApprovalBallot(v.CandidateCount, v.FirstPreference);

        // Vote for the candidate with utility greater than the polling EV who has the best chance to win
        public override ApprovalBallot GetStrategicBallot(Polling polling, Voter v)
        {
            var pollingEv = polling.EV(v);
            var bestCandidate = v.FirstPreference;
            var bestCandidateEv = (v.Utilities[bestCandidate] - pollingEv) * polling.VictoryChanceByCandidate(bestCandidate);

            for (int i = 0; i < v.CandidateCount; i++)
            {
                if (v.Utilities[i] > pollingEv && polling.VictoryChanceByCandidate(i) > 0)
                {
                    var candidateEv = (v.Utilities[i] - pollingEv) * polling.VictoryChanceByCandidate(i);
                    if (candidateEv > bestCandidateEv)
                    {
                        bestCandidate = i;
                        bestCandidateEv = candidateEv;
                    }
                }
            }

            return new ApprovalBallot(v.CandidateCount, bestCandidate);
        }

        public override ElectionResults GetElectionResults(CandidateComparerCollection<ApprovalBallot> ballots)
        {
            return ScoreBallot.GetElectionResults(ballots);
        }
    }
}