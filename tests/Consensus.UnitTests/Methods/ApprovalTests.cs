using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;
using System.Collections.Generic;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ApprovalTests : VotingMethodTestBase<Approval, ApprovalBallot>
    {
        [TestCase("b", "b")]
        [TestCase("ab", "ab")]
        [TestCase("a b", "a")]
        [TestCase("100a 80b 50cd", "ab")]
        public void HonestBallot(string voter, string expectedBallot) => HonestBallotCore(voter, expectedBallot);

        [TestCase(@"
            a * 2
            b",
            'a')]
        [TestCase(@"
            a
            b * 2",
            'b')]
        public void Tally(string ballots, char expectedWinner) => TallyCore(ballots, expectedWinner);

        [TestCase(
            new[] { "a", "b" },
            "ad 0bc",
            "ad",
            Description = "Contine supporting winner")]
        [TestCase(
            new[] { "a", "b" },
            "100ad 99b c",
            "ad",
            Description = "Drop support for runner-up when favorite has the same chance of winning")]
        public void StrategicBallot(string[] polls, string voter, string expectedBallot) => StrategicBallotCore(polls, voter, expectedBallot);
    }
}