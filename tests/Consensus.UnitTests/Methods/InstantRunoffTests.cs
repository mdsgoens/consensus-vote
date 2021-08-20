using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;
using System.Collections.Generic;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class InstantRunoffTests : VotingMethodTestBase<InstantRunoff, RankedBallot>
    {
        [TestCase("b", "b a")]
        [TestCase("ab", "a b")]
        [TestCase("a b c", "a b c")]
        public void HonestBallot(string voter, string expectedBallot) => HonestBallotCore(voter, expectedBallot);

        [TestCase("a b", 'a')]
        [TestCase(@"
            a
            b a * 2",
            'b')]
        [TestCase(@"
            a b c * 2
            b a c * 2
            c b a",
            'b')]
        [TestCase(@"
            a * 4
            b c * 3
            c a * 2",
            'a')]
        public void Tally(string ballots, char expectedWinner) => TallyCore(ballots, expectedWinner);

        [TestCase(
            new[] { "a", "c", },
            "b c a",
            "c b a",
            Description = "Betray your favorite")]
        public void StrategicBallot(string[] polls, string voter, string expectedBallot) => StrategicBallotCore(polls, voter, expectedBallot);
    }
}