using System;
using NUnit.Framework;
using Consensus.Methods;
using Faithlife.Testing;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class RandomVoteTests
    {
        [Test]
        public void Satisfaction()
        {
            var satisfaction = new RandomVote().CalculateSatisfaction(
                new Random(),
                new CandidateComparerCollection<Voter>(2, new CountedList<Voter> { new Voter(new [] { 1, 0 }) } ));
           
            AssertEx.Assert(() => satisfaction[VotingMethodBase.Strategy.Honest] == 1d);
        }
    }
}