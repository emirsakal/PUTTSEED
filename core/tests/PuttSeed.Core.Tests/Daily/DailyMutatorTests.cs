using NUnit.Framework;
using PuttSeed.Core.Daily;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Daily
{
    [TestFixture]
    public class DailyMutatorTests
    {
        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        [Test]
        public void Version1_IsNeverMutated()
        {
            // Journey levels and every archived daily regenerate under v1.
            // A mutator there would silently rewrite finished history.
            for (ulong seed = 1; seed <= 400; seed++)
            {
                Assert.That(DailyMutators.ForSeed(seed, 1), Is.EqualTo(DailyMutator.None), $"seed {seed}");
            }
        }

        [Test]
        public void SameSeed_SameMutator()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                Assert.That(DailyMutators.ForSeed(seed, 2), Is.EqualTo(DailyMutators.ForSeed(seed, 2)));
            }
        }

        [Test]
        public void EveryKind_AppearsAndStaysRare()
        {
            int none = 0, icy = 0, bouncy = 0, windy = 0;
            const int sample = 2000;
            for (ulong seed = 1; seed <= sample; seed++)
            {
                switch (DailyMutators.ForSeed(seed, 2))
                {
                    case DailyMutator.Icy: icy++; break;
                    case DailyMutator.Bouncy: bouncy++; break;
                    case DailyMutator.Windy: windy++; break;
                    default: none++; break;
                }
            }

            Assert.That(icy, Is.GreaterThan(0), "icy days must happen");
            Assert.That(bouncy, Is.GreaterThan(0), "bouncy days must happen");
            Assert.That(windy, Is.GreaterThan(0), "windy days must happen");

            // A themed day is a treat, not the norm: plain days stay the
            // clear majority, so the game a player learns is the usual one.
            Assert.That(none, Is.GreaterThan(sample * 3 / 4), $"only {none}/{sample} plain days");
        }

        [Test]
        public void PlainDays_LeaveTheConfigUntouched()
        {
            ulong plain = FindSeed(DailyMutator.None);
            var mutated = DailyMutators.Apply(SimConfig.Default, plain, 2);
            Assert.That(mutated, Is.SameAs(SimConfig.Default),
                "an unmutated day must not even rebuild the config");
        }

        [Test]
        public void IcyDay_SlidesFartherThanAPlainDay()
        {
            var plain = SimConfig.Default;
            var icy = DailyMutators.Apply(plain, FindSeed(DailyMutator.Icy), 2);
            Assert.That(icy.RollDamping > plain.RollDamping, Is.True, "less friction, not more");

            Assert.That(RollDistance(icy) > RollDistance(plain), Is.True,
                "an icy day must actually carry the ball farther");
        }

        [Test]
        public void BouncyDay_KicksHarderOffABumper()
        {
            var plain = SimConfig.Default;
            var bouncy = DailyMutators.Apply(plain, FindSeed(DailyMutator.Bouncy), 2);
            Assert.That(bouncy.BumperRestitution > plain.BumperRestitution, Is.True);
            Assert.That(bouncy.RollDamping, Is.EqualTo(plain.RollDamping), "friction is untouched");
        }

        [Test]
        public void WindyDay_PushesTheBallSideways()
        {
            var plain = SimConfig.Default;
            var windy = DailyMutators.Apply(plain, FindSeed(DailyMutator.Windy), 2);
            Assert.That(windy.Wind.LengthSq() > Fix64.Zero, Is.True, "a windy day needs wind");

            var course = new CourseData(Vec2Fix.Zero, V(50, 50), 2,
                System.Array.Empty<WallSegment>());
            var still = new GolfSim(course, plain);
            var blown = new GolfSim(course, windy);
            still.Shoot(new ShotInput(0, 255));
            blown.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 600; i++)
            {
                still.Tick();
                blown.Tick();
            }

            var drift = blown.Ball.Position - still.Ball.Position;
            Assert.That(drift.LengthSq() > Fix64.Zero, Is.True, "wind must move the ball off line");
        }

        [Test]
        public void ZeroWind_IsBitIdentical_ToNoWindAtAll()
        {
            // The guarantee that keeps every golden hash intact: the wind term
            // exists in the tick path even on a plain day, and adds nothing.
            var course = new CourseData(Vec2Fix.Zero, V(50, 50), 2,
                System.Array.Empty<WallSegment>(),
                bumpers: new[] { new Bumper(V(3, 0), Fix64.FromFraction(3, 10)) });
            var a = new GolfSim(course, SimConfig.Default);
            var b = new GolfSim(course, SimConfig.Default.WithWind(Vec2Fix.Zero));
            a.Shoot(new ShotInput(40, 255));
            b.Shoot(new ShotInput(40, 255));
            for (int i = 0; i < 1200; i++)
            {
                a.Tick();
                b.Tick();
                Assert.That(b.StateHash(), Is.EqualTo(a.StateHash()), $"diverged at tick {i}");
            }
        }

        [Test]
        public void MutatedConfigs_KeepEveryOtherKnob()
        {
            foreach (var kind in new[] { DailyMutator.Icy, DailyMutator.Bouncy, DailyMutator.Windy })
            {
                var c = DailyMutators.Apply(SimConfig.Default, FindSeed(kind), 2);
                Assert.That(c.Dt, Is.EqualTo(SimConfig.Default.Dt), $"{kind} dt");
                Assert.That(c.BallRadius, Is.EqualTo(SimConfig.Default.BallRadius), $"{kind} radius");
                Assert.That(c.MaxShotSpeed, Is.EqualTo(SimConfig.Default.MaxShotSpeed), $"{kind} speed");
                Assert.That(c.HoleRadius, Is.EqualTo(SimConfig.Default.HoleRadius), $"{kind} hole");
                Assert.That(c.RestTicksRequired, Is.EqualTo(SimConfig.Default.RestTicksRequired),
                    $"{kind} rest");
            }
        }

        /// <summary>Distance the ball rolls from a full-power shot before resting.</summary>
        private static Fix64 RollDistance(SimConfig config)
        {
            var course = new CourseData(Vec2Fix.Zero, V(500, 500), 2,
                System.Array.Empty<WallSegment>());
            var sim = new GolfSim(course, config);
            sim.Shoot(new ShotInput(0, 255));
            for (int i = 0; i < 20000 && !sim.IsAtRest; i++)
            {
                sim.Tick();
            }

            return sim.Ball.Position.X;
        }

        private static ulong FindSeed(DailyMutator kind)
        {
            for (ulong seed = 1; seed <= 5000; seed++)
            {
                if (DailyMutators.ForSeed(seed, 2) == kind)
                {
                    return seed;
                }
            }

            throw new System.InvalidOperationException($"no seed produced {kind}");
        }

        [Test]
        public void WindyDays_DoNotAllBlowTheSame()
        {
            // A vane with a strength readout is a lie when every windy day
            // blows at one number: the barbs always read two and the top bar
            // always says the same speed.
            bool light = false;
            bool strong = false;
            for (ulong seed = 1; seed <= 3000; seed++)
            {
                if (DailyMutators.ForSeed(seed, 4) != DailyMutator.Windy)
                {
                    continue;
                }

                var strength = DailyMutators.Apply(SimConfig.Default, seed, 4).Wind.Length();

                // Every windy day has to be worth the label.
                Assert.That(strength.Raw, Is.GreaterThan(Fix64.FromFraction(2, 10).Raw),
                    $"seed {seed} is called windy and barely blows");

                light |= strength < Fix64.FromFraction(5, 10);
                strong |= strength > Fix64.FromFraction(9, 10);
            }

            Assert.That(light, Is.True, "no windy day blows gently");
            Assert.That(strong, Is.True, "no windy day blows hard");
        }

        [Test]
        public void WindStrength_IsAFunctionOfTheSeed()
        {
            for (ulong seed = 1; seed <= 600; seed++)
            {
                var a = DailyMutators.Apply(SimConfig.Default, seed, 4).Wind;
                var b = DailyMutators.Apply(SimConfig.Default, seed, 4).Wind;
                Assert.That(a.X.Raw, Is.EqualTo(b.X.Raw), $"seed {seed}");
                Assert.That(a.Y.Raw, Is.EqualTo(b.Y.Raw), $"seed {seed}");
            }
        }
    }
}
