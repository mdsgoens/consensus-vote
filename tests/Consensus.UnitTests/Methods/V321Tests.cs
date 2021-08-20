using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class V321Tests : VotingMethodTestBase<V321, BucketBallot<V321.Result>>
    {
        [TestCase("b", "Good:b Bad:a")]
        [TestCase("ab", "Good:ab")]
        [TestCase("a b", "Good:a Bad:b")]
        [TestCase("100a 80b 50cd", "Good:a Adequate:b Bad:cd")]
        public void HonestBallot(string voter, string expectedBallot) => HonestBallotCore(voter, expectedBallot);

        [TestCase(@"
            Good:a * 2
            Good:b",
            'a')]
        [TestCase(@"
            Good:a Adequate:bd Bad:ce * 4
            Good:b Adequate:cd Bad:ae * 3
            Good:c Adequate:ad Bad:be * 2",
            'a')]
        public void Tally(string ballots, char expectedWinner) => TallyCore(ballots, expectedWinner);

        [Test]
        public void StrategicBallot()
        {
            Assert.Ignore("TODO");
        }
    }
}