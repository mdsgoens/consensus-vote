using System.Linq;
using Consensus.Methods;
using Faithlife.Testing;
using System;
using System.Reflection;
using Consensus.VoterFactory;
using NUnit.Framework;

namespace Consensus.UnitTests.Methods
{
    public abstract class VotingMethodTestBase<TMethod, TBallot>
        where TMethod : VotingMethodBase<TBallot>, new()
        where TBallot : CandidateComparer
    {
        protected void HonestBallotCore(string voter, string expectedBallot)
        {
            var parsedVoter = Voter(voter);
            var actualBallot = new TMethod()
                .GetHonestBallot(Voter(voter))
                .ToString();

            using (AssertEx.Context(() => parsedVoter))
                AssertEx.Assert(() => actualBallot == expectedBallot);
        }

        public void TallyCore(string ballots, char expectedWinner)
        {
            var parsedBallots = Ballots(ballots);
            var actualWinner = ParsingUtility.EncodeCandidateIndex(
                new TMethod().GetRanking(parsedBallots)[0].Single()
            );

            using (AssertEx.Context(() => parsedBallots))
                AssertEx.Assert(() => actualWinner == expectedWinner);
        }

        public void StrategicBallotCore(string[] polls, string voter, string expectedBallot)
        {
            var parsedVoter = Voter(voter);
            var polling = Polling.FromBallots(
                polls.Select(p => Ballots(p, parsedVoter.CandidateCount)).ToCountedList(),
                new TMethod().GetRanking);

            var actualBallot = new TMethod()
                .GetStrategicBallot(
                    polling,
                    parsedVoter)
                .ToString();

            using (AssertEx.Context(() => parsedVoter, () => polling))
                AssertEx.Assert(() => actualBallot == expectedBallot);
        }

        [Test]
        public void TestStrategicVoteNeverHarms()
        {
            var seed = new Random().Next();
            var random = new Random(seed);
            using var a = AssertEx.Context("seed", seed);

            var candidateCount = random.Next(3) + 3;
            var voterCount = random.Next(3) + 6;
            var voterCounts = Enumerable.Range(0, voterCount).Select(_ => random.Next(10) + 1);

            var voters = new CandidateComparerCollection<Voter>(
                candidateCount,
                Electorate.Normal(candidateCount, random).Cycle().Quality(random).Take(voterCount)
                .Zip(voterCounts, (a, b) => ((Voter) a, b))
                .ToCountedList());
 
            Assert.Ignore("Not implemented.");
        }

        public static CandidateComparerCollection<TBallot> Ballots(string source, int? candidateCount = null)
            => CandidateComparerCollection<TBallot>.Parse(source, candidateCount);

        public static Voter Voter(string source) => Voters(source).Comparers.Single();
        public static CandidateComparerCollection<Voter> Voters(string source) => CandidateComparerCollection<Voter>.Parse(source);
    }
}