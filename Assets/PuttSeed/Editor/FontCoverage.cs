#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Reads a font file's character map and answers which characters the font
    /// can actually print.
    ///
    /// Unity's own Font.HasCharacter is no help: on a DYNAMIC font it answers
    /// yes to everything, because the question it really answers is "will the
    /// rasterizer produce something" — OS fallback included. A font with no
    /// Turkish in it passes that check and then draws boxes on a player's
    /// screen. This was not a guess; a font known to be missing five Turkish
    /// letters passed a test built on HasCharacter. The file does not lie, so
    /// this reads the file: the cmap table, formats 4 and 12, which is every
    /// modern desktop font.
    /// </summary>
    public static class FontCoverage
    {
        /// <summary>
        /// The characters this font cannot print, in the order given. An empty
        /// list means every one of them has a glyph. A font with no file behind
        /// it — Unity's built-in face — reports nothing missing, because there
        /// is nothing to read and a false alarm is worse than no alarm.
        /// </summary>
        public static List<char> Missing(Font font, IEnumerable<char> characters)
        {
            var missing = new List<char>();
            string path = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return missing;
            }

            byte[] file = File.ReadAllBytes(path);
            int subtable = FindCmapSubtable(file);
            if (subtable < 0)
            {
                return missing;
            }

            foreach (char c in characters)
            {
                if (!Covers(file, subtable, c))
                {
                    missing.Add(c);
                }
            }

            return missing;
        }

        /// <summary>
        /// Locates the best character map in the file: full Unicode first,
        /// then the Windows BMP map, then anything. Returns -1 when the file
        /// has no cmap at all.
        /// </summary>
        private static int FindCmapSubtable(byte[] b)
        {
            if (b.Length < 12)
            {
                return -1;
            }

            int cmap = -1;
            int tables = U16(b, 4);
            for (int i = 0; i < tables; i++)
            {
                int record = 12 + 16 * i;
                if (record + 16 > b.Length)
                {
                    return -1;
                }

                if (b[record] == 'c' && b[record + 1] == 'm' && b[record + 2] == 'a' && b[record + 3] == 'p')
                {
                    cmap = (int)U32(b, record + 8);
                    break;
                }
            }

            if (cmap < 0 || cmap + 4 > b.Length)
            {
                return -1;
            }

            int best = -1;
            int bestRank = -1;
            int count = U16(b, cmap + 2);
            for (int i = 0; i < count; i++)
            {
                int record = cmap + 4 + 8 * i;
                if (record + 8 > b.Length)
                {
                    break;
                }

                int platform = U16(b, record);
                int encoding = U16(b, record + 2);
                int rank = platform == 3 && encoding == 10 ? 3
                    : platform == 3 && encoding == 1 ? 2
                    : platform == 0 ? 1 : 0;
                if (rank > bestRank)
                {
                    bestRank = rank;
                    best = cmap + (int)U32(b, record + 4);
                }
            }

            return best < b.Length ? best : -1;
        }

        private static bool Covers(byte[] b, int sub, int codepoint)
        {
            int format = U16(b, sub);
            if (format == 4)
            {
                int segments = U16(b, sub + 6) / 2;
                int ends = sub + 14;
                int starts = ends + segments * 2 + 2;
                int deltas = starts + segments * 2;
                int rangeOffsets = deltas + segments * 2;
                for (int i = 0; i < segments; i++)
                {
                    if (U16(b, ends + i * 2) < codepoint)
                    {
                        continue;
                    }

                    if (U16(b, starts + i * 2) > codepoint)
                    {
                        return false; // segments are sorted: past it, so absent
                    }

                    int rangeOffset = U16(b, rangeOffsets + i * 2);
                    if (rangeOffset == 0)
                    {
                        return ((codepoint + S16(b, deltas + i * 2)) & 0xFFFF) != 0;
                    }

                    int glyphAt = rangeOffsets + i * 2 + rangeOffset
                        + (codepoint - U16(b, starts + i * 2)) * 2;
                    return glyphAt + 1 < b.Length && U16(b, glyphAt) != 0;
                }

                return false;
            }

            if (format == 12)
            {
                int groups = (int)U32(b, sub + 12);
                for (int i = 0; i < groups; i++)
                {
                    int group = sub + 16 + i * 12;
                    if (group + 12 > b.Length || codepoint < U32(b, group))
                    {
                        return false; // groups are sorted too
                    }

                    if (codepoint <= U32(b, group + 4))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true; // a format nobody uses any more: do not invent a failure
        }

        private static ushort U16(byte[] b, int at) => (ushort)((b[at] << 8) | b[at + 1]);

        private static short S16(byte[] b, int at) => (short)U16(b, at);

        private static uint U32(byte[] b, int at)
            => ((uint)b[at] << 24) | ((uint)b[at + 1] << 16) | ((uint)b[at + 2] << 8) | b[at + 3];
    }
}
