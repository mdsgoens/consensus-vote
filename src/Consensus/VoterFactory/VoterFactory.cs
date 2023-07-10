using System.Collections.Generic;
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
            return new VoterFactory(candidateCount.LengthArray(_ => MathNet.Numerics.Distributions.Normal.Sample(random, 0, 1)));
        }

        public int CandidateCount => m_utilities.Length;
        public double this[int index] => m_utilities[index];

        public static implicit operator Voter(VoterFactory source) => new Voter(source.m_utilities.SelectToArray(u => (int) (10 * u)));
        
        public static VoterFactory operator*(VoterFactory source, double multiplier) => multiplier * source;

        public static VoterFactory operator*(double multiplier, VoterFactory source) => new VoterFactory(source.m_utilities.SelectToArray(u => u * multiplier));

        // https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L39
        // If both are standard normal to start with, the result will be standard normal too.
        public VoterFactory HybridWith(VoterFactory other, double weight)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (other.m_utilities.Length != m_utilities.Length)
                throw new ArgumentException("May only combine voter factories with the same initial conditions.", nameof(other));

            var combinedUtilities = new double[m_utilities.Length];
            var denominator = Math.Sqrt(1 + weight * weight);

            for (int i = 0; i < m_utilities.Length; i++)
                combinedUtilities[i] = (m_utilities[i] + weight * other.m_utilities[i]) / denominator;

            return new VoterFactory(combinedUtilities);
        }

        // Ceates a cycle of voters, one for each candidate, each with rotated preferences 
        public IEnumerable<VoterFactory> CreateCycle()
        {
            yield return this;

            for (var offset = 1; offset < m_utilities.Length; offset++)
            {
                var utilities = new double[m_utilities.Length];
                for (var i = 0; i < m_utilities.Length; i++)
                    utilities[i] = m_utilities[(i + offset) % m_utilities.Length];

                yield return new VoterFactory(utilities);
            }
        }

        // Creates a new VoterFactory wherein each candidates' utilitity
        // is determined by this VoterFactory's proximity to each candidate
        // in n-dimensional space (determined by the "candidate" utilities in the source factories)
        public VoterFactory ProximityTo(IReadOnlyList<VoterFactory> candidates)
        {
            return new VoterFactory(candidates.SelectToArray(c => 
                -Math.Sqrt(c.m_utilities
                    .Zip(m_utilities, (a, b) => (a - b) * (a - b))
                    .Sum()))
            );
        }

        // Creates a new VoterFactory with a clone of the specified candidate
        public VoterFactory Clone(int candidate)
        {
            var utilities = new double[m_utilities.Length + 1];
            
            Array.Copy(m_utilities, utilities, m_utilities.Length);
            utilities[m_utilities.Length] = m_utilities[candidate];

            return new VoterFactory(utilities);
        }

        private VoterFactory(double[] utilities)
        {
            m_utilities = utilities;
        }

        private readonly double[] m_utilities;
    }
}