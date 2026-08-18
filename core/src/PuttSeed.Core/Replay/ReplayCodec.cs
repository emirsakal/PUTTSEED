using System;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Replay
{
    /// <summary>
    /// Encodes a run as a short shareable code and back. Wire format (before
    /// base64url): <c>[version:1][seed:8 LE][shotCount:1][shots: count x 3]</c>,
    /// each shot packing <c>angle | power &lt;&lt; 10</c> into 24 little-endian
    /// bits (10-bit angle, 8-bit power, 6 zero bits). The text form is
    /// <c>PUTT-</c> + unpadded base64url. Because the sim is deterministic,
    /// replaying the decoded shots on the seed's course reproduces the exact
    /// run; a desync is a determinism bug by definition.
    ///
    /// The version byte picks BOTH the generator config a course regenerates
    /// with and the shot layout: v1 is generator V1 with 3-byte shots, v2 is
    /// generator V2 (the 2026-08 element wave) with 3-byte shots, and v3 is
    /// generator V2 with 4-byte shots that also carry each shot's mill clock.
    ///
    /// Timing joined the format when windmills started turning while the ball
    /// rests: the blade angle a shot launches into is part of the physics, so
    /// a replay knowing only angle and power would desync. Courses without
    /// mills ignore the value; it is stored anyway so one layout covers every
    /// v2 course.
    /// </summary>
    public static class ReplayCodec
    {
        /// <summary>Newest wire/config version this codec emits and accepts.</summary>
        public const byte Version = 3;

        private const string Prefix = "PUTT-";
        private const int HeaderBytes = 10; // version 1 + seed 8 + count 1

        /// <summary>Shot width in bytes for a wire version.</summary>
        private static int ShotBytes(int version) => version >= 3 ? 4 : 3;

        /// <summary>
        /// Encodes a seed and shot list into a shareable code.
        /// <paramref name="configVersion"/> is the generator config the course
        /// was played under; it defaults to 1 so legacy call sites keep
        /// producing codes that decode identically everywhere.
        /// </summary>
        /// <exception cref="ArgumentException">More than 255 shots, or an unknown config version.</exception>
        public static string Encode(ulong seed, ShotInput[] shots, int configVersion = 1)
            => Encode(seed, shots, null, configVersion);

        /// <summary>
        /// Encodes a run with each shot's mill clock — the moment it was taken.
        /// Pass null clocks for the untimed versions; version 3 requires one
        /// entry per shot.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// More than 255 shots, an unknown version, or a clock list that does
        /// not match the shots.
        /// </exception>
        public static string Encode(ulong seed, ShotInput[] shots, int[]? shotClocks, int configVersion)
        {
            if (shots.Length > 255)
            {
                throw new ArgumentException("A replay holds at most 255 shots.", nameof(shots));
            }

            if (configVersion < 1 || configVersion > Version)
            {
                throw new ArgumentException($"Unknown config version {configVersion}.", nameof(configVersion));
            }

            bool timed = configVersion >= 3;
            if (timed && (shotClocks == null || shotClocks.Length != shots.Length))
            {
                throw new ArgumentException(
                    "Version 3 needs one mill clock per shot.", nameof(shotClocks));
            }

            int shotBytes = ShotBytes(configVersion);
            var payload = new byte[HeaderBytes + shots.Length * shotBytes];
            payload[0] = (byte)configVersion;
            for (int i = 0; i < 8; i++)
            {
                payload[1 + i] = (byte)(seed >> (i * 8));
            }

            payload[9] = (byte)shots.Length;
            for (int i = 0; i < shots.Length; i++)
            {
                int packed = shots[i].AngleIndex | (shots[i].PowerIndex << 10);
                if (timed)
                {
                    packed |= (shotClocks![i] & 0x3FF) << 18;
                }

                int at = HeaderBytes + i * shotBytes;
                payload[at] = (byte)packed;
                payload[at + 1] = (byte)(packed >> 8);
                payload[at + 2] = (byte)(packed >> 16);
                if (timed)
                {
                    payload[at + 3] = (byte)(packed >> 24);
                }
            }

            return Prefix + ToBase64Url(payload);
        }

        /// <summary>Version-blind decode for call sites that regenerate v1-only content.</summary>
        public static bool TryDecode(string code, out ulong seed, out ShotInput[] shots)
            => TryDecode(code, out seed, out shots, out _);

        /// <summary>Decode without the per-shot clocks.</summary>
        public static bool TryDecode(string code, out ulong seed, out ShotInput[] shots,
            out int configVersion)
            => TryDecode(code, out seed, out shots, out configVersion, out _);

        /// <summary>
        /// Decodes a code produced by <c>Encode</c>;
        /// <paramref name="configVersion"/> is the generator config version the
        /// course must regenerate with. Returns false on any malformed input
        /// (wrong prefix, bad base64, unknown version, truncated or oversized
        /// payload) without throwing.
        /// </summary>
        public static bool TryDecode(string code, out ulong seed, out ShotInput[] shots,
            out int configVersion, out int[] shotClocks)
        {
            seed = 0;
            shots = Array.Empty<ShotInput>();
            shotClocks = Array.Empty<int>();
            configVersion = 0;

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

            if (payload.Length < HeaderBytes || payload[0] < 1 || payload[0] > Version)
            {
                return false;
            }

            configVersion = payload[0];

            int shotBytes = ShotBytes(configVersion);
            int count = payload[9];
            if (payload.Length != HeaderBytes + count * shotBytes)
            {
                return false;
            }

            ulong s = 0;
            for (int i = 0; i < 8; i++)
            {
                s |= (ulong)payload[1 + i] << (i * 8);
            }

            var result = new ShotInput[count];
            var clocks = new int[count];
            for (int i = 0; i < count; i++)
            {
                int at = HeaderBytes + i * shotBytes;
                int packed = payload[at] | (payload[at + 1] << 8) | (payload[at + 2] << 16);
                if (shotBytes == 4)
                {
                    packed |= payload[at + 3] << 24;
                    clocks[i] = (packed >> 18) & 0x3FF;
                }

                result[i] = new ShotInput(packed & 0x3FF, (packed >> 10) & 0xFF);
            }

            seed = s;
            shots = result;
            shotClocks = clocks;
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
