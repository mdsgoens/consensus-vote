using NUnit.Framework;
using Consensus.Ballots;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ConsensusVoteTestsBase<T> : VotingMethodTestBase<T, RankedBallot>
        where T : RankedConsensusBase, new()
    {
        [TestCase("b", "b a")]
        [TestCase("ab", "ab")]
        [TestCase("a b c", "a b c")]
        public void TestHonestBallots(string voter, string expectedBallot) => HonestBallotCore(voter, expectedBallot);

        [TestCase("a b", "a")]
        [TestCase(@"a b; b a * 2", "b")]
        [TestCase(@"a b c * 2; b a c * 2; c b a", "b")]
        public void TestBasicTallies(string ballots, string expectedWinner) => TallyCore(ballots, expectedWinner);

        [Test]
        public void TestFavoriteBetrayal()
        {
            // When the CBA voters express their honest preference, they spook the BAC voters into supporting A.
            TallyCore("a c b * 49; b a c * 48; c b a * 3", "a");

            // But if they drop support for their favorite, they can get their second-favorite to win.
            TallyCore("a c b * 49; b a c * 48; b a c * 3", "b");            
        }

        [Test]
        public void TestBurying()
        {
            // When the ABC voters express their honest preference, they spook the CBA voters into supporting B.
            TallyCore("a b c * 49; b a c * 48; c b a * 3", "b");

            // But if they drop support thier support for B below C, they'll *add* support for C if B wins -- which scares the BAC voters into voting for A.
            TallyCore("a c b * 49; b a c * 48; c b a * 3", "a");            
        }
      
        [Test]
        public void TestDarkHorse()
        {
            // In a "Dark Horse" scenario where we'd normally elect A:
            TallyCore("a bc d * 37; b ca d * 32; c ba d * 31", "a");
            
            // **if** B and C voters artifically elevating a "Dark Horse" candidate D could result in B (or A) winning:
            TallyCore("a bc d * 37; b d ca * 32; c d ba * 31", "b", "d");

            // Then A voters still have a dominating strategy in "truncation" so B or C can never win just by supporting the dark horse
            // (and therefore have no incentive to do so)
            for (int i = 0; i <= 32; i++)
            {
                for (int j = 0; j <= 31; j++)
                {
                    TallyCore($"a bcd * 37; b d ca * {i}; b ca d * {32 - i}; c d ba * {j}; c ba d * {31 - j}", "a", "d", "ad");
                }
            }
        }

        [Test]
        public void StrategicBallot()
        {
            //TODO
            Assert.Ignore("TODO");
        }
    }
}