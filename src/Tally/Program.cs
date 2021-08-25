using System.Reflection;
using System;
using Consensus.Methods;

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

            var results = method.GetElectionResults(ballots);

            Console.Write(results.Details);
        }
    }
}
