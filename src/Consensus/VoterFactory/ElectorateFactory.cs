using System;
using System.Collections.Generic;
using System.Linq;
using Consensus.VoterModels;

namespace Consensus.Models
{
    public sealed class ElectorateFactory
    {
        public ElectorateFactory(Random random, int candidateCount)
        {
            m_random = random;
            m_candidateCount = candidateCount;
        }

        
        private VoterFactory RandomNormalVoter()
        {
           return VoterFactory.Normal(m_candidateCount, m_random);
        }

        // Creates an infinite sequence of normally-distributed voters.
        public IEnumerable<VoterFactory> NormalElectorate()
        {
            while (true)
                yield return RandomNormalVoter();
        }

        // Creates a partisan electorate wherein every voter has an opposite.
        public IEnumerable<VoterFactory> Mirror(IEnumerable<VoterFactory> source)
        {
            foreach (var voter in source)
            {
                yield return voter;
                yield return -1 * voter;
            }
        }

        // Weights an electorate based on a common "quality" factor for each candidate which spans ideology
        public IEnumerable<VoterFactory> Quality(IEnumerable<VoterFactory> source, double weight = .5)
        {
            var quality = RandomNormalVoter();
            return source.Select(s => s.HybridWith(quality, weight));
        }

        /*
            This creates electorates based on a Polya/Hoppe/Dirichlet model, with mutation.
            You start with an "urn" of `seedVoter` voters from `seedModel`,
            plus `alpha` "wildcard" voters. Then you draw a voter from the urn,
            clone and mutate them, and put the original and clone back into the urn.
            If you draw a "wildcard", use `seedModel` to make a new voter.
            https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L204
        */
        public IEnumerable<VoterFactory> PolyaModel(IEnumerable<VoterFactory> seedModel, int seedVoters = 2, int alpha = 1, double mutantFactor = .2)
        {
            if (seedModel == null)
                throw new ArgumentNullException(nameof(seedModel));
            if (seedVoters < 1)
                throw new ArgumentException(nameof(seedVoters), "Must be positive.");
            if (alpha < 1)
                throw new ArgumentException(nameof(alpha), "Must be positive.");

            using (var seedEnumerator = seedModel.GetEnumerator())
            {
                var urn = new List<VoterFactory>();

                for (int i = 0; i < seedVoters; i++)
                    yield return GetNext();

                while (true)
                {
                    var i = m_random.Next(urn.Count + alpha);

                    if (i < urn.Count)
                    {
                        var mutant = urn[i].HybridWith(RandomNormalVoter(), mutantFactor);
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
                    if (!seedEnumerator.MoveNext())
                        throw new InvalidOperationException("Expected an infinite enumerable.");

                    urn.Add(seedEnumerator.Current);
                    return seedEnumerator.Current;
                }
            }
        }

        // https://github.com/electionscience/vse-sim/blob/1d7e48f639fd5ffcf84883dce0873aa7d6fa6794/voterModels.py#L230-L386
        // Creates an elecrtorate based on n "issues" and how much each voter cares about each "issue"
      
        public IEnumerable<VoterFactory> DimensionalModel(Func<ElectorateFactory, IEnumerable<VoterFactory>> getWeightSource, int voterCount)
        {
            var dimClusterDecay=(a:1, b:1);
            var dimClusterCut = .2;
            var wcdecay=(a:1,b:1);
            var wccut = .2;
            var wcalpha=1;
            var voterClusterCaring=(a:3,b:1.5);
            var dcs = new List<int>(); //number of dimensions in each dc
            var dimWeights = new List<double>(); //raw importance of each dimension, regardless of dc
            var clusterWeight = 1d;

            while (clusterWeight > dimClusterCut)
            {
                var dimWeight = clusterWeight;
                var dimNum = 0;
                while (dimWeight > wccut)
                {
                    dimWeights.Add(dimWeight);
                    dimNum++;
                    dimWeight *= MathNet.Numerics.Distributions.Beta.Sample(m_random, wcdecay.a, wcdecay.b);
                };

                dcs.Add(dimNum);
                clusterWeight *= MathNet.Numerics.Distributions.Beta.Sample(m_random, dimClusterDecay.a, dimClusterDecay.b);
            }

            var numClusters = dcs.Count;
            var numSubclusters = new int[numClusters];
            var (clusters, clusterMeans, clusterCaring) = ChooseClusters(m_candidateCount + voterCount, wcalpha, () => MathNet.Numerics.Distributions.Beta.Sample(m_random, voterClusterCaring.a, voterClusterCaring.b));

            return MakeElectorate();

            static (object clusters, object clusterMeans, object clusterCaring) ChooseClusters(int n, int alpha, Func<double> caring)
            {
/*
 self.clusters = []
        for i in range(n):
            item = []
            for c in range(self.numClusters):
                r = (i+alpha) * random.random()
                if r > i:
                    item.append(self.numSubclusters[c])
                    self.numSubclusters[c] += 1
                else:
                    item.append(self.clusters[int(r)][c])
            self.clusters.append(item)
        self.clusterMeans = []
        self.clusterCaring = []
        for c in range(self.numClusters):
            subclusterMeans = []
            subclusterCaring = []
            for i in range(self.numSubclusters[c]):
                cares = caring()

                subclusterMeans.append([random.gauss(0,sqrt(cares)) for i in range(self.dcs[c])])
                subclusterCaring.append(caring())
            self.clusterMeans.append(subclusterMeans)
            self.clusterCaring.append(subclusterCaring)
*/
                throw new NotImplementedException();
            }

            IEnumerable<VoterFactory> MakeElectorate()
            {
                var totalWeight = dimWeights.Select(w => w * w).Sum();
                var weightCount = dimWeights.Count;
                var candidates = getWeightSource(new ElectorateFactory(m_random, weightCount))
                    .Take(m_candidateCount)
                    .ToArray();
                /*
        votersncands = self.baseElectorate(nvot + ncand, len(elec.dimWeights), vType)
        elec.base = [elec.asDims(v,i) for i,v in enumerate(votersncands[:nvot])]
        elec.cands = [elec.asDims(v,nvot+i) for i,v in enumerate(votersncands[nvot:])]
        elec.fromDims(elec.base, vType)
                */
                throw new NotImplementedException();
            }

            (VoterFactory Voter, double[] Cares) AsDims(VoterFactory voter)
            {
            /*
result = []
        dim = 0
        cares = []
        for c in range(self.numClusters):
            clusterMean = self.clusterMeans[c][self.clusters[i][c]]
            for m in clusterMean:
                acare = self.clusterCaring[c][self.clusters[i][c]]
                result.append(m + (v[dim] * sqrt(1-acare)))
                cares.append(acare)
            dim += 1
        v = PersonalityVoter(result) #TODO: do personality right
        v.cares = cares
        return v
            */
            
                throw new NotImplementedException();
            }

            VoterFactory FromDims()
            {
                /*
            totCaring = sum((c*w)**2 for c,w in zip(caring, e.dimWeights))
        me = cls(-sqrt(
            sum(((vd - cd)*w*cares)**2 for (vd, cd, w, cares) in zip(v,c,e.dimWeights,caring)) /
                            totCaring)
          for c in e.cands)
        me.copyAttrsFrom(v)
        me.dims = v
        me.elec = e
        return me
                */
                throw new NotImplementedException();
            }
        }
        

        private readonly Random m_random;
        private readonly int m_candidateCount;
    }
}