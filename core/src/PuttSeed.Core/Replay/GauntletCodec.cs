using System;
using PuttSeed.Core.Daily;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Replay
{
    /// <summary>
    /// One week's gauntlet as a single shareable code. Wire format (before
    /// base64url): <c>[version:1][week:4 LE][per hole: shotCount:1][shots]</c>,
    /// seven holes in order, each shot the same four bytes a v3
    /// <see cref="ReplayCodec"/> shot uses — angle, power and the mill clock
    /// it was taken at.
    ///
    /// The seven seeds are NOT stored: they derive from the week index, the
    /// same way a daily derives from its date. A run of seven courses
    /// therefore costs barely more to share than the seven codes it replaces,
    /// and the text form keeps its own prefix so a gauntlet can never be
    /// mistaken for a single hole.
    /// </summary>
    public static class GauntletCodec
    {
        /// <summary>Wire version; bump on any format change.</summary>
        public const byte Version = 1;

        private const string Prefix = "PUTTWK-";
        private const int HeaderBytes = 5; // version 1 + week 4
        private const int BytesPerShot = 4;

        /// <summary>
        /// Encodes a whole gauntlet. Both arrays hold one entry per hole, and
        /// each hole's clocks must match its shots.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Wrong hole count, a hole over 255 shots, or clocks that do not line
        /// up with their shots.
        /// </exception>
        public static string Encode(int weekIndex, ShotInput[][] shotsPerHole, int[][] clocksPerHole)
        {
            if (shotsPerHole == null || shotsPerHole.Length != GauntletWeek.Length)
            {
                throw new ArgumentException(
                    $"A gauntlet holds exactly {GauntletWeek.Length} holes.", nameof(shotsPerHole));
            }

            if (clocksPerHole == null || clocksPerHole.Length != GauntletWeek.Length)
            {
                throw new ArgumentException(
                    "One clock list per hole is required.", nameof(clocksPerHole));
            }

            int total = HeaderBytes;
            for (int h = 0; h < GauntletWeek.Length; h++)
            {
                var shots = shotsPerHole[h] ?? Array.Empty<ShotInput>();
                if (shots.Length > 255)
                {
                    throw new ArgumentException("A hole holds at most 255 shots.", nameof(shotsPerHole));
                }

                var clocks = clocksPerHole[h] ?? Array.Empty<int>();
                if (clocks.Length != shots.Length)
                {
                    throw new ArgumentException(
                        $"Hole {h} has {shots.Length} shots but {clocks.Length} clocks.",
                        nameof(clocksPerHole));
                }

                total += 1 + shots.Length * BytesPerShot;
            }

            var payload = new byte[total];
            payload[0] = Version;
            for (int i = 0; i < 4; i++)
            {
                payload[1 + i] = (byte)(weekIndex >> (i * 8));
            }

            int at = HeaderBytes;
            for (int h = 0; h < GauntletWeek.Length; h++)
            {
                var shots = shotsPerHole[h] ?? Array.Empty<ShotInput>();
                var clocks = clocksPerHole[h] ?? Array.Empty<int>();
                payload[at++] = (byte)shots.Length;
                for (int i = 0; i < shots.Length; i++)
                {
                    int packed = shots[i].AngleIndex
                        | (shots[i].PowerIndex << 10)
                        | ((clocks[i] & 0x3FF) << 18);
                    payload[at++] = (byte)packed;
                    payload[at++] = (byte)(packed >> 8);
                    payload[at++] = (byte)(packed >> 16);
                    payload[at++] = (byte)(packed >> 24);
                }
            }

            return Prefix + ToBase64Url(payload);
        }

        /// <summary>
        /// Decodes a gauntlet code. Returns false on any malformed input
        /// without throwing, exactly like <see cref="ReplayCodec"/>.
        /// </summary>
        public static bool TryDecode(string code, out int weekIndex,
            out ShotInput[][] shotsPerHole, out int[][] clocksPerHole)
        {
            weekIndex = 0;
            shotsPerHole = Array.Empty<ShotInput[]>();
            clocksPerHole = Array.Empty<int[]>();

            if (code == null || !code.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            byte[] payload;
            try
            {
                payload = FromBase64Url(code.Substring(Prefix.Length));
            }
            catch (FormatException)
            {
                return false;
            }

            if (payload.Length < HeaderBytes || payload[0] != Version)
            {
                return false;
            }

            int week = 0;
            for (int i = 0; i < 4; i++)
            {
                week |= payload[1 + i] << (i * 8);
            }

            var shots = new ShotInput[GauntletWeek.Length][];
            var clocks = new int[GauntletWeek.Length][];
            int at = HeaderBytes;
            for (int h = 0; h < GauntletWeek.Length; h++)
            {
                if (at >= payload.Length)
                {
                    return false;
                }

                int count = payload[at++];
                if (at + count * BytesPerShot > payload.Length)
                {
                    return false;
                }

                shots[h] = new ShotInput[count];
                clocks[h] = new int[count];
                for (int i = 0; i < count; i++)
                {
                    int packed = payload[at]
                        | (payload[at + 1] << 8)
                        | (payload[at + 2] << 16)
                        | (payload[at + 3] << 24);
                    at += BytesPerShot;
                    shots[h][i] = new ShotInput(packed & 0x3FF, (packed >> 10) & 0xFF);
                    clocks[h][i] = (packed >> 18) & 0x3FF;
                }
            }

            if (at != payload.Length)
            {
                return false; // trailing bytes mean a corrupt code, not a longer one
            }

            weekIndex = week;
            shotsPerHole = shots;
            clocksPerHole = clocks;
            return true;
        }

        private static string ToBase64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        private static byte[] FromBase64Url(string text)
        {
            var s = text.Replace('-', '+').Replace('_', '/');
            int pad = (4 - s.Length % 4) % 4;
            if (pad == 3)
            {
                throw new FormatException("Invalid base64url length.");
            }

            return Convert.FromBase64String(s + new string('=', pad));
        }
    }
}
