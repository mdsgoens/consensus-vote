using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ConsensusVoteTests : VotingMethodTestBase<ConsensusVote, RankedBallot, VotingMethodBase.Tally>
    {
        [TestCase("b", "b a")]
        [TestCase("ab", "ab")]
        [TestCase("a b c", "a b c")]
        public void HonestBallot(string voter, string expectedBallot)
            => HonestBallotCore(voter, expectedBallot);

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
        public void Tally(string ballots, char expectedWinner)
            => TallyCore(ballots, expectedWinner);

        [Test]
        public void StrategicBallot()
        {
            //TODO
            Assert.Ignore("TODO");
        }
    }
}