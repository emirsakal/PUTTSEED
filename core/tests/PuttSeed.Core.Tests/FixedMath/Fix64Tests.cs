using NUnit.Framework;
using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Tests.FixedMath
{
    [TestFixture]
    public class Fix64Tests
    {
        private const long OneRaw = 1L << 32;

        [Test]
        public void FromInt_ProducesExpectedRaw()
        {
            Assert.That(Fix64.FromInt(0).Raw, Is.EqualTo(0L));
            Assert.That(Fix64.FromInt(1).Raw, Is.EqualTo(OneRaw));
            Assert.That(Fix64.FromInt(-3).Raw, Is.EqualTo(-3L * OneRaw));
            Assert.That(Fix64.FromInt(1000).Raw, Is.EqualTo(1000L * OneRaw));
        }

        [Test]
        public void Constants_HaveExpectedRaw()
        {
            Assert.That(Fix64.Zero.Raw, Is.EqualTo(0L));
            Assert.That(Fix64.One.Raw, Is.EqualTo(OneRaw));
            Assert.That(Fix64.Half.Raw, Is.EqualTo(OneRaw / 2));
            Assert.That(Fix64.MaxValue.Raw, Is.EqualTo(long.MaxValue));
            Assert.That(Fix64.MinValue.Raw, Is.EqualTo(long.MinValue));
        }

        [Test]
        public void ToInt_TruncatesTowardZero()
        {
            Assert.That(Fix64.FromFraction(7, 2).ToInt(), Is.EqualTo(3));   //  3.5 ->  3
            Assert.That(Fix64.FromFraction(-7, 2).ToInt(), Is.EqualTo(-3)); // -3.5 -> -3
            Assert.That(Fix64.FromInt(5).ToInt(), Is.EqualTo(5));
        }

        [Test]
        public void Addition_And_Subtraction_AreExact()
        {
            var a = Fix64.FromInt(5);
            var b = Fix64.FromInt(3);
            Assert.That((a + b).Raw, Is.EqualTo(8L * OneRaw));
            Assert.That((a - b).Raw, Is.EqualTo(2L * OneRaw));
            Assert.That((b - a).Raw, Is.EqualTo(-2L * OneRaw));
            Assert.That((-a).Raw, Is.EqualTo(-5L * OneRaw));
        }

        [Test]
        public void Multiplication_SimpleCases()
        {
            Assert.That(Fix64.FromInt(6) * Fix64.FromInt(7), Is.EqualTo(Fix64.FromInt(42)));
            Assert.That(Fix64.Half * Fix64.Half, Is.EqualTo(Fix64.FromFraction(1, 4)));
            Assert.That(Fix64.FromFraction(3, 2) * Fix64.FromInt(2), Is.EqualTo(Fix64.FromInt(3)));
            Assert.That(Fix64.FromInt(-4) * Fix64.FromInt(3), Is.EqualTo(Fix64.FromInt(-12)));
            Assert.That(Fix64.FromInt(-4) * Fix64.FromInt(-3), Is.EqualTo(Fix64.FromInt(12)));
            Assert.That(Fix64.Zero * Fix64.FromInt(123), Is.EqualTo(Fix64.Zero));
        }

        [Test]
        public void Multiplication_LargeOperands_Use128BitIntermediate()
        {
            // 30000 * 30000 = 900_000_000 fits in Q32.32 integer range, but the raw
            // product (30000<<32)*(30000<<32) would overflow a naive 64-bit multiply.
            Assert.That(Fix64.FromInt(30000) * Fix64.FromInt(30000),
                Is.EqualTo(Fix64.FromInt(900_000_000)));
        }

        [Test]
        public void Division_ExactCases()
        {
            Assert.That(Fix64.FromInt(6) / Fix64.FromInt(3), Is.EqualTo(Fix64.FromInt(2)));
            Assert.That(Fix64.FromInt(1) / Fix64.FromInt(4), Is.EqualTo(Fix64.FromFraction(1, 4)));
            Assert.That(Fix64.FromInt(-6) / Fix64.FromInt(3), Is.EqualTo(Fix64.FromInt(-2)));
            Assert.That(Fix64.FromInt(-6) / Fix64.FromInt(-3), Is.EqualTo(Fix64.FromInt(2)));
        }

        [Test]
        public void Division_OneOver120_MatchesPrecomputedRaw()
        {
            // (1<<32)/120 = 35791394.133..., round-to-nearest -> 35791394
            var dt = Fix64.FromInt(1) / Fix64.FromInt(120);
            Assert.That(dt.Raw, Is.EqualTo(35791394L));
        }

        [Test]
        public void Division_ByZero_Throws()
        {
            Assert.Throws<System.DivideByZeroException>(
                () => _ = Fix64.One / Fix64.Zero);
        }

        [Test]
        public void FromFraction_MatchesDivision()
        {
            Assert.That(Fix64.FromFraction(1, 120), Is.EqualTo(Fix64.FromInt(1) / Fix64.FromInt(120)));
            Assert.That(Fix64.FromFraction(-3, 4), Is.EqualTo(Fix64.FromInt(-3) / Fix64.FromInt(4)));
        }

        [Test]
        public void Sqrt_PerfectSquares_AreExact()
        {
            Assert.That(Fix64.Sqrt(Fix64.FromInt(0)), Is.EqualTo(Fix64.Zero));
            Assert.That(Fix64.Sqrt(Fix64.FromInt(1)), Is.EqualTo(Fix64.One));
            Assert.That(Fix64.Sqrt(Fix64.FromInt(4)), Is.EqualTo(Fix64.FromInt(2)));
            Assert.That(Fix64.Sqrt(Fix64.FromInt(9)), Is.EqualTo(Fix64.FromInt(3)));
            Assert.That(Fix64.Sqrt(Fix64.FromInt(1 << 20)), Is.EqualTo(Fix64.FromInt(1 << 10)));
        }

        [Test]
        public void Sqrt_Of2_MatchesPrecomputedRawWithinTolerance()
        {
            // sqrt(2) * 2^32 = 6074000999.72...
            var s = Fix64.Sqrt(Fix64.FromInt(2));
            Assert.That(s.Raw, Is.EqualTo(6074001000L).Within(4L));
        }

        [Test]
        public void Sqrt_OfQuarter_IsHalf()
        {
            var s = Fix64.Sqrt(Fix64.FromFraction(1, 4));
            Assert.That(s.Raw, Is.EqualTo(Fix64.Half.Raw).Within(4L));
        }

        [Test]
        public void Sqrt_Negative_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => _ = Fix64.Sqrt(Fix64.FromInt(-1)));
        }

        [Test]
        public void Comparisons_Work()
        {
            var a = Fix64.FromInt(1);
            var b = Fix64.FromInt(2);
            Assert.That(a < b, Is.True);
            Assert.That(b > a, Is.True);
            var aCopy = Fix64.FromRaw(a.Raw);
            Assert.That(a <= aCopy, Is.True);
            Assert.That(a >= aCopy, Is.True);
            Assert.That(a == Fix64.One, Is.True);
            Assert.That(a != b, Is.True);
        }

        [Test]
        public void AbsSignMinMaxClamp_Work()
        {
            Assert.That(Fix64.Abs(Fix64.FromInt(-5)), Is.EqualTo(Fix64.FromInt(5)));
            Assert.That(Fix64.Abs(Fix64.FromInt(5)), Is.EqualTo(Fix64.FromInt(5)));
            Assert.That(Fix64.Sign(Fix64.FromInt(-5)), Is.EqualTo(-1));
            Assert.That(Fix64.Sign(Fix64.Zero), Is.EqualTo(0));
            Assert.That(Fix64.Sign(Fix64.FromInt(5)), Is.EqualTo(1));
            Assert.That(Fix64.Min(Fix64.One, Fix64.Half), Is.EqualTo(Fix64.Half));
            Assert.That(Fix64.Max(Fix64.One, Fix64.Half), Is.EqualTo(Fix64.One));
            Assert.That(Fix64.Clamp(Fix64.FromInt(10), Fix64.Zero, Fix64.One), Is.EqualTo(Fix64.One));
            Assert.That(Fix64.Clamp(Fix64.FromInt(-10), Fix64.Zero, Fix64.One), Is.EqualTo(Fix64.Zero));
            Assert.That(Fix64.Clamp(Fix64.Half, Fix64.Zero, Fix64.One), Is.EqualTo(Fix64.Half));
        }

        [Test]
        public void Multiplication_IsCommutative_OnSamples()
        {
            // A few hand-picked raw values, including negative and fractional ones.
            long[] raws = { 0L, 1L, -1L, OneRaw, -OneRaw, OneRaw / 3, 123456789L, -987654321L, 5L * OneRaw + 12345L };
            foreach (var ra in raws)
            {
                foreach (var rb in raws)
                {
                    var a = Fix64.FromRaw(ra);
                    var b = Fix64.FromRaw(rb);
                    Assert.That((a * b).Raw, Is.EqualTo((b * a).Raw),
                        $"a*b != b*a for raw {ra}, {rb}");
                }
            }
        }
    }
}
