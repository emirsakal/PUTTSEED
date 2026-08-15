namespace PuttSeed.Core.FixedMath
{
    /// <summary>
    /// Deterministic xorshift128 PRNG (Marsaglia 2003) — the ONLY randomness
    /// source allowed in PuttSeed.Core. The 128-bit state is seeded from a
    /// 64-bit seed via two SplitMix64 outputs so that similar seeds still
    /// produce well-mixed, unrelated streams.
    /// </summary>
    public sealed class FixRng
    {
        private uint _x;
        private uint _y;
        private uint _z;
        private uint _w;

        /// <summary>Creates a generator whose stream is fully determined by <paramref name="seed"/>.</summary>
        public FixRng(ulong seed)
        {
            ulong s = seed;
            ulong a = SplitMix64(ref s);
            ulong b = SplitMix64(ref s);
            _x = (uint)a;
            _y = (uint)(a >> 32);
            _z = (uint)b;
            _w = (uint)(b >> 32);

            // xorshift must never sit in the all-zero fixed point.
            if ((_x | _y | _z | _w) == 0)
            {
                _w = 1;
            }
        }

        /// <summary>Next 32 uniformly distributed bits.</summary>
        public uint NextUInt()
        {
            unchecked
            {
                uint t = _x ^ (_x << 11);
                _x = _y;
                _y = _z;
                _z = _w;
                _w = _w ^ (_w >> 19) ^ t ^ (t >> 8);
                return _w;
            }
        }

        /// <summary>
        /// Uniform integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>)
        /// using Lemire's multiply-shift mapping (no modulo bias pattern, fully deterministic).
        /// </summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            uint range = (uint)(maxExclusive - minInclusive);
            uint scaled = (uint)(((ulong)NextUInt() * range) >> 32);
            return minInclusive + (int)scaled;
        }

        /// <summary>
        /// Uniform fixed-point value in [0, 1): the next 32 output bits placed
        /// directly into the fractional part.
        /// </summary>
        public Fix64 NextFix01() => Fix64.FromRaw(NextUInt());

        /// <summary>SplitMix64 mixing step; also used by DailySeed derivation.</summary>
        public static ulong SplitMix64(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
