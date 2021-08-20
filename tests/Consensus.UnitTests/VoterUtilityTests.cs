using System.Linq;
using Faithlife.Testing;
using NUnit.Framework;

namespace Consensus.UnitTests.Methods
{
    [TestFixture]
    public class ParsingUtilityTests
    {
        [TestCase("b", new [] { 0, 100 })]
        [TestCase("   b   ", new [] { 0, 100 })]
        [TestCase("\r\nb\r\n\r\n", new [] { 0, 100 })]
        [TestCase("a b", new [] { 100, 50 })]
        [TestCase("100a 75b 50cd", new  [] { 100, 75, 50, 50 })]
        public void SingleVoter(string election, int[] expected)
        {
            var actualVoter = CandidateComparerCollection<Voter>.Parse(election).Comparers.Single();
            var expectedVoter = AsVoter(expected);
            AssertEx.Assert(() => actualVoter == expectedVoter);
        }

        [TestCase(@"
            a
            b",
            new [] { 100, 0 }, 
            new [] { 0, 100 })]
        [TestCase(
            @"a;b",
            new [] { 100, 0 }, 
            new [] { 0, 100 })]
        [TestCase(
            "b * 2",
            new [] { 0, 100 },
            new [] { 0, 100 })]
        [TestCase(@"
            a b
            b c
            c a", 
            new [] { 100, 50, 0 },
            new [] { 0, 100, 50 },
            new [] { 50, 0, 100 })]
        public void MultiVoter(string voters, params int[][] expected)
        {
            var actualVoters = CandidateComparerCollection<Voter>.Parse(voters);
            var expectedVoters = new CandidateComparerCollection<Voter>(expected[0].Length, expected.Select(AsVoter).ToCountedList());

            AssertEx.Assert(() => actualVoters == expectedVoters);
        }

        [TestCase("b")]
        [TestCase("ab")]
        [TestCase("b a")]
        [TestCase("50b")]
        [TestCase("b * 4")]
        [TestCase("a\r\nb")]
        [TestCase("a\r\nbc")]
        public void StringRoundTrip(string expected)
        {
            var actual = CandidateComparerCollection<Voter>.Parse(expected).ToString();
            AssertEx.Assert(() => actual == expected);
        }

        private static Voter AsVoter(int[] utilities) => new Voter(utilities);
    }
}