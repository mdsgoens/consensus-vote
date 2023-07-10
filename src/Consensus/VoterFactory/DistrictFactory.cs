using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.VoterFactory;

public sealed class Districts
{
    public Districts(DistrictFactory factory, int candidateCount, int voterCount)
    {
        m_districts = factory.SelectToArray(
            d =>
            {
                var (candidates, voters) = d.Take(candidateCount, voterCount);
                return (
                    Candidates: candidates,
                    Voters: voters,
                    CandidateComparers: new CandidateComparerCollection<Voter>(
                        candidateCount, voters.SelectToArray(v => v.ProximityTo(candidates))
                            .Select(v => (Voter)v)
                            .ToCountedList()));
            });
    }
}

public sealed class DistrictFactory : IReadOnlyList<IEnumerable<VoterFactory>>
{
    private readonly IReadOnlyList<IEnumerable<VoterFactory>> m_source;

    private DistrictFactory(IReadOnlyList<IEnumerable<VoterFactory>> source)
    {
        m_source = source;
    }

    // Partitions voters into `count` districts, each with a distinct lean
    public static DistrictFactory GetDistricts(IEnumerable<VoterFactory> sourceModel, int count)
    {
        var sourceEnumerator = sourceModel.GetEnumerator();
        var disposeCount = count;

        try
        {
            return new DistrictFactory(count.LengthArray(GetDistrict));
        }
        catch
        {
            sourceEnumerator.Dispose();
            throw;
        }

        IEnumerable<VoterFactory> GetDistrict(int _)
        {
            try
            {
                while (true)
                {
                    yield return sourceEnumerator.GetNext();
                }
            }
            finally
            {
                disposeCount--;
                if (disposeCount == 0)
                {
                    sourceEnumerator.Dispose();
                }
            }
        }
    }

    public DistrictFactory Lean(double weight = .02)
    {
        return new DistrictFactory(m_source.SelectToArray(GetDistrict));

        IEnumerable<VoterFactory> GetDistrict(IEnumerable<VoterFactory> source)
        {
            using var sourceEnumerator = source.GetEnumerator();
            var lean = sourceEnumerator.GetNext();

            while (true)
                yield return sourceEnumerator.GetNext().HybridWith(lean, weight);
        }
    }

    // Keeps the "average" lean the same, but re-distributes districts
    // such that `ratio` percent of districts have the same lean
    public DistrictFactory Gerrymander(double weight = .02, double ratio = .66)
    {
        VoterFactory? lean = null;
        var cutoff = Math.Min((int) (m_source.Count * ratio), m_source.Count - 1);
        var gerrymanderRatio = (m_source.Count - cutoff) / (double) cutoff;

        return new DistrictFactory(m_source.SelectToArray(GetDistrict));

        IEnumerable<VoterFactory> GetDistrict(IEnumerable<VoterFactory> source, int index)
        {
            using var sourceEnumerator = source.GetEnumerator();
            if (lean is null)
                lean = sourceEnumerator.GetNext();
            int gerrymanderCount = 0;
            double targetGerrymanderCount = 0;

            while (true)
            {
                targetGerrymanderCount += gerrymanderRatio;
                if (index >= cutoff)
                {
                    yield return sourceEnumerator.GetNext().HybridWith(lean, weight);
                } else if (gerrymanderCount < targetGerrymanderCount)
                {
                    gerrymanderCount++;
                    yield return sourceEnumerator.GetNext().HybridWith(-1 * lean, weight);
                }
                else
                {
                    yield return sourceEnumerator.GetNext();
                }
            }
        }
    }

    public IEnumerator<IEnumerable<VoterFactory>> GetEnumerator()
    {
        return m_source.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)m_source).GetEnumerator();
    }

    public int Count => m_source.Count;

    public IEnumerable<VoterFactory> this[int index] => m_source[index];
}
