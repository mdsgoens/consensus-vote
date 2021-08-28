using NUnit.Framework;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    public class ConsensusRoundsMinimalTests : ConsensusVoteTestsBase<ConsensusRoundsMinimalChange>
    {
        [Test]
        public void TestDecidingVote()
        {
            // I would expect that the CAB voter would decide between A and B.
            TallyCore("a b c * 31; b a c * 32; c ba * 36; c a b", "a");
        }
    }
}