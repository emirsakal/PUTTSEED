using System;
using NUnit.Framework;
using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Tests.FixedMath
{
    [TestFixture]
    public class FixTrigTests
    {
        private const long OneRaw = 1L << 32;

        [Test]
        public void CardinalAngles_AreExact()
        {
            Assert.That(FixTrig.Sin(0), Is.EqualTo(Fix64.Zero));
            Assert.That(FixTrig.Sin(256), Is.EqualTo(Fix64.One));
            Assert.That(FixTrig.Sin(512), Is.EqualTo(Fix64.Zero));
            Assert.That(FixTrig.Sin(768), Is.EqualTo(-Fix64.One));

            Assert.That(FixTrig.Cos(0), Is.EqualTo(Fix64.One));
            Assert.That(FixTrig.Cos(256), Is.EqualTo(Fix64.Zero));
            Assert.That(FixTrig.Cos(512), Is.EqualTo(-Fix64.One));
            Assert.That(FixTrig.Cos(768), Is.EqualTo(Fix64.Zero));
        }

        [Test]
        public void Table_MatchesDoubleSine_WithinOneUlp()
        {
            // The committed table was generated offline from double-precision
            // sine. Tests live outside core/src, so double is allowed here.
            for (int i = 0; i < FixTrig.AngleSteps; i++)
            {
                double angle = 2.0 * Math.PI * i / FixTrig.AngleSteps;
                long expected = (long)Math.Round(Math.Sin(angle) * 4294967296.0, MidpointRounding.AwayFromZero);
                Assert.That(FixTrig.Sin(i).Raw, Is.EqualTo(expected).Within(1L),
                    $"sin table mismatch at index {i}");
            }
        }

        [Test]
        public void Sin_IsOddSymmetric()
        {
            for (int i = 1; i < FixTrig.AngleSteps; i++)
            {
                Assert.That(FixTrig.Sin(FixTrig.AngleSteps - i).Raw, Is.EqualTo(-FixTrig.Sin(i).Raw),
                    $"sin symmetry broken at index {i}");
            }
        }

        [Test]
        public void Cos_IsSinShiftedByQuarterTurn()
        {
            for (int i = 0; i < FixTrig.AngleSteps; i++)
            {
                Assert.That(FixTrig.Cos(i), Is.EqualTo(FixTrig.Sin(i + FixTrig.AngleSteps / 4)));
            }
        }

        [Test]
        public void Index_WrapsModuloTableSize()
        {
            Assert.That(FixTrig.Sin(FixTrig.AngleSteps + 17), Is.EqualTo(FixTrig.Sin(17)));
            Assert.That(FixTrig.Sin(-1), Is.EqualTo(FixTrig.Sin(FixTrig.AngleSteps - 1)));
            Assert.That(FixTrig.Cos(5 * FixTrig.AngleSteps + 3), Is.EqualTo(FixTrig.Cos(3)));
        }

        [Test]
        public void AllValues_AreWithinUnitRange()
        {
            for (int i = 0; i < FixTrig.AngleSteps; i++)
            {
                Assert.That(Math.Abs(FixTrig.Sin(i).Raw), Is.LessThanOrEqualTo(OneRaw));
            }
        }

        [Test]
        public void UnitVector_HasUnitLength_WithinTolerance()
        {
            // sin^2 + cos^2 == 1 within a few raw ulps for every angle.
            for (int i = 0; i < FixTrig.AngleSteps; i++)
            {
                var s = FixTrig.Sin(i);
                var c = FixTrig.Cos(i);
                long lenSq = (s * s + c * c).Raw;
                Assert.That(lenSq, Is.EqualTo(OneRaw).Within(8L), $"unit length broken at index {i}");
            }
        }
    }
}
