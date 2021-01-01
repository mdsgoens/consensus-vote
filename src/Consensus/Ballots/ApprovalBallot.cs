using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Ballots
{
    public sealed class ApprovalBallot : ScoreBallot
    {
        public ApprovalBallot(IEnumerable<bool> approvalsByCanidate)
            : base(approvalsByCanidate.Select(i => i ? 1 : 0).ToArray())
        {}
        
        public ApprovalBallot(int numberOfCandidates, int approveOfCanidate)
            : base(Enumerable.Range(0, numberOfCandidates).Select(i => i == approveOfCanidate ? 1 : 0).ToArray())
        {}

        public static new ApprovalBallot Parse(int candidateCount, string source)
        {
            var approvalsByCanidate = new bool[candidateCount];
            foreach (var c in source)
                approvalsByCanidate[ParsingUtility.DecodeCandidateIndex(c)] = true;
            
            return new ApprovalBallot(approvalsByCanidate);
        }

        public override string ToString() => ParsingUtility.EncodeCandidates(CandidateScores.Select(a => a.Candidate));
    }
}