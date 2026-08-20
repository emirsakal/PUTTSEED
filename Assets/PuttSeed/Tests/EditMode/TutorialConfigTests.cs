using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.Sim;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The tutorial's promise, held to: a lesson's course contains EXACTLY the
    /// elements the lesson declares — every one it names, and nothing it does
    /// not. Hand-picked seeds are what rot silently; a generator change could
    /// leave "the blades never stop turning" printed over a course with no
    /// windmill on it, or drop a water hazard into the opening lesson, and
    /// nothing would fail until a player noticed. Something did exactly that
    /// once, which is why this file exists.
    ///
    /// Generation runs under the SAME config the game plays — the FeelConfig
    /// asset, not core's defaults — because acceptance depends on solvability
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
                simConfig, SolverConfig.ForVersion(stage.ConfigVersion)).Course;
        }

        /// <summary>
        /// Declared means present; undeclared means absent. Both halves matter:
        /// the first keeps a lesson from teaching nothing, the second keeps a
        /// beginner from meeting an element the hint never mentions.
        /// </summary>
        private static void AssertElement(TutorialConfig.Stage stage, int count, TutorialConfig.Lesson element)
        {
            bool declared = (stage.Teaches & element) != 0;
            if (declared)
            {
                Assert.That(count, Is.GreaterThan(0),
                    $"seed {stage.Seed} teaches {element} and grows none of it");
            }
            else
            {
                Assert.That(count, Is.Zero,
                    $"seed {stage.Seed} does not teach {element}, but the player meets it there");
            }
        }

        [Test]
        public void EveryLesson_ContainsExactlyWhatItDeclares()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                var course = Generate(stage);
                AssertElement(stage, course.Bumpers.Length, TutorialConfig.Lesson.Bumper);
                AssertElement(stage, course.SandZones.Length, TutorialConfig.Lesson.Sand);
                AssertElement(stage, course.IceZones.Length, TutorialConfig.Lesson.Ice);
                AssertElement(stage, course.WaterZones.Length, TutorialConfig.Lesson.Water);
                AssertElement(stage, course.Gates.Length, TutorialConfig.Lesson.Gate);
                AssertElement(stage, course.Ramps.Length, TutorialConfig.Lesson.Ramp);
                AssertElement(stage, course.Portals.Length, TutorialConfig.Lesson.Portal);
                AssertElement(stage, course.Windmills.Length, TutorialConfig.Lesson.Windmill);
            }
        }

        [Test]
        public void TheOpeningLesson_IsBareGround()
        {
            Assert.That(TutorialConfig.Stages[0].Teaches, Is.EqualTo(TutorialConfig.Lesson.Shot),
                "the first lesson is the shot itself — it may declare no element at all");
        }

        [Test]
        public void EveryElementInTheGame_IsTaughtSomewhere()
        {
            var taught = TutorialConfig.Lesson.Shot;
            foreach (var stage in TutorialConfig.Stages)
            {
                taught |= stage.Teaches;
            }

            foreach (TutorialConfig.Lesson element in System.Enum.GetValues(typeof(TutorialConfig.Lesson)))
            {
                if (element == TutorialConfig.Lesson.Shot)
                {
                    continue;
                }

                Assert.That(taught & element, Is.EqualTo(element),
                    $"{element} ships in the game and no lesson teaches it");
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
                    SimConfig.Default, SolverConfig.ForVersion(stage.ConfigVersion));

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
