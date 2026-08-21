using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    /// <summary>
    /// A baked course has to come back byte-identical, because the device
    /// plays what it reads. A field silently lost in the round trip is a
    /// course that looks right and simulates differently — the one failure
    /// this project cannot tolerate, since every replay code and every shared
    /// score assumes two devices agree.
    /// </summary>
    [TestFixture]
    public class CourseBakeTests
    {
        /// <summary>
        /// Courses carrying the full element wave, so the format is exercised
        /// rather than merely executed.
        /// </summary>
        private static List<CourseBake.Entry> Sample()
        {
            var entries = new List<CourseBake.Entry>();
            for (ulong seed = 1; seed <= 12; seed++)
            {
                try
                {
                    entries.Add(new CourseBake.Entry(seed, CourseGenerator.Generate(
                        seed, GeneratorConfig.V2, SimConfig.Default, SolverConfig.Default)));
                }
                catch (System.InvalidOperationException)
                {
                    // A seed that will not grow is not this test's business.
                }
            }

            return entries;
        }

        [Test]
        public void EveryFieldSurvivesTheRoundTrip()
        {
            var original = Sample();
            Assert.That(original, Is.Not.Empty);

            var bytes = CourseBake.Write(original, generatorVersion: 2);
            var restored = CourseBake.Read(bytes, out int version);

            Assert.That(version, Is.EqualTo(2));
            Assert.That(restored.Count, Is.EqualTo(original.Count));
            for (int i = 0; i < original.Count; i++)
            {
                AssertSame(original[i], restored[i]);
            }
        }

        [Test]
        public void TheSampleActuallyCarriesEveryElement()
        {
            // Without this, the round trip could pass for years while never
            // once serializing a portal.
            bool bumpers = false, sand = false, water = false, ice = false;
            bool gates = false, ramps = false, portals = false, mills = false;
            foreach (var entry in Sample())
            {
                var c = entry.Result.Course;
                bumpers |= c.Bumpers.Length > 0;
                sand |= c.SandZones.Length > 0;
                water |= c.WaterZones.Length > 0;
                ice |= c.IceZones.Length > 0;
                gates |= c.Gates.Length > 0;
                ramps |= c.Ramps.Length > 0;
                portals |= c.Portals.Length > 0;
                mills |= c.Windmills.Length > 0;
            }

            Assert.That(bumpers && sand && water && ice, Is.True, "the five-element wave is missing");
            Assert.That(gates && ramps && portals && mills, Is.True, "the 2026-08 wave is missing");
        }

        [Test]
        public void ABakedCourseSimulatesIdentically()
        {
            // The claim the whole idea rests on: play the author solution
            // through the restored course and reach the cup in the same
            // strokes. Geometry that reads back "equal" but drifts by one raw
            // unit would show up here and nowhere else.
            var original = Sample()[0];
            var restored = CourseBake.Read(
                CourseBake.Write(new[] { original }, 2), out _)[0];

            Assert.That(Play(restored.Result), Is.EqualTo(Play(original.Result)));
            Assert.That(Play(restored.Result), Is.EqualTo(original.Result.AuthorStrokes));
        }

        [Test]
        public void AForeignBlobIsRefusedRatherThanMisread()
        {
            Assert.Throws<InvalidDataException>(
                () => CourseBake.Read(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, out _));

            var good = CourseBake.Write(Sample(), 2);
            good[4] = 99; // the format byte
            Assert.Throws<InvalidDataException>(() => CourseBake.Read(good, out _));
        }

        private static int Play(GenerationResult result)
        {
            var sim = new GolfSim(result.Course, SimConfig.Default);
            foreach (var shot in result.AuthorSolution)
            {
                sim.Shoot(shot);
                for (int i = 0; i < 4000 && !sim.IsAtRest; i++)
                {
                    sim.Tick();
                }
            }

            return sim.IsHoled ? sim.Strokes : -1;
        }

        private static void AssertSame(CourseBake.Entry expected, CourseBake.Entry actual)
        {
            Assert.That(actual.Seed, Is.EqualTo(expected.Seed));
            Assert.That(actual.Result.AuthorStrokes, Is.EqualTo(expected.Result.AuthorStrokes));
            Assert.That(actual.Result.Difficulty, Is.EqualTo(expected.Result.Difficulty));
            Assert.That(actual.Result.DifficultyScore, Is.EqualTo(expected.Result.DifficultyScore));
            Assert.That(actual.Result.Attempts, Is.EqualTo(expected.Result.Attempts));
            Assert.That(actual.Result.RelaxationLevel, Is.EqualTo(expected.Result.RelaxationLevel));

            var expectedShots = expected.Result.AuthorSolution;
            var actualShots = actual.Result.AuthorSolution;
            Assert.That(actualShots.Length, Is.EqualTo(expectedShots.Length));
            for (int i = 0; i < expectedShots.Length; i++)
            {
                Assert.That(actualShots[i].AngleIndex, Is.EqualTo(expectedShots[i].AngleIndex));
                Assert.That(actualShots[i].PowerIndex, Is.EqualTo(expectedShots[i].PowerIndex));
            }

            var a = expected.Result.Course;
            var b = actual.Result.Course;
            Assert.That(b.Par, Is.EqualTo(a.Par));
            AssertSame(a.StartPosition, b.StartPosition);
            AssertSame(a.HolePosition, b.HolePosition);

            Assert.That(b.Walls.Length, Is.EqualTo(a.Walls.Length));
            for (int i = 0; i < a.Walls.Length; i++)
            {
                AssertSame(a.Walls[i].A, b.Walls[i].A);
                AssertSame(a.Walls[i].B, b.Walls[i].B);
            }

            Assert.That(b.Bumpers.Length, Is.EqualTo(a.Bumpers.Length));
            for (int i = 0; i < a.Bumpers.Length; i++)
            {
                AssertSame(a.Bumpers[i].Center, b.Bumpers[i].Center);
                Assert.That(b.Bumpers[i].Radius.Raw, Is.EqualTo(a.Bumpers[i].Radius.Raw));
            }

            AssertSame(a.SandZones, b.SandZones);
            AssertSame(a.WaterZones, b.WaterZones);
            AssertSame(a.IceZones, b.IceZones);

            Assert.That(b.Gates.Length, Is.EqualTo(a.Gates.Length));
            for (int i = 0; i < a.Gates.Length; i++)
            {
                AssertSame(a.Gates[i].A, b.Gates[i].A);
                AssertSame(a.Gates[i].B, b.Gates[i].B);
                AssertSame(a.Gates[i].PassNormal, b.Gates[i].PassNormal);
            }

            Assert.That(b.Ramps.Length, Is.EqualTo(a.Ramps.Length));
            for (int i = 0; i < a.Ramps.Length; i++)
            {
                AssertSame(new[] { a.Ramps[i].Area }, new[] { b.Ramps[i].Area });
                AssertSame(a.Ramps[i].Accel, b.Ramps[i].Accel);
            }

            Assert.That(b.Portals.Length, Is.EqualTo(a.Portals.Length));
            for (int i = 0; i < a.Portals.Length; i++)
            {
                AssertSame(a.Portals[i].Entry, b.Portals[i].Entry);
                AssertSame(a.Portals[i].Exit, b.Portals[i].Exit);
                Assert.That(b.Portals[i].Radius.Raw, Is.EqualTo(a.Portals[i].Radius.Raw));
            }

            Assert.That(b.Windmills.Length, Is.EqualTo(a.Windmills.Length));
            for (int i = 0; i < a.Windmills.Length; i++)
            {
                AssertSame(a.Windmills[i].Pivot, b.Windmills[i].Pivot);
                Assert.That(b.Windmills[i].BladeLength.Raw, Is.EqualTo(a.Windmills[i].BladeLength.Raw));
                Assert.That(b.Windmills[i].BladeCount, Is.EqualTo(a.Windmills[i].BladeCount));
                Assert.That(b.Windmills[i].OmegaSteps, Is.EqualTo(a.Windmills[i].OmegaSteps));
                Assert.That(b.Windmills[i].Phase0, Is.EqualTo(a.Windmills[i].Phase0));
            }
        }

        private static void AssertSame(ZonePolygon[] expected, ZonePolygon[] actual)
        {
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i].Vertices.Length, Is.EqualTo(expected[i].Vertices.Length));
                for (int v = 0; v < expected[i].Vertices.Length; v++)
                {
                    AssertSame(expected[i].Vertices[v], actual[i].Vertices[v]);
                }
            }
        }

        private static void AssertSame(PuttSeed.Core.FixedMath.Vec2Fix expected,
            PuttSeed.Core.FixedMath.Vec2Fix actual)
        {
            Assert.That(actual.X.Raw, Is.EqualTo(expected.X.Raw));
            Assert.That(actual.Y.Raw, Is.EqualTo(expected.Y.Raw));
        }
    }
}
