using System.Linq;
using System;

namespace Consensus.VoterModels
{
    public sealed class VoterFactory
    {
        public VoterFactory(double[] utilities)
        {
            m_utilities = utilities;
        }

        public static implicit operator Voter(VoterFactory source) => new Voter(source.m_utilities.Select(u => (int) (50 + 25 * u)).ToList());
        
        public static VoterFactory operator*(VoterFactory source, double multiplier) => multiplier * source;

        public static VoterFactory operator*(double multiplier, VoterFactory source) => new VoterFactory(source.m_utilities.Select(u => u * multiplier).ToArray());

        // https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L39
        public VoterFactory HybridWith(VoterFactory other, double weight)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (other.m_utilities.Length != m_utilities.Length || other.m_random != m_random)
                throw new ArgumentException(nameof(other), "May only combine voter factories with the same initial conditions.");

            var combinedUtilities = new double[m_utilities.Length];
            var denominator = Math.Sqrt(1 + weight * weight);

            for (int i = 0; i < m_utilities.Length; i++)
                combinedUtilities[i] = (m_utilities[i] + weight * other.m_utilities[i]) / denominator;

            return new NormalVoterFactory(m_random, combinedUtilities);
        }

        private readonly double[] m_utilities;
    }
}