using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;
using System.Collections.Generic;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class PluralityTests : VotingMethodTestBase<Plurality, ApprovalBallot>
    {
        [TestCase("b", "b")]
        [TestCase("ab", "a")]
        [TestCase("a b", "a")]
        public void HonestBallot(string voter, string expectedBallot) => HonestBallotCore(voter, expectedBallot);

        [TestCase(@"
            a * 2
            b",
            "a")]
        [TestCase(@"
            a
            b * 2",
            "b")]
        public void Tally(string ballots, string expectedWinner) => TallyCore(ballots, expectedWinner);

        [TestCase(
            new[] { "a", "b" },
            "a b c",
            "a",
            Description = "Contine supporting winner")]
        [TestCase(
            new[] { "a", "b" },
            "c b a",
            "b",
            Description = "Support the 'lesser of two evils'")]
        public void StrategicBallot(string[] polls, string voter, string expectedBallot) => StrategicBallotCore(polls, voter, expectedBallot);
    }
}