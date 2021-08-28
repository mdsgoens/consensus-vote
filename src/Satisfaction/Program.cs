using System.Collections.Generic;
using System.Data;
using System;
using System.Linq;
using Consensus.Methods;
using Consensus.VoterFactory;
using System.Reflection;

namespace Consensus.Satisfaction
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var issueCount = 1;
            var candidateCount = 5;
            var voterCount = 200;
            var trialCount = 10_000;
            var seed = new Random().Next();
            var random = new Random(seed);

            Console.WriteLine("Seed: " + seed);

            var votingMethods = Assembly.GetAssembly(typeof(VotingMethodBase))
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(VotingMethodBase)) && !t.IsAbstract)
                .Select(t => Activator.CreateInstance(t) as VotingMethodBase)
                .ToList();

            var strategies = Enum.GetValues<VotingMethodBase.Strategy>();

            var scoresByMethod = votingMethods.ToDictionary(a => a, _ => new Dictionary<VotingMethodBase.Strategy, VotingMethodBase.SatisfactionResult>());

            Console.Write("Trials: 0");

            for (int i = 0; i < trialCount; i++)
            {
                Console.Write("\rTrials: " + i);

                // Get the voters!
                var voters = new CandidateComparerCollection<Voter>(
                    candidateCount,
                    Electorate.DimensionalModel(
                        Electorate.Normal(issueCount, random)
                            .PolyaModel(random)
                            .Quality(random)
                            .Take(voterCount),
                            random,
                            candidateCount)                        
                    .Select(v => (Voter) v)
                    .ToCountedList());

                // Test the methods!
                foreach (var m in votingMethods)
                {
                    Console.Write("\rTrials: " + i + " " + m.GetType().Name.PadRight(20));

                    foreach (var (strategy, result) in m.CalculateSatisfaction(random, voters))
                    {
                        scoresByMethod[m][strategy] = scoresByMethod[m].TryGetValue(strategy, out var value)
                            ? new VotingMethodBase.SatisfactionResult(
                                value.AllVoterSatisfaction + result.AllVoterSatisfaction,
                                value.StrategicVoterSatisfaction + result.StrategicVoterSatisfaction,
                                value.StrategicVoterSatisfactionWithHonestOutcome + result.StrategicVoterSatisfactionWithHonestOutcome)
                            : result;
                    }
                }
            }

            PrintTable("Satisfaction", a => a.AllVoterSatisfaction / trialCount);
            PrintTable("Strategy Ratio", a => (a.StrategicVoterSatisfaction - a.StrategicVoterSatisfactionWithHonestOutcome) / a.StrategicVoterSatisfactionWithHonestOutcome);

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
                    {
                        Console.Write(" | ");
                        
                        if (a.Value.ContainsKey(strategy))
                            Console.Write((getValue(a.Value[strategy])).ToString("P2").PadLeft(6).PadLeft(strategy.ToString().Length));
                        else
                            Console.Write("".PadLeft(strategy.ToString().Length));
                    }
                    
                    Console.WriteLine("");
                }
            }
        }
    }
}
