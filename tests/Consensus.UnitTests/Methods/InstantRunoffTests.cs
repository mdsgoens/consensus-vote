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

        [TestCase("a b", "a")]
        [TestCase(@"
            a
            b a * 2",
            "b")]
        [TestCase(@"
            a b c * 2
            b a c * 2
            c b a",
            "b")]
        [TestCase(@"
            a * 4
            b c * 3
            c a * 2",
            "a")]
        [TestCase("b e c a d * 4; c d b e a * 2; a c d e b * 2; d a c e b; a d c b e", "b")]
        [TestCase("d c e a b * 2; b a c d e * 2; e c d a b; d c a e b; e c a d b; b a d c e; d c e b a; b c a e d", "d")]
        public void Tally(string ballots, string expectedWinner) => TallyCore(ballots, expectedWinner);

        [TestCase(
            new[] { "a", "c", },
            "b c a",
            "c b a",
            Description = "Betray your favorite")]
        public void StrategicBallot(string[] polls, string voter, string expectedBallot) => StrategicBallotCore(polls, voter, expectedBallot);
    }
}