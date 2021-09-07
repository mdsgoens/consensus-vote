using NUnit.Framework;
using Consensus.Methods;
using Consensus.Ballots;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ConsensusCondorcetTests : VotingMethodTestBase<RankedConsensusCondorcet, RankedBallot>
    {
        [Test]
        public void CannotFormBasicCoalition()
        {
            // I would resonably expect A and B to band together, making B win or tie with A, but they do not.
            TallyCore("a b c * 31; b a c * 32; c ba * 37", "c");

            // I would expect the CBA voter to tiebreak between A and B, but it does not.
            TallyCore(@"a bc * 2; b ac * 2; c b a", "ab");
        }
    }
}