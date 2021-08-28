using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ConsensusNaiveTests : ConsensusVoteTestsBase<ConsensusBeats>
    {
         [Test]
        public void SpolerEffectWhenLeaderTruncates()
        {
            // One would expect the supporters of a and b to form a coalition against c, but they don't -- 
            // they are both so confident that with the other's support they'd win that they don't bother to support the other.
            // This is a spoiler effect, and thus is fatal.
            // NOTE: This *does* require all `c` supporters rank b and a *equally*, not just be split between them
            // and indicates that this algorithm is vulnerable to a "truncation" strategy by `c`'s supporters
            TallyCore("a b c * 31; b a c * 32; c ba * 37", "c");
        }
    }
}