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
    /// The version byte doubles as the GENERATOR config version: a version-N
    /// code regenerates its course with <c>GeneratorConfig.ForVersion(N)</c>.
    /// The byte layout is identical across versions; only the regeneration
    /// config differs (v2 = the 2026-08 element wave).
    /// </summary>
    public static class ReplayCodec
    {
        /// <summary>Newest wire/config version this codec emits and accepts.</summary>
        public const byte Version = 2;

        private const string Prefix = "PUTT-";
        private const int HeaderBytes = 10; // version 1 + seed 8 + count 1
        private const int BytesPerShot = 3;

        /// <summary>
        /// Encodes a seed and shot list into a shareable code.
        /// <paramref name="configVersion"/> is the generator config the course
        /// was played under; it defaults to 1 so legacy call sites keep
        /// producing codes that decode identically everywhere.
        /// </summary>
        /// <exception cref="ArgumentException">More than 255 shots, or an unknown config version.</exception>
        public static string Encode(ulong seed, ShotInput[] shots, int configVersion = 1)
        {
            if (shots.Length > 255)
            {
                throw new ArgumentException("A replay holds at most 255 shots.", nameof(shots));
            }

            if (configVersion < 1 || configVersion > Version)
            {
                throw new ArgumentException($"Unknown config version {configVersion}.", nameof(configVersion));
            }

            var payload = new byte[HeaderBytes + shots.Length * BytesPerShot];
            payload[0] = (byte)configVersion;
            for (int i = 0; i < 8; i++)
            {
                payload[1 + i] = (byte)(seed >> (i * 8));
            }

            payload[9] = (byte)shots.Length;
            for (int i = 0; i < shots.Length; i++)
            {
                int packed = shots[i].AngleIndex | (shots[i].PowerIndex << 10);
                int at = HeaderBytes + i * BytesPerShot;
                payload[at] = (byte)packed;
                payload[at + 1] = (byte)(packed >> 8);
                payload[at + 2] = (byte)(packed >> 16);
            }

            return Prefix + ToBase64Url(payload);
        }

        /// <summary>Version-blind decode for call sites that regenerate v1-only content.</summary>
        public static bool TryDecode(string code, out ulong seed, out ShotInput[] shots)
            => TryDecode(code, out seed, out shots, out _);

        /// <summary>
        /// Decodes a code produced by <see cref="Encode"/>;
        /// <paramref name="configVersion"/> is the generator config version the
        /// course must regenerate with. Returns false on any malformed input
        /// (wrong prefix, bad base64, unknown version, truncated or oversized
        /// payload) without throwing.
        /// </summary>
        public static bool TryDecode(string code, out ulong seed, out ShotInput[] shots, out int configVersion)
        {
            seed = 0;
            shots = Array.Empty<ShotInput>();
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

            int count = payload[9];
            if (payload.Length != HeaderBytes + count * BytesPerShot)
            {
                return false;
            }

            ulong s = 0;
            for (int i = 0; i < 8; i++)
            {
                s |= (ulong)payload[1 + i] << (i * 8);
            }

            var result = new ShotInput[count];
            for (int i = 0; i < count; i++)
            {
                int at = HeaderBytes + i * BytesPerShot;
                int packed = payload[at] | (payload[at + 1] << 8) | (payload[at + 2] << 16);
                result[i] = new ShotInput(packed & 0x3FF, (packed >> 10) & 0xFF);
            }

            seed = s;
            shots = result;
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
