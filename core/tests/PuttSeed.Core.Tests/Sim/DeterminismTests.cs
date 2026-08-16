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
        /// Re-frozen 2026-08-16 when an ice zone joined the fixture, so the
        /// flagship regression hash covers all six elements.
        /// </summary>
        private const ulong GoldenHash10K = 12853565983001025895UL;

        private static Vec2Fix V(int x, int y) => new Vec2Fix(Fix64.FromInt(x), Fix64.FromInt(y));

        /// <summary>Fixture course: box walls, bumpers, sand, ice, water, far hole.</summary>
        // Par 10 keeps the stroke limit (par + 3) above anything the 8-shot
        // script plus water penalties can reach, so no shot is ever refused and
        // the golden hash predates the stroke-limit rule unchanged.
        private static CourseData FixtureCourse() => new CourseData(
            startPosition: V(0, 0),
            holePosition: V(10, 4),
            par: 10,
            walls: new[]
            {
                new WallSegment(V(-1, -5), V(11, -5)),
                new WallSegment(V(11, -5), V(11, 5)),
                new WallSegment(V(11, 5), V(-1, 5)),
                new WallSegment(V(-1, 5), V(-1, -5)),
            },
            bumpers: new[]
            {
                new Bumper(V(5, 0), Fix64.FromFraction(3, 10)),
                new Bumper(V(7, -2), Fix64.FromFraction(3, 10)),
            },
            sandZones: new[]
            {
                new ZonePolygon(new[] { V(2, 2), V(4, 2), V(4, 4), V(2, 4) }),
            },
            waterZones: new[]
            {
                new ZonePolygon(new[] { V(8, -4), V(10, -4), V(10, -2), V(8, -2) }),
            },
            iceZones: new[]
            {
                // Straddles the start→bumper line so the very first scripted
                // shot glides across it — ice physics is in the hash for sure.
                new ZonePolygon(new[] { V(2, -1), V(4, -1), V(4, 1), V(2, 1) }),
            });

        private static readonly ShotInput[] ShotScript =
        {
            new ShotInput(0, 255),    // full power into the far wall / bumpers
            new ShotInput(100, 230),  // up-right, through the sand corner
            new ShotInput(950, 255),  // down-right, toward the water pocket
            new ShotInput(128, 200),  // diagonal bounce run
            new ShotInput(512, 180),  // straight back left
            new ShotInput(300, 255),  // steep left-up carom
            new ShotInput(700, 210),  // down-left
            new ShotInput(64, 140),   // gentle chip up-right
        };

        private static ulong Run10K()
        {
            var sim = new GolfSim(FixtureCourse(), SimConfig.Default);
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

            return sim.StateHash();
        }

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
    }
}
