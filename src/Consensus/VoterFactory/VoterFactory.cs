using System.Linq;
using System;

namespace Consensus.VoterFactory
{
    // Capable of creating a voter who assigns a certain utility to each candidate.
    // Can also produce new VoterFactories to help create an electorate.
    public sealed class VoterFactory
    {
        public static VoterFactory Normal(int candidateCount, Random random)
        {
            var utilities = new double[candidateCount];

            for (int i = 0; i < candidateCount; i++)
                utilities[i] = MathNet.Numerics.Distributions.Normal.Sample(random, 0, 1);

            return new VoterFactory(utilities);
        }

        public static implicit operator Voter(VoterFactory source) => new Voter(source.m_utilities.Select(u => (int) (50 + 25 * u)).ToList());
        
        public static VoterFactory operator*(VoterFactory source, double multiplier) => multiplier * source;

        public static VoterFactory operator*(double multiplier, VoterFactory source) => new VoterFactory(source.m_utilities.Select(u => u * multiplier).ToArray());

        // https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L39
        // If both are standard normal to start with, the result will be standard normal too.
        public VoterFactory HybridWith(VoterFactory other, double weight)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (other.m_utilities.Length != m_utilities.Length)
                throw new ArgumentException(nameof(other), "May only combine voter factories with the same initial conditions.");

            var combinedUtilities = new double[m_utilities.Length];
            var denominator = Math.Sqrt(1 + weight * weight);

            for (int i = 0; i < m_utilities.Length; i++)
                combinedUtilities[i] = (m_utilities[i] + weight * other.m_utilities[i]) / denominator;

            return new VoterFactory(combinedUtilities);
        }

        private VoterFactory(double[] utilities)
        {
            m_utilities = utilities;
        }

        private readonly double[] m_utilities;
    }
}