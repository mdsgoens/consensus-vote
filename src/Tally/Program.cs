using System;
using System.Linq;
using Consensus;
using Consensus.Ballots;
using Consensus.Methods;

namespace Tally
{
    class Program
    {
        static void Main(string[] args)
        {
            var ballots = args.Length > 0 ? args[0] : null;

            if (string.IsNullOrEmpty(ballots))
            {
                Console.WriteLine("Enter ballots:");
                ballots = Console.ReadLine();
            }

            var parsed = CandidateComparerCollection<RankedBallot>.Parse(ballots);
            var consensus = new ConsensusCoalition();

            var candidateCount = parsed.CandidateCount;
            var (winners, approvalCount, firstChoiceCount, compromises) = consensus.GetDetailedTally(parsed);

            Console.WriteLine("Winner: " + string.Join(", ", winners[0].Select(ParsingUtility.EncodeCandidateIndex)));

            Console.WriteLine("");
            Console.WriteLine("Approvals:");
            foreach (var (a, i) in approvalCount.Select((a, i) => (a, i)).OrderByDescending(a => a.a))
                Console.WriteLine($"{Candidate(i)}{Count(a)}");

            var beatMatrix = parsed.GetBeatMatrix();
            Console.WriteLine("");
            Console.WriteLine("Beat Matrix:");
            Console.WriteLine("beat=> " + string.Join("   ", Enumerable.Range(0, candidateCount).Select(ParsingUtility.EncodeCandidateIndex)));

            for (int i = 0; i < candidateCount; i++)
            {
                Console.Write(Candidate(i));
                Console.Write(" | ");
                for (int j = 0; j < candidateCount; j++)
                    Console.Write(Count(beatMatrix.Compare(i, j), 4));

                Console.WriteLine();
            }

            Console.WriteLine("");
            Console.WriteLine("Compromises:");
            if (compromises.Any())
            {
                Console.WriteLine("First Comp. Bogey Count");
                foreach (var ((firstChoice, compromiseChoice, bogeyman), count) in compromises.OrderByDescending(a => a.Count))
                    Console.WriteLine($"{Candidate(firstChoice, 5)} {Candidate(compromiseChoice, 5)} {Candidate(bogeyman, 5)} {Count(count, 5)}");
            }
            else
            {
                Console.Write("None");
            }

        }

        private static string Candidate(int index, int padding = 1) => new string(new [] { ParsingUtility.EncodeCandidateIndex(index) }).PadLeft(padding);
        private static string Count(int count, int padding = 3) => (count == 0 ? "" : count.ToString()).PadLeft(padding);
    }
}
