using Consensus.Methods;
using Faithlife.Testing;
using System;
using System.Reflection;

namespace Consensus.UnitTests.Methods
{
    public abstract class VotingMethodTestBase<TMethod, TBallot, TTally>
        where TMethod : VotingMethodBase<TBallot, TTally>, new()
        where TTally : VotingMethodBase.Tally
        where TBallot : CandidateComparer
    {
        protected void HonestBallotCore(string voter, string expectedBallot)
        {
            var actualBallot = new TMethod()
                .GetHonestBallot(Voter(voter))
                .ToString();

            AssertEx.Assert(() => actualBallot == expectedBallot);
        }

        public void TallyCore(string ballots, char expectedWinner)
        {
            var actualWinner = ParsingUtility.EncodeCandidateIndex(
                new TMethod().GetTally(Ballots(ballots)).Winner
            );

            AssertEx.Assert(() => actualWinner == expectedWinner);
        }

        public void StrategicBallotCore(TTally tally, string voter, string expectedBallot)
        {
            var actualBallot = new TMethod()
                .GetStrategicBallot(
                    tally,
                    Voter(voter))
                .ToString();

            AssertEx.Assert(() => actualBallot == expectedBallot);
        }

        public static TBallot Ballot(string source) => ParseBallot(ParsingUtility.NumberOfCandidates(source), source);
        public static CandidateComparerCollection<TBallot> Ballots(string source)
            => CandidateComparerCollection<TBallot>.Parse(source, ParseBallot);

        public static Voter Voter(string source) => Consensus.Voter.Parse(ParsingUtility.NumberOfCandidates(source), source);
        public static CandidateComparerCollection<Voter> Voters(string source)
            => CandidateComparerCollection<Voter>.Parse(source, Consensus.Voter.Parse);

        private static TBallot ParseBallot(int numberOfCandidates, string source) => (TBallot) s_parse.Invoke(null, new object[] { numberOfCandidates, source });
        
        private static readonly MethodInfo s_parse = typeof(TBallot).GetMethod("Parse", new [] { typeof(int), typeof(string) })
            ?? throw new InvalidOperationException("Could not find a Parse method.");
    }
}