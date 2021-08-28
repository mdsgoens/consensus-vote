using System.Collections.Generic;
using System.Linq;
using System;

namespace Consensus
{
    public abstract class CandidateComparer : IEquatable<CandidateComparer>, IComparer<int>
    {
        protected CandidateComparer()
        {
        }

        protected CandidateComparer(List<List<int>> ranking)
        {
            m_ranking = ranking;
        }

        public override int GetHashCode() => m_hashCode ??= Enumerable.Range(0, CandidateCount).Aggregate(0x2D2816FE, (res, c) => res * 31 + CandidateValue(c).GetHashCode());

        public int Compare(int first, int second) => CandidateValue(first) - CandidateValue(second);

        public bool Prefers(int first, int second) => CandidateValue(first) > CandidateValue(second);
   
        public List<List<int>> Ranking => m_ranking ?? GetRankingCore();

        public override bool Equals(object other) => Equals(other as CandidateComparer);
        public static bool operator==(CandidateComparer first, CandidateComparer second) => first is null ? second is null : first.Equals(second);
        public static bool operator!=(CandidateComparer first, CandidateComparer second) => !(first == second);

        public bool Equals(CandidateComparer other)
        {
            if (other?.CandidateCount != CandidateCount || other.GetType() != this.GetType())
                return false;
                
            for (int i = 0; i < CandidateCount; i++)
            {
                if (other.CandidateValue(i) != CandidateValue(i))
                    return false;
            }

            return true;
        }   

        public abstract int CandidateCount { get; }
        protected abstract int CandidateValue(int candidate);
        protected virtual List<List<int>> GetRankingCore()
        {
            var ranking = new List<List<int>>();
            
            ranking.Add(new List<int> { 0 });

            for (int index = 1; index < CandidateCount; index++)
            {
                var value = CandidateValue(index);
                var insertIndex = ranking.FindIndex(bucket => CandidateValue(bucket[0]) <= value);

                if (insertIndex == -1)
                {
                    ranking.Add(new List<int> { index });
                }
                else if (CandidateValue(ranking[insertIndex][0]) == value)
                {
                    ranking[insertIndex].Add(index);
                }
                else
                {
                    ranking.Insert(insertIndex, new List<int> { index });
                }
            }

            return ranking;
        }

        private int? m_hashCode;
        private List<List<int>> m_ranking;
    }
}
