using System;
using System.Collections.Generic;
using System.Linq;

namespace Consensus.VoterFactory
{
    // Helpers for working with sets of voter factories.
    public static class Electorate
    {
        // Creates an infinite sequence of normally-distributed voters.
        public static IEnumerable<VoterFactory> Normal(int candidateCount, Random random)
        {
            while (true)
                yield return VoterFactory.Normal(candidateCount, random);
        }

        // Creates a partisan electorate wherein every voter has an opposite.
        public static IEnumerable<VoterFactory> Mirror(this IEnumerable<VoterFactory> source)
        {
            foreach (var voter in source)
            {
                yield return voter;
                yield return -1 * voter;
            }
        }
        
        public static IEnumerable<VoterFactory> CenterSqueeze(this IEnumerable<VoterFactory> source)
        {
            foreach (var voter in source)
            {
                yield return voter;
                yield return -1 * voter;
            }
        }

        // Produces cycles where each voter preference is cancelled out
        public static IEnumerable<VoterFactory> Cycle(this IEnumerable<VoterFactory> source)
        {
            return source.SelectMany<VoterFactory, VoterFactory>(v => v.CreateCycle());
        }

        // Weights an electorate based on a common "quality" factor for each candidate which spans ideology
        public static IEnumerable<VoterFactory> Quality(this IEnumerable<VoterFactory> source, Random random, double weight = .5)
        {
            using (var sourceEnumerator = source.GetEnumerator())
            {
                var current = sourceEnumerator.GetNext();

                var quality = VoterFactory.Normal(current.CandidateCount, random);

                while (true)
                {
                    yield return current.HybridWith(quality, weight);

                    current = sourceEnumerator.GetNext();
                }
            }
        }

        /*
            This creates electorates based on a Polya/Hoppe/Dirichlet model, with mutation.
            You start with an "urn" of `seedVoter` voters from `seedModel`,
            plus `alpha` "wildcard" voters. Then you draw a voter from the urn,
            clone and mutate them, and put the original and clone back into the urn.
            If you draw a "wildcard", use `seedModel` to make a new voter.
            https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L204
        */
        public static IEnumerable<VoterFactory> PolyaModel(this IEnumerable<VoterFactory> seedModel, Random random, int seedVoters = 2, int alpha = 1, double mutantFactor = .2)
        {
            if (seedModel == null)
                throw new ArgumentNullException(nameof(seedModel));
            if (seedVoters < 1)
                throw new ArgumentException(nameof(seedVoters), "Must be positive.");
            if (alpha < 0)
                throw new ArgumentException(nameof(alpha), "Must be non-negative.");

            using (var seedEnumerator = seedModel.GetEnumerator())
            {
                var urn = new List<VoterFactory>();

                for (int i = 0; i < seedVoters; i++)
                    yield return GetNext();

                while (true)
                {
                    var i = random.Next(urn.Count + alpha);

                    if (i < urn.Count)
                    {
                        var mutant = urn[i].HybridWith(VoterFactory.Normal(urn[i].CandidateCount, random), mutantFactor);
                        urn.Add(mutant);
                        yield return mutant;
                    }
                    else
                    {
                        yield return GetNext();
                    }
                }

                VoterFactory GetNext()
                {
                    var next = seedEnumerator.GetNext();

                    urn.Add(next);
                    return next;
                }
            }
        }

        // Treats each VoterFactory in the `seedModel` as a voter's "position" on each issue, rather than utility for each candidate.
        // Enumerates the full `seedModel` (make sure it's finite!), counts `candidateCount` of them as candidates at random, and
        // returns `voters` model with statisfaction for each candidate based on proximity.
        public static IEnumerable<VoterFactory> DimensionalModel(this IEnumerable<VoterFactory> seedModel, Random random, int candidateCount, int? trainingSetSize = null)
        {
            var trainingSet = new List<VoterFactory>();
            trainingSetSize ??= candidateCount;

            using (var seedEnumerator = seedModel.GetEnumerator())
            {
                while (trainingSet.Count < trainingSetSize.Value)
                {
                    trainingSet.Add(seedEnumerator.GetNext());
                }

                var candidatesIndices = new HashSet<int>();
                while (candidatesIndices.Count < candidateCount)
                    candidatesIndices.Add(random.Next(trainingSet.Count));

                var candidates = candidatesIndices.Select(i => trainingSet[i]).ToList();

                foreach (var v in trainingSet.Select(v => v.ProximityTo(candidates)))
                    yield return v;
  
                while (true)
                {
                    yield return seedEnumerator.GetNext().ProximityTo(candidates);
                }
            }
        }

