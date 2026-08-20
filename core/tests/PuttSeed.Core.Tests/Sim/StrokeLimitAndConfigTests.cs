using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    [TestFixture]
    public class StrokeLimitAndConfigTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static CourseData OpenCourse(int par) => new CourseData(
            startPosition: Vec2Fix.Zero,
            holePosition: V(100, 100),
            par: par,
            walls: System.Array.Empty<WallSegment>());

        private static void RunToRest(GolfSim sim)
        {
            for (int i = 0; i < 20000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsAtRest, Is.True);
        }

        [Test]
        public void Shoot_IsRefused_AtStrokeLimit()
        {
            // Par 2 -> GDD stroke limit par + 3 = 5.
            var sim = new GolfSim(OpenCourse(2), SimConfig.Default);
            for (int s = 0; s < 5; s++)
            {
                sim.Shoot(new ShotInput(s * 100, 10));
                RunToRest(sim);
            }

            Assert.That(sim.Strokes, Is.EqualTo(5));
            sim.Shoot(new ShotInput(0, 200));
            Assert.That(sim.Strokes, Is.EqualTo(5), "shot beyond the stroke limit must be refused");
            Assert.That(sim.IsAtRest, Is.True);
        }

        [Test]
        public void IsFailed_TrueAtLimitWithoutCapture_FalseBefore()
        {
            var sim = new GolfSim(OpenCourse(2), SimConfig.Default);
            Assert.That(sim.IsFailed, Is.False);
            for (int s = 0; s < 5; s++)
            {
                Assert.That(sim.IsFailed, Is.False, $"failed too early at stroke {s}");
                sim.Shoot(new ShotInput(s * 100, 10));
                RunToRest(sim);
            }

            Assert.That(sim.IsFailed, Is.True);
            Assert.That(sim.IsHoled, Is.False);
        }

        [Test]
        public void HoledRun_IsNeverFailed()
        {
            var course = new CourseData(Vec2Fix.Zero, V(3, 0), par: 2,
                walls: System.Array.Empty<WallSegment>());
            var sim = new GolfSim(course, SimConfig.Default);
            sim.Shoot(new ShotInput(0, 159));
            for (int i = 0; i < 2400 && !sim.IsHoled; i++)
            {
                sim.Tick();
            }

            Assert.That(sim.IsHoled, Is.True);
            Assert.That(sim.IsFailed, Is.False);
        }

        [Test]
        public void SimConfigCreate_RoundTripsAllParameters()
        {
            var c = SimConfig.Create(
                dt: Fix64.FromFraction(1, 120),
                ballRadius: Fix64.FromFraction(1, 10),
                maxShotSpeed: Fix64.FromInt(9),
                rollDamping: Fix64.FromFraction(99, 100),
                sandDamping: Fix64.FromFraction(9, 10),
                iceDamping: Fix64.FromFraction(995, 1000),
                wallRestitution: Fix64.FromFraction(3, 4),
                maxTravelPerSubStep: Fix64.FromFraction(1, 20),
                bumperRestitution: Fix64.FromFraction(13, 10),
                bumperMaxExitSpeed: Fix64.FromInt(9),
                holeRadius: Fix64.FromFraction(1, 5),
                holeCaptureSpeedSq: Fix64.FromInt(2),
                rimRestitution: Fix64.Half,
                restSpeedEpsSq: Fix64.FromFraction(1, 2500),
                restTicksRequired: 8);

            Assert.That(c.MaxShotSpeed, Is.EqualTo(Fix64.FromInt(9)));
            Assert.That(c.RollDamping, Is.EqualTo(Fix64.FromFraction(99, 100)));
            Assert.That(c.RestTicksRequired, Is.EqualTo(8));
            Assert.That(c.HoleRadius, Is.EqualTo(Fix64.FromFraction(1, 5)));
        }

        [Test]
        public void WithHoleCapture_KeepsEveryOtherKnob_WindIncluded()
        {
            var windy = SimConfig.Default.WithWind(V(0, 1)).WithRollDamping(Fix64.FromFraction(97, 100));
            var relaxed = windy.WithHoleCapture(Fix64.FromInt(9));

            Assert.That(relaxed.HoleCaptureSpeedSq, Is.EqualTo(Fix64.FromInt(9)));

            // The whole point: a config rebuilt to move ONE knob must not
            // quietly lose another. Wind is the one that got lost.
            Assert.That(relaxed.Wind.X, Is.EqualTo(windy.Wind.X));
            Assert.That(relaxed.Wind.Y, Is.EqualTo(windy.Wind.Y));
            Assert.That(relaxed.RollDamping, Is.EqualTo(windy.RollDamping));
            Assert.That(relaxed.HoleRadius, Is.EqualTo(windy.HoleRadius));
            Assert.That(relaxed.RimRestitution, Is.EqualTo(windy.RimRestitution));
            Assert.That(relaxed.RestTicksRequired, Is.EqualTo(windy.RestTicksRequired));
        }

        [Test]
        public void SimConfigCreate_MatchingDefaults_BehavesLikeDefault()
        {
            var d = SimConfig.Default;
            var c = SimConfig.Create(d.Dt, d.BallRadius, d.MaxShotSpeed, d.RollDamping, d.SandDamping,
                d.IceDamping, d.WallRestitution, d.MaxTravelPerSubStep, d.BumperRestitution,
                d.BumperMaxExitSpeed, d.HoleRadius, d.HoleCaptureSpeedSq, d.RimRestitution,
                d.RestSpeedEpsSq, d.RestTicksRequired);

            var course = new CourseData(Vec2Fix.Zero, V(3, 0), par: 2,
                walls: System.Array.Empty<WallSegment>());
            var a = new GolfSim(course, d);
            var b = new GolfSim(course, c);
            a.Shoot(new ShotInput(100, 200));
            b.Shoot(new ShotInput(100, 200));
            for (int i = 0; i < 1200; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(b.StateHash(), Is.EqualTo(a.StateHash()), $"tick {i}");
            }
        }
    }
}
