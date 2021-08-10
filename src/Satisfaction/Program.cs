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
            var trialCount = 10_000;
            var seed = new Random().Next();
            var random = new Random(seed);

            Console.WriteLine("Seed: " + seed);

        
            var factory = new ElectorateFactory(random, candidateCount);
            var votingMethods = new VotingMethodBase[] {
                new Approval(),
                new ConsensusVote(),
                new InstantRunoff(),
                new Plurality(),
                new RandomVote(),
                new Score(),
                new Star(),
                new V321(),
            };

            var scoresByMethod = votingMethods.ToDictionary(a => a, _ => 0m);

            Console.Write("Trials: 0");

            for (int i = 0; i < trialCount; i++)
            {
                Console.Write("\rTrials: " + i);

                // Get the voters!
                var voters = new CandidateComparerCollection<Voter>(
                    candidateCount, 
                    factory.Quality(factory.PolyaModel(factory.Mirror(factory.NormalElectorate()))).Take(voterCount).Select(v => (Voter) v).ToList());

                // Test the methods!
                foreach (var m in votingMethods)
                {
                    var satisfaction = m.CalculateSatisfaction(random, voters);
                    scoresByMethod[m] += satisfaction[VotingMethodBase.Strategy.Honest];
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");

            foreach (var a in scoresByMethod.OrderByDescending(a => a.Value))
            {
                Console.WriteLine($"{a.Key.GetType().Name}: {a.Value / trialCount:P2}");
            }
        }
    }
}