        public static Districts GetDistricts(this IEnumerable<VoterFactory> source, int count) => Districts.GetDistricts(source, count);

//         // https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L230-L386
//         // Creates an elecrtorate based on n "issues" and how much each voter cares about each "issue"
      
//         public IEnumerable<VoterFactory> DimensionalModel(Func<ElectorateFactory, IEnumerable<VoterFactory>> getWeightSource, int voterCount)
//         {
//             var dimClusterDecay=(a:1, b:1);
//             var dimClusterCut = .2;
//             var wcdecay=(a:1,b:1);
//             var wccut = .2;
//             var wcalpha=1;
//             var voterClusterCaring=(a:3,b:1.5);
//             var dcs = new List<int>(); //number of dimensions in each dc
//             var dimWeights = new List<double>(); //raw importance of each dimension, regardless of dc
//             var clusterWeight = 1d;

//             while (clusterWeight > dimClusterCut)
//             {
//                 var dimWeight = clusterWeight;
//                 var dimNum = 0;
//                 while (dimWeight > wccut)
//                 {
//                     dimWeights.Add(dimWeight);
//                     dimNum++;
//                     dimWeight *= MathNet.Numerics.Distributions.Beta.Sample(m_random, wcdecay.a, wcdecay.b);
//                 };

//                 dcs.Add(dimNum);
//                 clusterWeight *= MathNet.Numerics.Distributions.Beta.Sample(m_random, dimClusterDecay.a, dimClusterDecay.b);
//             }

//             var numClusters = dcs.Count;
//             var numSubclusters = new int[numClusters];
//             var (clusters, clusterMeans, clusterCaring) = ChooseClusters(m_candidateCount + voterCount, wcalpha, () => MathNet.Numerics.Distributions.Beta.Sample(m_random, voterClusterCaring.a, voterClusterCaring.b));

//             return MakeElectorate();

//             static (object clusters, object clusterMeans, object clusterCaring) ChooseClusters(int n, int alpha, Func<double> caring)
//             {
// /*
//  self.clusters = []
//         for i in range(n):
//             item = []
//             for c in range(self.numClusters):
//                 r = (i+alpha) * random.random()
//                 if r > i:
//                     item.append(self.numSubclusters[c])
//                     self.numSubclusters[c] += 1
//                 else:
//                     item.append(self.clusters[int(r)][c])
//             self.clusters.append(item)
//         self.clusterMeans = []
//         self.clusterCaring = []
//         for c in range(self.numClusters):
//             subclusterMeans = []
//             subclusterCaring = []
//             for i in range(self.numSubclusters[c]):
//                 cares = caring()

//                 subclusterMeans.append([random.gauss(0,sqrt(cares)) for i in range(self.dcs[c])])
//                 subclusterCaring.append(caring())
//             self.clusterMeans.append(subclusterMeans)
//             self.clusterCaring.append(subclusterCaring)
// */
//                 throw new NotImplementedException();
//             }

//             IEnumerable<VoterFactory> MakeElectorate()
//             {
//                 var totalWeight = dimWeights.Select(w => w * w).Sum();
//                 var weightCount = dimWeights.Count;
//                 var candidates = getWeightSource(new ElectorateFactory(m_random, weightCount))
//                     .Take(m_candidateCount)
//                     .ToArray();
//                 /*
//         votersncands = self.baseElectorate(nvot + ncand, len(elec.dimWeights), vType)
//         elec.base = [elec.asDims(v,i) for i,v in enumerate(votersncands[:nvot])]
//         elec.cands = [elec.asDims(v,nvot+i) for i,v in enumerate(votersncands[nvot:])]
//         elec.fromDims(elec.base, vType)
//                 */
//                 throw new NotImplementedException();
//             }

//             (VoterFactory Voter, double[] Cares) AsDims(VoterFactory voter)
//             {
//             /*
// result = []
//         dim = 0
//         cares = []
//         for c in range(self.numClusters):
//             clusterMean = self.clusterMeans[c][self.clusters[i][c]]
//             for m in clusterMean:
//                 acare = self.clusterCaring[c][self.clusters[i][c]]
//                 result.append(m + (v[dim] * sqrt(1-acare)))
//                 cares.append(acare)
//             dim += 1
//         v = PersonalityVoter(result) #TODO: do personality right
//         v.cares = cares
//         return v
//             */
            
//                 throw new NotImplementedException();
//             }

//             VoterFactory FromDims()
//             {
//                 /*
//             totCaring = sum((c*w)**2 for c,w in zip(caring, e.dimWeights))
//         me = cls(-sqrt(
//             sum(((vd - cd)*w*cares)**2 for (vd, cd, w, cares) in zip(v,c,e.dimWeights,caring)) /
//                             totCaring)
//           for c in e.cands)
//         me.copyAttrsFrom(v)
//         me.dims = v
//         me.elec = e
//         return me
//                 */
//                 throw new NotImplementedException();
//             }
//         }
    }
}