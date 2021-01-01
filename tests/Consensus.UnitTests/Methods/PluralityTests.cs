using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class PluralityTests : VotingMethodTestBase<Plurality, ApprovalBallot, VotingMethodBase.Tally>
    {
        [TestCase("b", "b")]
        [TestCase("ab", "a")]
        [TestCase("a b", "a")]
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

        [TestCase("a b c", "a", Description = "Contine supporting winner")]
        [TestCase("c b a", "b", Description = "Support the 'lesser of two evils'")]
        public void StrategicBallot(string voter, string expectedBallot) => StrategicBallotCore(
            new VotingMethodBase.Tally(Winner: 0, RunnersUp: new [] { 1 } ),
            voter,
            expectedBallot);
    }
}