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
            var candidateCount = 5;
            var voterCount = 100;
            var trialCount = 10_000;
            var seed = new Random().Next();
            var random = new Random(seed);

            Console.WriteLine("Seed: " + seed);

            var voterModels = new (string Name, Func<IEnumerable<VoterFactory.VoterFactory>> GetVoters) [] {
                ("Partisan", () =>
                    Electorate.DimensionalModel(
                        Electorate.Normal(1, random).Mirror().PolyaModel(random),
                        random,
                        candidateCount,
                        voterCount)),
                ("2d", () =>
                    Electorate.DimensionalModel(
                        Electorate.Normal(2, random).PolyaModel(random),
                        random,
                        candidateCount,
                        voterCount)),
                ("Cycles", () =>
                    Electorate.Normal(candidateCount, random).Cycle().PolyaModel(random).Quality(random))
            };

            var votingMethods = Assembly.GetAssembly(typeof(VotingMethodBase))
                .GetTypes()
                .Where(t => t.IsAssignableTo(typeof(VotingMethodBase)) && !t.IsAbstract)
                .Select(t => Activator.CreateInstance(t) as VotingMethodBase)
                .ToList();

            var strategies = Enum.GetValues<VotingMethodBase.Strategy>();

            var scoresByMethod = votingMethods.ToDictionary(m => m, _ => voterModels.ToDictionary(a => a.Name, _ => new Dictionary<VotingMethodBase.Strategy, VotingMethodBase.SatisfactionResult>()));

            Console.Write("Trials: 0");

            for (int i = 0; i < trialCount; i++)
            {
                Console.Write("\rTrials: " + i);

                foreach (var (model, getVoters) in voterModels)
                {
                    // Get the voters!
                    var voters = new CandidateComparerCollection<Voter>(
                        candidateCount,
                        getVoters()
                            .Take(voterCount)                        
                            .Select(v => (Voter) v)
                            .ToCountedList());

                    // Test the methods!
                    foreach (var method in votingMethods)
                    {
                        foreach (var (strategy, result) in method.CalculateSatisfaction(random, voters))
                        {
                            scoresByMethod[method][model][strategy] = scoresByMethod[method][model].TryGetValue(strategy, out var value)
                                ? new VotingMethodBase.SatisfactionResult(
                                    value.AllVoterSatisfaction + result.AllVoterSatisfaction,
                                    value.StrategicVoterSatisfaction + result.StrategicVoterSatisfaction,
                                    value.StrategicVoterSatisfactionWithHonestOutcome + result.StrategicVoterSatisfactionWithHonestOutcome)
                                : result;
                        }
                    }
                }
            }

            PrintTable("Satisfaction", a => a.AllVoterSatisfaction / trialCount);

            void PrintTable(string name, Func<VotingMethodBase.SatisfactionResult, double> getValue)
            {
                Console.WriteLine("");
                Console.WriteLine($"# {name}");

                var methodNameLength = scoresByMethod.Max(a => a.Key.GetType().Name.Length);

                Console.Write("Method".PadLeft(methodNameLength));

                foreach (var (model, _) in voterModels)
                    Console.Write(" | " + model.PadLeft(5));

                Console.WriteLine("");
                foreach (var a in scoresByMethod.OrderByDescending(a => a.Value.Max(b => getValue(b.Value[VotingMethodBase.Strategy.Honest]))))
                {
                    Console.Write(a.Key.GetType().Name.PadLeft(methodNameLength));
                    
                foreach (var (model, _) in voterModels)
                    {
                        Console.Write(" | ");
                        
                        if (a.Value.ContainsKey(model))
                            Console.Write((getValue(a.Value[model][VotingMethodBase.Strategy.Honest])).ToString("P1").PadLeft(5).PadLeft(model.ToString().Length));
                        else
                            Console.Write("".PadLeft(model.ToString().Length));
                    }
                    
                    Console.WriteLine("");
                }
            }
        }
    }
}
