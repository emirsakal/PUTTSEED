using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.Sim;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The tutorial's promise, held to: every lesson's hand-picked seed really
    /// does grow a course containing the thing its hint talks about. Curated
    /// constants are exactly what rots silently — a generator change could
    /// leave "the blades never stop turning" printed over a course with no
    /// windmill on it, and nothing would fail until a player noticed.
    ///
    /// Generation runs under the SAME config the game plays: the FeelConfig
    /// asset, not core's defaults, because acceptance depends on solvability
    /// and solvability depends on friction.
    /// </summary>
    public class TutorialConfigTests
    {
        private static CourseData Generate(TutorialConfig.Stage stage)
        {
            var feel = Resources.Load<FeelConfig>("FeelConfig");
            var simConfig = feel != null ? feel.BuildSimConfig() : SimConfig.Default;
            return CourseGenerator.Generate(stage.Seed,
                GeneratorConfig.ForVersion(stage.ConfigVersion),
                simConfig, SolverConfig.Default).Course;
        }

        private static int CountOf(CourseData course, TutorialConfig.Lesson lesson) => lesson switch
        {
            TutorialConfig.Lesson.Bumper => course.Bumpers.Length,
            TutorialConfig.Lesson.Sand => course.SandZones.Length,
            TutorialConfig.Lesson.Ice => course.IceZones.Length,
            TutorialConfig.Lesson.Water => course.WaterZones.Length,
            TutorialConfig.Lesson.Gate => course.Gates.Length,
            TutorialConfig.Lesson.Ramp => course.Ramps.Length,
            TutorialConfig.Lesson.Portal => course.Portals.Length,
            TutorialConfig.Lesson.Windmill => course.Windmills.Length,
            _ => 0,
        };

        [Test]
        public void EveryLesson_ActuallyContainsWhatItTeaches()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                if (stage.Teaches == TutorialConfig.Lesson.Shot)
                {
                    continue; // the shot is taught by the absence of everything
                }

                Assert.That(CountOf(Generate(stage), stage.Teaches), Is.GreaterThan(0),
                    $"seed {stage.Seed} is the {stage.Teaches} lesson but grows no {stage.Teaches}");
            }
        }

        [Test]
        public void TheFirstLesson_IsCleanGround()
        {
            var course = Generate(TutorialConfig.Stages[0]);
            int hazards = course.Bumpers.Length + course.SandZones.Length + course.IceZones.Length
                + course.WaterZones.Length + course.Gates.Length + course.Ramps.Length
                + course.Portals.Length + course.Windmills.Length;

            Assert.That(TutorialConfig.Stages[0].Teaches, Is.EqualTo(TutorialConfig.Lesson.Shot));
            Assert.That(hazards, Is.Zero,
                "the first lesson is the shot itself — nothing else belongs on that course");
        }

        [Test]
        public void EachWaveLesson_IntroducesOneNewElementAtATime()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                if (stage.ConfigVersion < 2)
                {
                    continue; // v1 courses cannot hold a wave element at all
                }

                var course = Generate(stage);
                int strangers =
                    (stage.Teaches == TutorialConfig.Lesson.Gate ? 0 : course.Gates.Length)
                    + (stage.Teaches == TutorialConfig.Lesson.Ramp ? 0 : course.Ramps.Length)
                    + (stage.Teaches == TutorialConfig.Lesson.Portal ? 0 : course.Portals.Length)
                    + (stage.Teaches == TutorialConfig.Lesson.Windmill ? 0 : course.Windmills.Length);

                Assert.That(strangers, Is.Zero,
                    $"seed {stage.Seed} teaches {stage.Teaches} but meets the player with another "
                    + "new element on the same course");
            }
        }

        [Test]
        public void NoLesson_IsAThemedDay()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                Assert.That(DailyMutators.ForSeed(stage.Seed, stage.ConfigVersion),
                    Is.EqualTo(DailyMutator.None),
                    $"seed {stage.Seed}: a lesson has to be the plain game — a beginner cannot "
                    + "tell an icy day from a broken one");
            }
        }

        [Test]
        public void EveryLesson_IsSolvableWithinItsStrokeLimit()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                var result = CourseGenerator.Generate(stage.Seed,
                    GeneratorConfig.ForVersion(stage.ConfigVersion),
                    SimConfig.Default, SolverConfig.Default);

                Assert.That(result.AuthorStrokes, Is.LessThanOrEqualTo(result.Course.Par),
                    $"seed {stage.Seed}: the author solution must reach par");
            }
        }

        [Test]
        public void EveryHint_HasTurkish()
        {
            Loc.Apply("tr");
            try
            {
                foreach (var stage in TutorialConfig.Stages)
                {
                    Assert.That(Loc.Tr(stage.Hint), Is.Not.EqualTo(stage.Hint),
                        $"the {stage.Teaches} hint is not translated");
                }
            }
            finally
            {
                Loc.Apply("en"); // never leak a language into other tests
            }
        }
    }
}
