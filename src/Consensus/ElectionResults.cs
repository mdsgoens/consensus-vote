using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Consensus
{
    public sealed class ElectionResults
    {
        public ElectionResults(List<List<int>> ranking)
        {
            Ranking = ranking;
            m_stringBuilder = new StringBuilder();

            AddValue("Winner", ranking[0]);
        }

        public int Winner => Ranking[0][0];
        public List<int> Winners => Ranking[0];

        public List<List<int>> Ranking { get; }

        public string Details => m_stringBuilder.ToString();

        public void AddHeading(string heading)
            => m_stringBuilder.AppendLine().Append("# ").AppendLine(heading);

            
        public void AddValue(string heading, Value cell)
            => m_stringBuilder.Append(heading).Append(": ").Append(cell.Display).AppendLine();

        public void AddCandidateTable(int[] table)
        {
            AddTable(table.IndexOrderByDescending()
                .Select(c => new Value[] {
                    (Candidate) c,
                    (Count) table[c]
            }));
        }

        public void AddCandidateTable(IEnumerable<(int Candidate, int Value)> table)
        {
            AddTable(table.Select(row => new Value[] {
                (Candidate) row.Candidate,
                (Count) row.Value
            }));
        }

        public void AddTable(IEnumerable<Value[]> body, params Value[] headings) => AppendTable(m_stringBuilder, body, headings);

        public static void AppendTable(StringBuilder sb, IEnumerable<Value[]> body, params Value[] headings)
        {
            var table = body.ToList();

            if (!table.Any())
            {
                sb.AppendLine("None");
                return;
            }

            var columnCount = table.Max(row => row.Length);
            var padRight = new bool[columnCount];
            var padding = new int[columnCount];

            foreach (var row in table)
            {
                for (var i = 0; i < columnCount; i++)
                {
                    if (row[i].IsRightJustified)
                        padRight[i] = true;

                    if (row[i].Display.Length > padding[i])
                        padding[i] = row[i].Display.Length;
                }
            }           
            
            if (headings.Length == columnCount - 1)
                headings = headings.Prepend("").ToArray();

            if (headings.Length == columnCount)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    if (headings[i].Display.Length > padding[i])
                        padding[i] = headings[i].Display.Length;
                }

                WriteRow(headings);
                
                sb.Append('|');
                for (int i = 0; i < columnCount; i++)
                {
                    sb.Append(new string ((padding[i] + 2).LengthArray(_ => '-')));
                    sb.Append('|');
                }
                sb.AppendLine();
            }
      
            foreach (var row in table)
                WriteRow(row);

            void WriteRow(Value[] row)
            {
                sb.Append("| ");

                for (int i = 0; i < row.Length; i++)
                {
                    var value = row[i].Display;

                    sb.Append(padRight[i]
                        ? value.PadRight(padding[i])
                        : value.PadLeft(padding[i]));

                    sb.Append(" |");

                    if (i + 1 < row.Length)
                        sb.Append(' ');
                }

                sb.AppendLine();
            }
        }

        public void AddCandidatetMatrix(int candidateCount, Func<int, int, Value> getValue)
        {
            var candidates = Enumerable.Range(0, candidateCount);

            AddTable(
                candidates.Select(row =>
                    candidates.Select(col => getValue(row, col))
                    .Prepend((Candidate) row)
                    .ToArray()),
                candidateCount.LengthArray(c => (Candidate) c)
            );
        }

        public class Value
        {
            public string Display { get; protected set; }
            public virtual bool IsRightJustified { get; } = true;

            public override string ToString() => Display;

            public static implicit operator Value(string display) => new Value { Display = display };
            public static implicit operator Value(int count) => new Count { Display = (count == 0 ? "" : count.ToString()) };
            public static implicit operator Value(int? count) => new Count { Display = (count == null ? "" : count.ToString()) };
            public static implicit operator Value(ulong coalition) => VotingMethodBase.GetCandidates(coalition).ToList();
            public static implicit operator Value(List<int> candidates) => new Value { Display = string.Join(", ", candidates.Select(ParsingUtility.EncodeCandidateIndex)) };
        }

        public sealed class Candidate : Value
        {
            public static implicit operator Candidate(int candidate) => new Candidate { Display = new string(new [] { ParsingUtility.EncodeCandidateIndex(candidate) }) };
        }

        public sealed class Count : Value
        {
            public override bool IsRightJustified { get; } = false;
        }

        private readonly StringBuilder m_stringBuilder;
    }
}