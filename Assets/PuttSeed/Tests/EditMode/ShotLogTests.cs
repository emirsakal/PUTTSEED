using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The scorecard's contract: one glyph per stroke, in stroke order, and
    /// the most telling thing that happened to each. It is the only part of a
    /// share a stranger can read, so a stroke silently losing its mark — or
    /// gaining one it never earned — is the failure worth catching.
    /// </summary>
    public class ShotLogTests
    {
        [Test]
        public void BeforeTheFirstShot_TheLineIsEmpty()
        {
            var log = new ShotLog();
            Assert.That(log.Glyphs(), Is.Empty);
        }

        [Test]
        public void EveryStroke_LeavesExactlyOneGlyph()
        {
            var log = new ShotLog();
            log.BeginShot();
            log.BeginShot();
            log.BeginShot();

            Assert.That(log.Shots.Count, Is.EqualTo(3));
            Assert.That(log.Glyphs(), Is.EqualTo("🟩🟩🟩"), "a clean roll is a green square");
        }

        [Test]
        public void MarksLandOnTheStrokeInProgress()
        {
            var log = new ShotLog();
            log.BeginShot();
            log.Record(ShotLog.Mark.Sand);
            log.BeginShot();
            log.Record(ShotLog.Mark.Holed);

            Assert.That(log.Glyphs(), Is.EqualTo("🟫⛳"));
        }

        [Test]
        public void TheCupOutranksEverythingElseOnTheStrokeThatDrops()
        {
            var log = new ShotLog();
            log.BeginShot();
            log.Record(ShotLog.Mark.Wall);
            log.Record(ShotLog.Mark.Bumper);
            log.Record(ShotLog.Mark.Holed);

            Assert.That(log.Glyphs(), Is.EqualTo("⛳"));
        }

        [Test]
        public void WaterOutranksTheOrdinary_BecauseItCostAStroke()
        {
            var log = new ShotLog();
            log.BeginShot();
            log.Record(ShotLog.Mark.Wall);
            log.Record(ShotLog.Mark.Sand);
            log.Record(ShotLog.Mark.Water);

            Assert.That(log.Glyphs(), Is.EqualTo("💧"));
        }

        [Test]
        public void ABankedShot_ReadsDifferentlyFromACleanOne()
        {
            var log = new ShotLog();
            log.BeginShot();
            log.Record(ShotLog.Mark.Wall);
            log.BeginShot();

            Assert.That(log.Glyphs(), Is.EqualTo("⬜🟩"));
        }

        [Test]
        public void MarksArrivingBeforeAnyShot_AreDropped()
        {
            var log = new ShotLog();
            log.Record(ShotLog.Mark.Water);

            Assert.That(log.Shots.Count, Is.Zero, "a mark with no stroke to belong to is not a stroke");
            Assert.That(log.Glyphs(), Is.Empty);
        }

        [Test]
        public void Reset_StartsTheRunOver()
        {
            var log = new ShotLog();
            log.BeginShot();
            log.Record(ShotLog.Mark.Windmill);
            log.Reset();

            Assert.That(log.Glyphs(), Is.Empty);
        }

        [Test]
        public void EveryElementHasItsOwnGlyph()
        {
            var marks = new[]
            {
                ShotLog.Mark.None, ShotLog.Mark.Wall, ShotLog.Mark.Bumper, ShotLog.Mark.Sand,
                ShotLog.Mark.Ice, ShotLog.Mark.Water, ShotLog.Mark.Gate, ShotLog.Mark.Ramp,
                ShotLog.Mark.Portal, ShotLog.Mark.Windmill, ShotLog.Mark.Holed,
            };

            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var mark in marks)
            {
                string glyph = ShotLog.Render(new[] { mark });
                Assert.That(seen.Add(glyph), Is.True, $"{mark} shares a glyph with something else");
            }
        }
    }
}
