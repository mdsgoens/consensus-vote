using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Consensus
{
    public static class ParsingUtility
    {
        public static string Join(this IEnumerable<string> values, string separator) => string.Join(separator, values);

        public static int NumberOfCandidates(string source) => DecodeCandidateIndex(source.Where(c => c >= 'a' && c <= 'z').Max()) + 1;

        public static int DecodeCandidateIndex(char candidate) => (int) (candidate - 'a');
        public static char EncodeCandidateIndex(int candidateIndex) => (char) (candidateIndex + 'a');
        public static string EncodeCandidates(IEnumerable<int> candidates) => new string(candidates.OrderBy(x => x).Select(EncodeCandidateIndex).ToArray());

    }
}