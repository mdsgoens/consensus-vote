using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class StarTests : VotingMethodTestBase<Star, ScoreBallot, VotingMethodBase.Tally>
    {
        [TestCase("b", "5:b 1:a")]
        [TestCase("ab", "1:ab")]
        [TestCase("a b", "5:a 1:b")]
        [TestCase("100a 80b 50cd", "5:a 3:b 1:cd")]
        public void HonestBallot(string voter, string expectedBallot)
            => HonestBallotCore(voter, expectedBallot);

        [TestCase(@"
            5:a * 2
            5:b",
            'a')]
        [TestCase(@"
            5:a 3:bd 1:ce * 4
            5:b 3:cd 1:ae * 3
            5:c 3:ad 1:be * 2",
            'a')]
        public void Tally(string ballots, char expectedWinner)
            => TallyCore(ballots, expectedWinner);

        [Test]
        public void StrategicBallot()
        {
            Assert.Ignore("TODO");
        }
    }
}