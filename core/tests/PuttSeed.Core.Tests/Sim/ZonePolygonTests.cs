using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class ZonePolygonTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static Vec2Fix VF(int xn, int xd, int yn, int yd)
            => new Vec2Fix(Fix64.FromFraction(xn, xd), Fix64.FromFraction(yn, yd));

        private static ZonePolygon Square() => new ZonePolygon(new[]
        {
            V(0, 0), V(4, 0), V(4, 4), V(0, 4),
        });

        [Test]
        public void Square_ContainsInteriorPoints()
        {
            Assert.That(Square().Contains(V(2, 2)), Is.True);
            Assert.That(Square().Contains(V(1, 3)), Is.True);
            Assert.That(Square().Contains(VF(1, 2, 7, 2)), Is.True); // (0.5, 3.5)
        }

        [Test]
        public void Square_ExcludesExteriorPoints()
        {
            Assert.That(Square().Contains(V(5, 2)), Is.False);
            Assert.That(Square().Contains(V(-1, 2)), Is.False);
            Assert.That(Square().Contains(V(2, 5)), Is.False);
            Assert.That(Square().Contains(V(2, -1)), Is.False);
            Assert.That(Square().Contains(V(100, 100)), Is.False);
        }

        [Test]
        public void Square_PointsLevelWithVertices_ResolveConsistently()
        {
            // Interior point level with a vertex row (must not double-count edges):
            Assert.That(Square().Contains(VF(2, 1, 1, 1)), Is.True); // (2, 1)
            // Exterior point level with a vertex row:
            Assert.That(Square().Contains(V(9, 0)), Is.False);
            Assert.That(Square().Contains(V(-9, 4)), Is.False);
        }

        [Test]
        public void ConcavePolygon_LShape_ClassifiesNotchCorrectly()
        {
            // L-shape: big square with the top-right quadrant removed.
            var l = new ZonePolygon(new[]
            {
                V(0, 0), V(4, 0), V(4, 2), V(2, 2), V(2, 4), V(0, 4),
            });
            Assert.That(l.Contains(V(1, 1)), Is.True);   // lower body
            Assert.That(l.Contains(V(3, 1)), Is.True);   // lower right arm
            Assert.That(l.Contains(V(1, 3)), Is.True);   // upper left arm
            Assert.That(l.Contains(V(3, 3)), Is.False);  // the notch
        }

        [Test]
        public void Triangle_Works()
        {
            var t = new ZonePolygon(new[] { V(0, 0), V(6, 0), V(3, 6) });
            Assert.That(t.Contains(V(3, 2)), Is.True);
            Assert.That(t.Contains(V(1, 5)), Is.False);
            Assert.That(t.Contains(V(5, 5)), Is.False);
        }
    }
}
