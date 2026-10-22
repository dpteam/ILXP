using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ILXP
{
    /// <summary>
    /// Maps rendered IL text lines to IL offsets (and back).
    ///
    /// The IL writer labels every instruction with <c>L_xxxx:</c>, so the map is recovered
    /// from the text itself: no extra bookkeeping in the writers, and it also works for IL
    /// that was pasted or loaded from a file.
    /// </summary>
    internal sealed class IlLineMap
    {
        private static readonly Regex LabelPattern = new Regex(
            @"^\s*L_([0-9a-fA-F]{4,8}):",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly Dictionary<int, int> lineToOffset = new Dictionary<int, int>();
        private readonly Dictionary<int, int> offsetToLine = new Dictionary<int, int>();
        private readonly List<int> sortedOffsets = new List<int>();

        private IlLineMap()
        {
        }

        public int Count
        {
            get { return lineToOffset.Count; }
        }

        public bool IsEmpty
        {
            get { return lineToOffset.Count == 0; }
        }

        public static IlLineMap Parse(string text)
        {
            IlLineMap map = new IlLineMap();
            if (string.IsNullOrEmpty(text))
            {
                return map;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Match match = LabelPattern.Match(lines[i]);
                if (!match.Success)
                {
                    continue;
                }

                int offset;
                if (!int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset))
                {
                    continue;
                }

                map.lineToOffset[i] = offset;
                if (!map.offsetToLine.ContainsKey(offset))
                {
                    map.offsetToLine[offset] = i;
                    map.sortedOffsets.Add(offset);
                }
            }

            map.sortedOffsets.Sort();
            return map;
        }

        /// <summary>Offset of the instruction on the given line, if any.</summary>
        public bool TryGetOffset(int line, out int offset)
        {
            return lineToOffset.TryGetValue(line, out offset);
        }

        /// <summary>Line of the instruction at exactly this offset.</summary>
        public bool TryGetLine(int offset, out int line)
        {
            return offsetToLine.TryGetValue(offset, out line);
        }

        /// <summary>
        /// Line of the instruction at <paramref name="offset"/>, or of the closest instruction
        /// before it. Returns -1 when the map is empty.
        /// </summary>
        public int FindLine(int offset)
        {
            int line;
            if (offsetToLine.TryGetValue(offset, out line))
            {
                return line;
            }

            int bestOffset = -1;
            foreach (int candidate in sortedOffsets)
            {
                if (candidate > offset)
                {
                    break;
                }
                bestOffset = candidate;
            }

            if (bestOffset >= 0)
            {
                return offsetToLine[bestOffset];
            }

            return sortedOffsets.Count > 0 ? offsetToLine[sortedOffsets[0]] : -1;
        }

        /// <summary>First line of the map (used for diagnostics).</summary>
        public int FirstLine
        {
            get
            {
                int best = int.MaxValue;
                foreach (int line in lineToOffset.Keys)
                {
                    if (line < best)
                    {
                        best = line;
                    }
                }
                return best == int.MaxValue ? -1 : best;
            }
        }

        public IEnumerable<KeyValuePair<int, int>> Entries
        {
            get
            {
                List<KeyValuePair<int, int>> entries = new List<KeyValuePair<int, int>>(lineToOffset);
                entries.Sort(delegate(KeyValuePair<int, int> a, KeyValuePair<int, int> b) { return a.Key.CompareTo(b.Key); });
                return entries;
            }
        }
    }
}
