using System.Reflection;
using System;
using Consensus;

namespace Tally
{
    class Program
    {
        static void Main(string[] args)
        {
            var methodName = args[0];

            if (string.IsNullOrWhiteSpace(methodName))
            {
                Console.WriteLine("Must provide a voding method in the first argument.");
                return;
            }

            var methodType = Assembly.GetAssembly(typeof(VotingMethodBase))
                .GetType("Consensus.Methods." + methodName);

            if (methodType == null)
            {
                Console.WriteLine("Must provide a valid voding method in the first argument.");
                return;
            }

            var method = Activator.CreateInstance(methodType) as VotingMethodBase;

            if (method == null)
            {
                Console.WriteLine("Must provide a valid voding method in the first argument.");
                return;
            }

            var ballots = args[1];

            if (string.IsNullOrWhiteSpace(ballots))
            {
                Console.WriteLine("Must provide a set of ballots as the second argument.");
                return;
            }

            if (ballots == "voters")
            {
                var voters = args[2];

                if (string.IsNullOrWhiteSpace(voters))
                {
                    Console.WriteLine("Must provide a set of voters as the third argument when the second is 'honest'.");
                    return;
                }
            
                var results = method.GetPossibleResults(CandidateComparerCollection<Voter>.Parse(voters));

                Console.WriteLine(results.GetHonestBallot());
                Console.WriteLine(results.Honest.Details);
                var (favorite, utility) = results.GetPlausibleStrategies();
                Console.WriteLine("Favorite: " + string.Join(", ", favorite));
                Console.WriteLine("Utility: " + string.Join(", ", utility));
            }
            else
            {
                var results = method.GetElectionResults(ballots);

                Console.Write(results.Details);
            }
        }
    }
}
