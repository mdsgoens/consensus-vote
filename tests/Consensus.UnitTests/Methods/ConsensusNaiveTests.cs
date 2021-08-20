using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ConsensusNaiveTests : VotingMethodTestBase<ConsensusNaive, RankedBallot>
    {
        [TestCase("b", "b a")]
        [TestCase("ab", "ab")]
        [TestCase("a b c", "a b c")]
        
        public void HonestBallot(string voter, string expectedBallot) => HonestBallotCore(voter, expectedBallot);

         [Test]
        public void SpolerEffectWhenLeaderTruncates()
        {
            // One would expect the supporters of a and b to form a coalition against c, but they don't -- 
            // they are both so confident that with the other's support they'd win that they don't bother to support the other.
            // This is a spoiler effect, and thus is fatal.
            // NOTE: This *does* require all `c` supporters rank b and a *equally*, not just be split between them
            // and indicates that this algorithm is vulnerable to a "truncation" strategy by `c`'s supporters
            TallyCore("a b c * 31; b a c * 32; c ba * 37", 'c');
        }

        [TestCase("a b", 'a')]
        [TestCase(@"
            a b
            b a * 2",
            'b')]
        [TestCase(@"
            a b c * 2
            b a c * 2
            c b a",
            'b')]
        public void Tally(string ballots, char expectedWinner) => TallyCore(ballots, expectedWinner);

        [Test]
        public void StrategicBallot()
        {
            //TODO
            Assert.Ignore("TODO");
        }
    }
}