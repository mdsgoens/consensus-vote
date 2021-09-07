using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.Ballots
{
    public sealed class ApprovalBallot : ScoreBallot
    {
        public ApprovalBallot(IReadOnlyList<bool> approvalsByCanidate)
            : base(approvalsByCanidate.SelectToArray(i => i ? 1 : 0))
        {}
        
        public ApprovalBallot(int candidateCount, int approveOfCanidate)
            : base(candidateCount.LengthArray(i => i == approveOfCanidate ? 1 : 0))
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