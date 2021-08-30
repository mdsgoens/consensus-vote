using NUnit.Framework;
using Consensus.Methods;

namespace Consensus.UnitTests.Methods
{
    public class ConsensusCoalitionTests : ConsensusVoteTestsBase<ConsensusCoalition>
    {
        [TestCase(@"
            ab c * 51
            a b c * 49",
            "a")]
        public void Tally(string ballots, string expectedWinner) => TallyCore(ballots, expectedWinner);

        [Test]
        public void TestCoalitionBuilding()
        {
            TallyCore("a b c * 49; b c a * 48; c b a * 3", "b");
            TallyCore("a bc * 48; a b c; b c a * 48; c b a * 3", "b");
            TallyCore("a bc * 48; a c b; b c a * 48; c b a * 3", "c");
            TallyCore("a bc * 49; b c a * 48; c b a * 3", "bc");
        }
    }
}