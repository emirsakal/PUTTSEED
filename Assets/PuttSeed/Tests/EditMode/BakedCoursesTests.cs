using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The shipped courses have to be the courses the game would have made.
    ///
    /// Baking trades a proof for a lookup, and the trade is only honest while
    /// the two agree. If they ever drift — a generator change, a physics knob,
    /// a stale pack left in Resources — the game would hand players a course
    /// its own solver never approved, with a par nobody can reach. That is the
    /// one failure this whole idea can produce, so it is the one this file
    /// exists to catch.
    /// </summary>
    public class BakedCoursesTests
    {
        private const int Version = 4;

        private static PuttSeed.Core.Sim.SimConfig BaseConfig()
        {
            var feel = Resources.Load<FeelConfig>("FeelConfig");
            Assert.That(feel, Is.Not.Null, "the game's own physics asset is missing");
            return feel.BuildSimConfig();
        }

        [Test]
        public void ABakedCourse_IsWhatTheGeneratorWouldHaveMade()
        {
            ulong seed = JourneyConfig.Seeds[0];
            Assert.That(BakedCourses.TryGet(BakedCourses.Pack.Journey, seed, Version, out var baked),
                Is.True, $"journey seed {seed} is not in the shipped pack");

            var live = CourseGenerator.Generate(
                seed,
                GeneratorConfig.ForVersion(Version),
                DailyMutators.Apply(BaseConfig(), seed, Version),
                SolverConfig.ForVersion(Version));

            Assert.That(baked.Course.Par, Is.EqualTo(live.Course.Par));
            Assert.That(baked.Difficulty, Is.EqualTo(live.Difficulty));
            Assert.That(baked.AuthorStrokes, Is.EqualTo(live.AuthorStrokes));
            Assert.That(baked.DifficultyScore, Is.EqualTo(live.DifficultyScore));
            Assert.That(baked.Course.Walls.Length, Is.EqualTo(live.Course.Walls.Length));
            Assert.That(baked.Course.StartPosition.X.Raw, Is.EqualTo(live.Course.StartPosition.X.Raw));
            Assert.That(baked.Course.StartPosition.Y.Raw, Is.EqualTo(live.Course.StartPosition.Y.Raw));
            Assert.That(baked.Course.HolePosition.X.Raw, Is.EqualTo(live.Course.HolePosition.X.Raw));
            Assert.That(baked.Course.HolePosition.Y.Raw, Is.EqualTo(live.Course.HolePosition.Y.Raw));

            for (int i = 0; i < live.Course.Walls.Length; i++)
            {
                Assert.That(baked.Course.Walls[i].A.X.Raw, Is.EqualTo(live.Course.Walls[i].A.X.Raw), $"wall {i}");
                Assert.That(baked.Course.Walls[i].B.Y.Raw, Is.EqualTo(live.Course.Walls[i].B.Y.Raw), $"wall {i}");
            }
        }

        [Test]
        public void EveryTutorialLessonIsShipped()
        {
            // The tutorial is the first thing a new player meets, on the phone
            // where generation is slowest and patience is shortest.
            foreach (var stage in TutorialConfig.Stages)
            {
                Assert.That(
                    BakedCourses.TryGet(BakedCourses.Pack.Tutorial, stage.Seed, stage.ConfigVersion, out _),
                    Is.True, $"tutorial seed {stage.Seed} is not in the shipped pack");
            }
        }

        [Test]
        public void EveryJourneyLevelIsShipped()
        {
            for (int level = 0; level < JourneyConfig.Seeds.Length; level++)
            {
                Assert.That(
                    BakedCourses.TryGet(BakedCourses.Pack.Journey, JourneyConfig.Seeds[level], Version, out _),
                    Is.True, $"journey level {level + 1} is not in the shipped pack");
            }
        }

        [Test]
        public void ThePracticePoolCoversEveryDifficulty()
        {
            var entropy = new System.Random(1);
            foreach (Difficulty want in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
            {
                Assert.That(
                    BakedCourses.TryDrawPractice(want, PracticeCourses.Version, entropy, out _, out var drawn),
                    Is.True, $"nothing baked for {want}");
                Assert.That(drawn.Difficulty, Is.EqualTo(want), "the pool handed back the wrong bucket");
            }
        }

        [Test]
        public void TodaysDailyIsShipped()
        {
            // The window is finite by design, but it has to cover TODAY, or the
            // one hole everybody opens first is the one that takes half a
            // minute to appear.
            var utc = System.DateTime.UtcNow;
            ulong seed = DailySeed.FromUtcDate(utc.Year, utc.Month, utc.Day);
            Assert.That(BakedCourses.TryGet(BakedCourses.Pack.Daily, seed, Version, out _), Is.True,
                "today's daily is outside the baked window — rebake with a later start date");
        }

        [Test]
        public void AVersionMismatchIsAMissRatherThanTheWrongCourse()
        {
            // The same seed grows a different hole under a different
            // generator. Handing back the v4 course for a v1 request would be
            // worse than making the player wait.
            Assert.That(BakedCourses.TryGet(BakedCourses.Pack.Journey, JourneyConfig.Seeds[0], 1, out _),
                Is.False);
        }
    }
}
