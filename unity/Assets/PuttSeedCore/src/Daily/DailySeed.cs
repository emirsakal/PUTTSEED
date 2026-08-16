using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Daily
{
    /// <summary>
    /// Derives the daily course seed from a UTC calendar date — computed
    /// locally on every device, no backend. The date arrives as integers (the
    /// caller reads the clock; core never touches system time):
    /// <c>seed = SplitMix64(FNV1a64(SALT + "yyyyMMdd"))</c>.
    /// The derivation is a cross-device contract; changing it (or the salt)
    /// changes every future daily course.
    /// </summary>
    public static class DailySeed
    {
        private const string Salt = "PUTTSEED-v1";
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        /// <summary>Seed for the given UTC date (year, month 1-12, day 1-31).</summary>
        public static ulong FromUtcDate(int year, int month, int day)
        {
            ulong h = FnvOffset;
            for (int i = 0; i < Salt.Length; i++)
            {
                h = Mix(h, (byte)Salt[i]);
            }

            // "yyyyMMdd" as eight ASCII digits, most significant first.
            h = MixDigits(h, year, 4);
            h = MixDigits(h, month, 2);
            h = MixDigits(h, day, 2);

            return FixRng.SplitMix64(ref h);
        }

        private static ulong MixDigits(ulong h, int value, int digits)
        {
            for (int i = digits - 1; i >= 0; i--)
            {
                int digit = value;
                for (int d = 0; d < i; d++)
                {
                    digit /= 10;
                }

                h = Mix(h, (byte)('0' + digit % 10));
            }

            return h;
        }

        private static ulong Mix(ulong h, byte b)
        {
            unchecked
            {
                h ^= b;
                h *= FnvPrime;
                return h;
            }
        }
    }
}
