using System.Text;
using System.Collections.Generic;
using System;
using System.Linq;
using Consensus.VoterFactory;
using System.Reflection;
using Consensus;

var uniqueCandidateCount = 5;
var voterCount = 100;
var seed = new Random().Next();
var random = new Random(seed);

Console.WriteLine("Seed: " + seed);

var voterModels = new (string Name, int candidateCount, Func<IEnumerable<VoterFactory>> GetVoters) [] {
    ("2d", uniqueCandidateCount, () =>
        Electorate.Normal(2, random).PolyaModel(random).DimensionalModel(
            random,
            uniqueCandidateCount,
            voterCount)),
    ("Cycles", uniqueCandidateCount, () =>
        Electorate.Normal(uniqueCandidateCount, random).Cycle().Quality(random).PolyaModel(random)),
    ("Clones", uniqueCandidateCount + 2, () =>
        Electorate.Normal(3, random).PolyaModel(random).DimensionalModel(
                random,
                uniqueCandidateCount,
                voterCount)
            .Select(v => v.Clone(0).Clone(1)))
};

var votingMethods = Assembly.GetAssembly(typeof(VotingMethodBase))
    .GetTypes()
    .Where(t => t.IsAssignableTo(typeof(VotingMethodBase)) && !t.IsAbstract && t.CustomAttributes.All(a => a.AttributeType != typeof(ObsoleteAttribute)))
    .Select(t => Activator.CreateInstance(t) as VotingMethodBase)
    .ToList();

if (args.Length > 0)
{
    votingMethods = votingMethods.Where(vm => args.Contains(vm.GetType().Name)).ToList();

    if (!votingMethods.Any())
        throw new InvalidOperationException("Args must be valid voting systems.");
}

var strategies = Enum.GetValues<VotingMethodBase.Strategy>();

var scoreSumByMethod = votingMethods.ToDictionary(
    m => m,
    _ => voterModels.ToDictionary(
        a => a.Name,
        _ => new Dictionary<VotingMethodBase.Strategy, double>()));
                      
var percentileByMethod = votingMethods.ToDictionary(
    m => m,
    _ => voterModels.ToDictionary(
        a => a.Name,
        _ => new Dictionary<VotingMethodBase.Strategy, SortedList<double, int>>()));

Console.Write("Trials: 0");

int trialCount = 0;
while (true)
{
    trialCount++;
    Console.Write("\rTrials: " + trialCount);

    foreach (var (model, candidateCount, getVoters) in voterModels)
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
                scoreSumByMethod[method][model][strategy] = scoreSumByMethod[method][model].TryGetValue(strategy, out var value)
                    ? value + result
                    : result;

                if (!percentileByMethod[method][model].TryGetValue(strategy, out var percentile))
                    percentileByMethod[method][model][strategy] = percentile = new SortedList<double, int>();

                if (percentile.TryGetValue(result, out var count))
                    percentile[result] = count + 1;
                else
                    percentile.Add(result, 1);
            }
        }
    }

    GC.Collect();

    if (trialCount % 20 == 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Average");
        ElectionResults.AppendTable(sb,
            scoreSumByMethod.Select(a => 
                strategies.Select(s =>
                    {
                        var scores = voterModels.Where(m => a.Value[m.Name].ContainsKey(s)).Select(m => 100 * a.Value[m.Name][s] / trialCount).ToList();

                        if (!scores.Any())
                            return (ElectionResults.Value) "";

                        var min = scores.Min().ToString("N1");
                        var max = scores.Max().ToString("N1");

                        if (min == max)
                            return min;

                        return (ElectionResults.Value) min + " - " + max;

                    })
                    .Prepend(a.Key.GetType().Name)
                    .ToArray()),
            strategies.SelectToArray(s => (ElectionResults.Value) s.ToString())
        );
                    
        sb.AppendLine();
        sb.AppendLine("# 95th Percentile");
        ElectionResults.AppendTable(sb,
            percentileByMethod.Select(a => 
                strategies.Select(s =>
                    {
                        var scores = voterModels.Where(m => a.Value[m.Name].ContainsKey(s)).Select(m => 
                        {
                            var remaining = trialCount / 20;

                            foreach (var (value, count) in a.Value[m.Name][s])
                            {
                                remaining -= count;
                                if (remaining <= 0)
                                    return value * 100;
                            }

                            throw new InvalidOperationException("There is a bug.");
                        }).ToList();

                        if (!scores.Any())
                            return (ElectionResults.Value) "";

                        var min = scores.Min().ToString("N1");
                        var max = scores.Max().ToString("N1");

                        if (min == max)
                            return min;

                        return (ElectionResults.Value) min + " - " + max;

                    })
                    .Prepend(a.Key.GetType().Name)
                    .ToArray()),
            strategies.SelectToArray(s => (ElectionResults.Value) s.ToString())
        );

        Console.Clear();
        Console.WriteLine(sb.ToString());
    }
}