using NUnit.Framework;
using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Tests.FixedMath
{
    [TestFixture]
    public class Vec2FixTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        [Test]
        public void Constructor_StoresComponents()
        {
            var v = V(3, -4);
            Assert.That(v.X, Is.EqualTo(Fix64.FromInt(3)));
            Assert.That(v.Y, Is.EqualTo(Fix64.FromInt(-4)));
        }

        [Test]
        public void Zero_IsAllZero()
        {
            Assert.That(Vec2Fix.Zero.X, Is.EqualTo(Fix64.Zero));
            Assert.That(Vec2Fix.Zero.Y, Is.EqualTo(Fix64.Zero));
        }

        [Test]
        public void Addition_Subtraction_Negation()
        {
            Assert.That(V(1, 2) + V(3, 4), Is.EqualTo(V(4, 6)));
            Assert.That(V(5, 5) - V(2, 7), Is.EqualTo(V(3, -2)));
            Assert.That(-V(1, -2), Is.EqualTo(V(-1, 2)));
        }

        [Test]
        public void ScalarMultiplication_BothSides()
        {
            Assert.That(V(1, -2) * Fix64.FromInt(3), Is.EqualTo(V(3, -6)));
            Assert.That(Fix64.FromInt(3) * V(1, -2), Is.EqualTo(V(3, -6)));
            Assert.That(V(3, 6) * Fix64.Half, Is.EqualTo(new Vec2Fix(Fix64.FromFraction(3, 2), Fix64.FromInt(3))));
        }

        [Test]
        public void ScalarDivision()
        {
            Assert.That(V(6, -4) / Fix64.FromInt(2), Is.EqualTo(V(3, -2)));
            Assert.That(V(1, 0) / Fix64.Half, Is.EqualTo(V(2, 0)));
        }

        [Test]
        public void Dot_Product()
        {
            Assert.That(Vec2Fix.Dot(V(1, 2), V(3, 4)), Is.EqualTo(Fix64.FromInt(11)));
            Assert.That(Vec2Fix.Dot(V(1, 0), V(0, 1)), Is.EqualTo(Fix64.Zero));
            Assert.That(Vec2Fix.Dot(V(-2, 3), V(4, 1)), Is.EqualTo(Fix64.FromInt(-5)));
        }

        [Test]
        public void LengthSq_And_Length()
        {
            Assert.That(V(3, 4).LengthSq(), Is.EqualTo(Fix64.FromInt(25)));
            Assert.That(V(3, 4).Length(), Is.EqualTo(Fix64.FromInt(5)));
            Assert.That(V(0, 0).Length(), Is.EqualTo(Fix64.Zero));
            Assert.That(V(-6, 8).Length(), Is.EqualTo(Fix64.FromInt(10)));
        }

        [Test]
        public void Equality_IsExact()
        {
            Assert.That(V(1, 2) == V(1, 2), Is.True);
            Assert.That(V(1, 2) != V(2, 1), Is.True);
            Assert.That(V(1, 2).Equals(V(1, 2)), Is.True);
        }

        [Test]
        public void Perp_RotatesQuarterTurnCounterClockwise()
        {
            // Perp of (1,0) is (0,1) in a Y-up convention.
            Assert.That(V(1, 0).Perp(), Is.EqualTo(V(0, 1)));
            Assert.That(V(0, 1).Perp(), Is.EqualTo(V(-1, 0)));
        }
    }
}
