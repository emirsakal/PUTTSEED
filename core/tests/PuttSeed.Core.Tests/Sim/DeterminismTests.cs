using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Sim
{
    /// <summary>
    /// The backbone determinism test: a fixture course exercising every course
    /// element, a scripted shot sequence, 10,000 ticks. The final state hash is
    /// compared against a committed golden value; any accidental change to sim
    /// math (a stray rounding change, reordered collision resolution, an
    /// altered constant) fails this test.
    /// </summary>
    [TestFixture]
    public class DeterminismTests
    {
        /// <summary>
        /// Committed golden hash. Regenerate ONLY for an intentional sim change:
        /// run this test, read the actual value from the failure message, update
        /// the constant, and call the change out in the commit message.
        /// Re-frozen 2026-08-16 when an ice zone joined the fixture, and again
        /// 2026-08-18 when the gate, ramp, portal and windmill did — the hash
        /// now covers all ten elements.
        /// </summary>
        private const ulong GoldenHash10K = 11426007175965104957UL;

        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        private static Fix64 F(int numerator, int denominator) => Fix64.FromFraction(numerator, denominator);

        /// <summary>The course elements the fixture must actually exercise.</summary>
        public enum Element
        {
            Bumpers,
            Sand,
            Water,
            Ice,
            Gates,
            Ramps,
            Portals,
            Windmills,
        }

        /// <summary>
        /// Fixture course: a box holding one of everything, spread so a long
        /// script cannot avoid them. Par 40 keeps the stroke limit (par + 3)
        /// clear of anything the script plus water penalties can reach, so no
        /// shot is ever refused and the hash measures physics, not rules.
        /// </summary>
        /// <param name="omit">
        /// Leaves one element out, which is how the fixture proves it earns its
        /// place: drop any element and the final hash must move.
        /// </param>
        private static CourseData FixtureCourse(Element? omit = null)
        {
            bool Keep(Element e) => omit != e;

            return new CourseData(
                startPosition: V(0, 0),
                holePosition: V(10, 4),
                par: 40,
                walls: new[]
                {
                    new WallSegment(V(-1, -5), V(11, -5)),
                    new WallSegment(V(11, -5), V(11, 5)),
                    new WallSegment(V(11, 5), V(-1, 5)),
                    new WallSegment(V(-1, 5), V(-1, -5)),
                },
                bumpers: Keep(Element.Bumpers)
                    ? new[]
                    {
                        // Sat mid-run rather than at a rest point: a bumper the
                        // ball settles inside is pushed out without a bounce,
                        // and a bounce is what the hash needs to cover.
                        new Bumper(V(3, -1), F(3, 5)),
                        new Bumper(V(9, -3), F(3, 5)),
                    }
                    : System.Array.Empty<Bumper>(),
                sandZones: Keep(Element.Sand)
                    ? new[] { new ZonePolygon(new[] { V(5, 1), V(7, 1), V(7, 3), V(5, 3) }) }
                    : System.Array.Empty<ZonePolygon>(),
                waterZones: Keep(Element.Water)
                    // Moved 2026-08-18 from the bottom-right corner, which the
                    // scripted ball never reached: the old fixture carried a
                    // water zone the golden hash could not have covered.
                    ? new[] { new ZonePolygon(new[] { V(8, -2), V(10, -2), V(10, 0), V(8, 0) }) }
                    : System.Array.Empty<ZonePolygon>(),
                iceZones: Keep(Element.Ice)
                    // A vertical band the ball crosses on the way out and back.
                    // Kept clear of the sand (x >= 5): sand wins the friction
                    // tie-break, so an overlap would hide the ice it covers.
                    ? new[] { new ZonePolygon(new[] { V(1, -5), V(4, -5), V(4, 5), V(1, 5) }) }
                    : System.Array.Empty<ZonePolygon>(),
                gates: Keep(Element.Gates)
                    // Across the left half of the box: the ball is thrown back
                    // this way repeatedly, and the gate refuses the return trip.
                    ? new[]
                    {
                        new OneWayGate(V(1, -4), V(1, 4), new Vec2Fix(Fix64.One, Fix64.Zero)),
                    }
                    : System.Array.Empty<OneWayGate>(),
                ramps: Keep(Element.Ramps)
                    // A broad band across the left half rather than a patch on
                    // one line: this fixture is a coverage harness, not a
                    // playable hole, and an element the ball can dodge covers
                    // nothing. Wide is how a single trajectory stays honest.
                    ? new[]
                    {
                        new RampZone(
                            new ZonePolygon(new[] { V(-1, -5), V(5, -5), V(5, 5), V(-1, 5) }),
                            new Vec2Fix(F(2, 1), Fix64.Zero)),
                    }
                    : System.Array.Empty<RampZone>(),
                portals: Keep(Element.Portals)
                    // Mouths sit ON the measured path rather than near it.
                    ? new[]
                    {
                        new Portal(V(2, 0), V(9, -4), F(3, 4)),
                        new Portal(V(9, -4), V(2, 0), F(3, 4)),
                    }
                    : System.Array.Empty<Portal>(),
                windmills: Keep(Element.Windmills)
                    ? new[]
                    {
                        // Long blades on the box's centre, for the same reason the
                        // ramp is wide: the swept disc is hard to cross without
                        // meeting one.
                        new Windmill(V(5, 0), F(2, 1), bladeCount: 2, omegaSteps: 3, phase0: 128),
                    }
                    : System.Array.Empty<Windmill>());
        }

        /// <summary>
        /// Sixteen shots sweeping the whole box. Length is the point: one
        /// element only has to be met once, and a longer wander is far easier
        /// to keep honest than a short one threaded through ten obstacles.
        /// </summary>
        private static readonly ShotInput[] ShotScript =
        {
            new ShotInput(0, 255),    // full power east
            new ShotInput(100, 230),  // up-right
            new ShotInput(950, 255),  // down-right
            new ShotInput(128, 200),  // diagonal bounce run
            new ShotInput(512, 180),  // straight back west
            new ShotInput(300, 255),  // steep left-up carom
            new ShotInput(700, 210),  // down-left
            new ShotInput(64, 140),   // gentle chip up-right
            new ShotInput(896, 240),  // hard south-east
            new ShotInput(160, 255),  // north-east sweep
            new ShotInput(448, 200),  // west-north-west
            new ShotInput(768, 255),  // due south
            new ShotInput(32, 180),   // east, medium
            new ShotInput(608, 220),  // south-west
            new ShotInput(224, 160),  // north
            new ShotInput(864, 255),  // south-east again
        };

        private static GolfSim Run(CourseData course)
        {
            var sim = new GolfSim(course, SimConfig.Default);
            int nextShot = 0;
            for (int tick = 0; tick < 10_000; tick++)
            {
                if (sim.IsAtRest && !sim.IsHoled && nextShot < ShotScript.Length)
                {
                    sim.Shoot(ShotScript[nextShot]);
                    nextShot++;
                }

                sim.Tick();
            }

            return sim;
        }

        private static ulong Run10K() => Run(FixtureCourse()).StateHash();

        [Test]
        public void TenThousandTicks_TwoRuns_IdenticalHash()
        {
            Assert.That(Run10K(), Is.EqualTo(Run10K()),
                "two in-process runs of the same script must be bit-identical");
        }

        [Test]
        public void TenThousandTicks_MatchesCommittedGoldenHash()
        {
            var actual = Run10K();
            Assert.That(actual, Is.EqualTo(GoldenHash10K),
                $"sim state hash after 10k ticks changed — actual: {actual}UL. " +
                "If this change is intentional, update GoldenHash10K and explain in the commit.");
        }

        /// <summary>
        /// The guard on the guard. A golden hash only covers what the ball
        /// actually meets, so an element parked off the path would sit in the
        /// fixture proving nothing. Removing any one of them must move the
        /// hash — if this fails, that element is decoration, not coverage.
        /// </summary>
        [TestCase(Element.Bumpers)]
        [TestCase(Element.Sand)]
        [TestCase(Element.Water)]
        [TestCase(Element.Ice)]
        [TestCase(Element.Gates)]
        [TestCase(Element.Ramps)]
        [TestCase(Element.Portals)]
        [TestCase(Element.Windmills)]
        public void EveryElement_ChangesTheHash(Element element)
        {
            // Compared against the LIVE fixture hash, not the committed
            // constant: a stale constant would make every variant look
            // different and quietly pass a fixture that touches nothing.
            Assert.That(Run(FixtureCourse(element)).StateHash(), Is.Not.EqualTo(Run10K()),
                $"{element} can be removed without changing the run — the fixture does not exercise it");
        }

        /// <summary>
        /// The counters say the same thing in the other direction: the ball
        /// really does bounce, splash and teleport its way through the script.
        /// </summary>
        [Test]
        public void TheRun_TouchesEveryElementItShould()
        {
            var sim = Run(FixtureCourse());
            Assert.Multiple(() =>
            {
                Assert.That(sim.WallHitCount, Is.GreaterThan(0), "walls");
                Assert.That(sim.BumperHitCount, Is.GreaterThan(0), "bumpers");
                Assert.That(sim.WaterEntryCount, Is.GreaterThan(0), "water");
                Assert.That(sim.GateHitCount, Is.GreaterThan(0), "gate");
                Assert.That(sim.PortalTransitCount, Is.GreaterThan(0), "portal");
                Assert.That(sim.WindmillHitCount, Is.GreaterThan(0), "windmill blades");
                Assert.That(sim.TouchedHazard, Is.True, "sand or ice");
            });
        }
    }
}
