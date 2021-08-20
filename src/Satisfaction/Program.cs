using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;
using Consensus.Methods;
using Consensus.VoterFactory;

namespace Consensus.Satisfaction
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var candidateCount = 5;
            var voterCount = 1000;
            var trialCount = 10_0;
            var seed = new Random().Next();
            var random = new Random(seed);

            Console.WriteLine("Seed: " + seed);

            var votingMethods = new VotingMethodBase[] {
                new Approval(),
                new ConsensusCoalition(),
                new InstantRunoff(),
                new Plurality(),
                new RandomVote(),
                new Score(),
                new Star(),
                new V321(),
                new CondorcetRandomVote(),
            };

            var strategies = Enum.GetValues<VotingMethodBase.Strategy>();

            var scoresByMethod = votingMethods.ToDictionary(a => a, _ => new Dictionary<VotingMethodBase.Strategy, VotingMethodBase.SatisfactionResult>());

            Console.Write("Trials: 0");

            for (int i = 0; i < trialCount; i++)
            {
                Console.Write("\rTrials: " + i);

                // Get the voters!
                var voters = new CandidateComparerCollection<Voter>(
                    candidateCount,
                    Electorate.Normal(candidateCount, random)
                    .Mirror()
                    .PolyaModel(random)
                    .Quality(random)
                    .Take(voterCount)
                    .Select(v => (Voter) v)
                    .ToCountedList());

                // Test the methods!
                foreach (var m in votingMethods)
                {
                    var satisfaction = m.CalculateSatisfaction(random, voters);
                    foreach (var kvp in satisfaction)
                    {
                        scoresByMethod[m][kvp.Key] = scoresByMethod[m].TryGetValue(kvp.Key, out var value)
                            ? new VotingMethodBase.SatisfactionResult(value.Satisfaction + kvp.Value.Satisfaction, value.StrategyRatio + kvp.Value.StrategyRatio)
                            : kvp.Value;
                    }
                }
            }

            PrintTable("Satisfaction", a => a.Satisfaction);
            PrintTable("Strategy Ratio", a => a.StrategyRatio);

            void PrintTable(string name, Func<VotingMethodBase.SatisfactionResult, double> getValue)
            {
                Console.WriteLine("");
                Console.WriteLine($"# {name}");

                var methodNameLength = scoresByMethod.Max(a => a.Key.GetType().Name.Length);

                Console.Write("Method".PadLeft(methodNameLength));

                foreach (var strategy in strategies)
                    Console.Write(" | " + strategy.ToString().PadLeft(6));

                Console.WriteLine("");
                foreach (var a in scoresByMethod.OrderByDescending(a => a.Value.Max(b => getValue(b.Value))))
                {
                    Console.Write(a.Key.GetType().Name.PadLeft(methodNameLength));
                    
                    foreach (var strategy in strategies)
                        Console.Write(" | " + (getValue(a.Value[strategy]) / trialCount).ToString("P2").PadLeft(6).PadLeft(strategy.ToString().Length));
                    
                    Console.WriteLine("");
                }
            }
        }
    }
}
