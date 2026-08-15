using NUnit.Framework;
using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Tests.FixedMath
{
    [TestFixture]
    public class FixRngTests
    {
        // Golden values computed by an offline reference implementation of the
        // spec: xorshift128 (Marsaglia 2003), state = two SplitMix64 outputs.
        [Test]
        public void NextUInt_MatchesGoldenSequence_Seed42()
        {
            var rng = new FixRng(42UL);
            Assert.That(rng.NextUInt(), Is.EqualTo(1543815037u));
            Assert.That(rng.NextUInt(), Is.EqualTo(1481044185u));
            Assert.That(rng.NextUInt(), Is.EqualTo(3710778427u));
            Assert.That(rng.NextUInt(), Is.EqualTo(2324458198u));
            Assert.That(rng.NextUInt(), Is.EqualTo(4077573037u));
        }

        [Test]
        public void NextUInt_MatchesGoldenSequence_Seed123()
        {
            var rng = new FixRng(123UL);
            Assert.That(rng.NextUInt(), Is.EqualTo(1782967707u));
            Assert.That(rng.NextUInt(), Is.EqualTo(974641468u));
            Assert.That(rng.NextUInt(), Is.EqualTo(3075366229u));
        }

        [Test]
        public void NextUInt_MatchesGoldenSequence_SeedZero()
        {
            // Seed 0 must not collapse to an all-zero state.
            var rng = new FixRng(0UL);
            Assert.That(rng.NextUInt(), Is.EqualTo(4221392575u));
            Assert.That(rng.NextUInt(), Is.EqualTo(471550101u));
        }

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new FixRng(0xDEADBEEFCAFEBABEUL);
            var b = new FixRng(0xDEADBEEFCAFEBABEUL);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
            }
        }

        [Test]
        public void DifferentSeeds_DivergeQuickly()
        {
            var a = new FixRng(1UL);
            var b = new FixRng(2UL);
            bool anyDifferent = false;
            for (int i = 0; i < 10; i++)
            {
                if (a.NextUInt() != b.NextUInt())
                {
                    anyDifferent = true;
                }
            }

            Assert.That(anyDifferent, Is.True);
        }

        [Test]
        public void NextInt_MatchesGoldenSequence_AndStaysInRange()
        {
            var rng = new FixRng(42UL);
            int[] expected = { 2, 2, 5, 3, 5, 3, 1, 4 };
            foreach (int e in expected)
            {
                int v = rng.NextInt(0, 6);
                Assert.That(v, Is.EqualTo(e));
                Assert.That(v, Is.InRange(0, 5));
            }
        }

        [Test]
        public void NextInt_RespectsNonZeroMinimum()
        {
            var rng = new FixRng(7UL);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(rng.NextInt(10, 20), Is.InRange(10, 19));
            }
        }

        [Test]
        public void NextFix01_IsInHalfOpenUnitInterval()
        {
            var rng = new FixRng(99UL);
            for (int i = 0; i < 1000; i++)
            {
                var v = rng.NextFix01();
                Assert.That(v >= Fix64.Zero, Is.True);
                Assert.That(v < Fix64.One, Is.True);
            }
        }

        [Test]
        public void NextFix01_RawEqualsNextUIntOfTwinRng()
        {
            // NextFix01 is specified as the next 32 output bits placed in the
            // fractional part, so its raw value equals a twin RNG's NextUInt.
            var a = new FixRng(5UL);
            var b = new FixRng(5UL);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.NextFix01().Raw, Is.EqualTo((long)b.NextUInt()));
            }
        }
    }
}
