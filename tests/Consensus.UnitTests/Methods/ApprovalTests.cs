using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ApprovalTests : VotingMethodTestBase<Approval, ApprovalBallot, VotingMethodBase.Tally>
    {
        [TestCase("b", "b")]
        [TestCase("ab", "ab")]
        [TestCase("a b", "a")]
        [TestCase("100a 80b 50cd", "ab")]
        public void HonestBallot(string voter, string expectedBallot)
            => HonestBallotCore(voter, expectedBallot);

        [TestCase(@"
            a * 2
            b",
            'a')]
        [TestCase(@"
            a
            b * 2",
            'b')]
        public void Tally(string ballots, char expectedWinner)
            => TallyCore(ballots, expectedWinner);

        [TestCase("ad 0bc", "ad", Description = "Contine supporting winner")]
        [TestCase("100ad 99b c", "ad", Description = "Drop support for runner-up when favorite wins")]
        [TestCase("100bd 99a c", "bd", Description = "Drop support for winner when favorite is runner up")]
        public void StrategicBallot(string voter, string expectedBallot) => StrategicBallotCore(
            new VotingMethodBase.Tally(Winner: 0, RunnersUp: new [] { 1 } ),
            voter,
            expectedBallot);
    }
}