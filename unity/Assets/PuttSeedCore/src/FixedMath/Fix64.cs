using System;

namespace PuttSeed.Core.FixedMath
{
    /// <summary>
    /// Q32.32 signed fixed-point number stored in a 64-bit integer.
    /// All simulation math in PuttSeed.Core uses this type; no binary
    /// floating point is allowed anywhere in the core assembly so that the
    /// simulation is bit-identical on every device.
    /// Overflow behavior of add/sub/mul wraps (unchecked); division saturates
    /// to <see cref="MaxValue"/>/<see cref="MinValue"/> on overflow.
    /// </summary>
    public readonly struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
    {
        /// <summary>Number of fractional bits.</summary>
        public const int FractionalBits = 32;

        private const long OneRaw = 1L << FractionalBits;

        /// <summary>Raw Q32.32 bits. Exposed for hashing, serialization and tests.</summary>
        public long Raw { get; }

        private Fix64(long raw) => Raw = raw;

        /// <summary>The value 0.</summary>
        public static readonly Fix64 Zero = new Fix64(0);

        /// <summary>The value 1.</summary>
        public static readonly Fix64 One = new Fix64(OneRaw);

        /// <summary>The value 0.5.</summary>
        public static readonly Fix64 Half = new Fix64(OneRaw / 2);

        /// <summary>Largest representable value (~2^31).</summary>
        public static readonly Fix64 MaxValue = new Fix64(long.MaxValue);

        /// <summary>Smallest representable value (~-2^31).</summary>
        public static readonly Fix64 MinValue = new Fix64(long.MinValue);

        /// <summary>Smallest positive increment (2^-32).</summary>
        public static readonly Fix64 Epsilon = new Fix64(1);

        /// <summary>Reinterprets raw Q32.32 bits as a <see cref="Fix64"/>.</summary>
        public static Fix64 FromRaw(long raw) => new Fix64(raw);

        /// <summary>Converts an integer to fixed point exactly.</summary>
        public static Fix64 FromInt(int value) => new Fix64((long)value << FractionalBits);

        /// <summary>
        /// Builds the fixed-point value <paramref name="numerator"/>/<paramref name="denominator"/>.
        /// Preferred way to write fractional constants in core.
        /// </summary>
        public static Fix64 FromFraction(int numerator, int denominator)
            => FromInt(numerator) / FromInt(denominator);

        /// <summary>Truncates toward zero to an integer.</summary>
        public int ToInt() => (int)(Raw / OneRaw);

        /// <summary>Adds two values (wraps on overflow).</summary>
        public static Fix64 operator +(Fix64 a, Fix64 b) => new Fix64(unchecked(a.Raw + b.Raw));

        /// <summary>Subtracts <paramref name="b"/> from <paramref name="a"/> (wraps on overflow).</summary>
        public static Fix64 operator -(Fix64 a, Fix64 b) => new Fix64(unchecked(a.Raw - b.Raw));

        /// <summary>Negates a value.</summary>
        public static Fix64 operator -(Fix64 a) => new Fix64(unchecked(-a.Raw));

        /// <summary>
        /// Multiplies using a 128-bit intermediate (manual 32-bit hi/lo split)
        /// so that large operands do not overflow the 64-bit raw product.
        /// </summary>
        public static Fix64 operator *(Fix64 a, Fix64 b)
        {
            unchecked
            {
                long xl = a.Raw;
                long yl = b.Raw;

                ulong xlo = (ulong)(xl & 0xFFFFFFFF);
                long xhi = xl >> FractionalBits;
                ulong ylo = (ulong)(yl & 0xFFFFFFFF);
                long yhi = yl >> FractionalBits;

                ulong lolo = xlo * ylo;
                long lohi = (long)xlo * yhi;
                long hilo = xhi * (long)ylo;
                long hihi = xhi * yhi;

                long loResult = (long)(lolo >> FractionalBits);
                long hiResult = hihi << FractionalBits;

                return new Fix64(loResult + lohi + hilo + hiResult);
            }
        }

        /// <summary>
        /// Divides via long division on the raw magnitudes (shift-subtract with
        /// round-to-nearest). Saturates on overflow, throws on division by zero.
        /// </summary>
        public static Fix64 operator /(Fix64 a, Fix64 b)
        {
            long xl = a.Raw;
            long yl = b.Raw;

            if (yl == 0)
            {
                throw new DivideByZeroException("Fix64 division by zero.");
            }

            unchecked
            {
                ulong remainder = (ulong)(xl >= 0 ? xl : -xl);
                ulong divider = (ulong)(yl >= 0 ? yl : -yl);
                ulong quotient = 0;
                int bitPos = FractionalBits + 1;

                // If the divider is divisible by 2^n, take advantage of it.
                while ((divider & 0xF) == 0 && bitPos >= 4)
                {
                    divider >>= 4;
                    bitPos -= 4;
                }

                while (remainder != 0 && bitPos >= 0)
                {
                    int shift = CountLeadingZeroes(remainder);
                    if (shift > bitPos)
                    {
                        shift = bitPos;
                    }

                    remainder <<= shift;
                    bitPos -= shift;

                    ulong div = remainder / divider;
                    remainder %= divider;
                    quotient += div << bitPos;

                    // Overflow: quotient does not fit -> saturate.
                    if ((div & ~(0xFFFFFFFFFFFFFFFF >> bitPos)) != 0)
                    {
                        return ((xl ^ yl) & long.MinValue) == 0 ? MaxValue : MinValue;
                    }

                    remainder <<= 1;
                    --bitPos;
                }

                // Round to nearest.
                ++quotient;
                long result = (long)(quotient >> 1);
                if (((xl ^ yl) & long.MinValue) != 0)
                {
                    result = -result;
                }

                return new Fix64(result);
            }
        }

        /// <summary>
        /// Square root via Newton iteration (x' = (x + a/x)/2) starting from a
        /// power-of-two guess above the root; the sequence decreases monotonically
        /// and iteration stops as soon as it fails to decrease, with a fixed cap
        /// so runtime is bounded and deterministic.
        /// </summary>
        public static Fix64 Sqrt(Fix64 value)
        {
            if (value.Raw < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Fix64.Sqrt of a negative value.");
            }

            if (value.Raw == 0)
            {
                return Zero;
            }

            // Raw of sqrt(v) is sqrt(raw * 2^32); start from 2^ceil((bitlen+32)/2),
            // which is always >= the true root.
            int bitLength = 64 - CountLeadingZeroes((ulong)value.Raw);
            int guessExponent = (bitLength + FractionalBits + 1) / 2;
            var x = new Fix64(1L << guessExponent);

            for (int i = 0; i < 64; i++)
            {
                var next = new Fix64((x + value / x).Raw >> 1);
                if (next.Raw >= x.Raw)
                {
                    break;
                }

                x = next;
            }

            return x;
        }

        /// <summary>Absolute value.</summary>
        public static Fix64 Abs(Fix64 value) => value.Raw < 0 ? new Fix64(-value.Raw) : value;

        /// <summary>-1, 0 or 1.</summary>
        public static int Sign(Fix64 value) => value.Raw < 0 ? -1 : value.Raw > 0 ? 1 : 0;

        /// <summary>Smaller of two values.</summary>
        public static Fix64 Min(Fix64 a, Fix64 b) => a.Raw < b.Raw ? a : b;

        /// <summary>Larger of two values.</summary>
        public static Fix64 Max(Fix64 a, Fix64 b) => a.Raw > b.Raw ? a : b;

        /// <summary>Clamps <paramref name="value"/> into [min, max].</summary>
        public static Fix64 Clamp(Fix64 value, Fix64 min, Fix64 max)
            => value.Raw < min.Raw ? min : value.Raw > max.Raw ? max : value;

        /// <summary>Exact raw equality.</summary>
        public static bool operator ==(Fix64 a, Fix64 b) => a.Raw == b.Raw;

        /// <summary>Exact raw inequality.</summary>
        public static bool operator !=(Fix64 a, Fix64 b) => a.Raw != b.Raw;

        /// <summary>Less-than comparison.</summary>
        public static bool operator <(Fix64 a, Fix64 b) => a.Raw < b.Raw;

        /// <summary>Greater-than comparison.</summary>
        public static bool operator >(Fix64 a, Fix64 b) => a.Raw > b.Raw;

        /// <summary>Less-than-or-equal comparison.</summary>
        public static bool operator <=(Fix64 a, Fix64 b) => a.Raw <= b.Raw;

        /// <summary>Greater-than-or-equal comparison.</summary>
        public static bool operator >=(Fix64 a, Fix64 b) => a.Raw >= b.Raw;

        /// <inheritdoc />
        public bool Equals(Fix64 other) => Raw == other.Raw;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Fix64 other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Raw.GetHashCode();

        /// <inheritdoc />
        public int CompareTo(Fix64 other) => Raw.CompareTo(other.Raw);

        /// <summary>
        /// Decimal string with 6 fractional digits, computed in integer math
        /// (diagnostics only; never used by the simulation).
        /// </summary>
        public override string ToString()
        {
            long raw = Raw;
            string sign = raw < 0 ? "-" : "";
            ulong magnitude = raw < 0 ? (ulong)(-raw) : (ulong)raw;
            ulong intPart = magnitude >> FractionalBits;
            ulong frac = magnitude & 0xFFFFFFFF;

            // 6 decimal digits of the 32-bit fraction.
            ulong digits = 0;
            for (int i = 0; i < 6; i++)
            {
                frac *= 10;
                digits = digits * 10 + (frac >> FractionalBits);
                frac &= 0xFFFFFFFF;
            }

            return $"{sign}{intPart}.{digits:D6}";
        }

        private static int CountLeadingZeroes(ulong x)
        {
            int result = 0;
            while ((x & 0xF000000000000000) == 0)
            {
                result += 4;
                x <<= 4;
            }

            while ((x & 0x8000000000000000) == 0)
            {
                result += 1;
                x <<= 1;
            }

            return result;
        }
    }
}
