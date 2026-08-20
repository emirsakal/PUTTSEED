using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The framing guarantee: no part of a course is ever drawn under the
    /// chrome, at either roll. A ball resting beneath a button is not a
    /// tidiness problem — the button takes the touch that was meant to aim it
    /// — so the free band is arithmetic, not eyeballing.
    /// </summary>
    public class CameraFramerTests
    {
        // A 20:9 phone held upright.
        private const float PhoneAspect = 0.462f;

        /// <summary>Where a point on the screen's vertical axis lands, 0 bottom, 1 top.</summary>
        private static float ScreenFraction(float along, float offset, float size)
            => (along - (offset - size)) / (2f * size);

        [Test]
        public void NoCourseEdge_EverLandsUnderTheChrome()
        {
            float[] halves = { 1f, 2.5f, 6f, 11f };
            float[] tops = { CameraFramer.TopChrome, CameraFramer.TopChromeWithHint };

            foreach (float top in tops)
            {
                foreach (float x in halves)
                {
                    foreach (float y in halves)
                    {
                        var half = new Vector2(x, y);
                        bool rolled = CameraFramer.RollFor(half) != 0f;
                        float size = CameraFramer.OrthographicSizeFor(
                            half, PhoneAspect, top - CameraFramer.BottomChrome, rolled);
                        float offset = CameraFramer.CameraOffsetFor(size, CameraFramer.BottomChrome, top);

                        // The screen's height measures the long axis once rolled.
                        float halfAlong = rolled ? half.x : half.y;
                        Assert.That(ScreenFraction(halfAlong, offset, size),
                            Is.LessThanOrEqualTo(top + 1e-4f),
                            $"half {half}, top {top}: the course runs under the top bar");
                        Assert.That(ScreenFraction(-halfAlong, offset, size),
                            Is.GreaterThanOrEqualTo(CameraFramer.BottomChrome - 1e-4f),
                            $"half {half}, top {top}: the course runs under the button row");
                    }
                }
            }
        }

        [Test]
        public void TheCourseAlsoFitsAcrossTheScreen()
        {
            float[] halves = { 1f, 2.5f, 6f, 11f };
            foreach (float x in halves)
            {
                foreach (float y in halves)
                {
                    var half = new Vector2(x, y);
                    bool rolled = CameraFramer.RollFor(half) != 0f;
                    float size = CameraFramer.OrthographicSizeFor(
                        half, PhoneAspect, CameraFramer.TopChrome - CameraFramer.BottomChrome, rolled);
                    float halfAcross = rolled ? half.y : half.x;

                    Assert.That(size * PhoneAspect,
                        Is.GreaterThanOrEqualTo(halfAcross + 0.8f - 1e-4f), $"half {half} is cropped sideways");
                }
            }
        }

        [Test]
        public void WideCoursesTurnOntoTheLongAxis_TallOnesStayUpright()
        {
            Assert.That(CameraFramer.RollFor(new Vector2(6f, 2f)), Is.EqualTo(90f));
            Assert.That(CameraFramer.RollFor(new Vector2(2f, 6f)), Is.EqualTo(0f));
            Assert.That(CameraFramer.RollFor(new Vector2(4f, 4f)), Is.EqualTo(0f),
                "a square hole gains nothing from turning, so it does not turn");
        }

        [Test]
        public void RollingAWideCourse_FillsFarMoreOfThePhone()
        {
            var wide = new Vector2(6f, 2f); // 12 x 4 — the shape that started this
            float band = CameraFramer.TopChrome - CameraFramer.BottomChrome;
            float upright = CameraFramer.OrthographicSizeFor(wide, PhoneAspect, band, rolled: false);
            float rolled = CameraFramer.OrthographicSizeFor(wide, PhoneAspect, band, rolled: true);

            Assert.That(rolled, Is.LessThan(upright * 0.7f),
                "turning a wide hole onto the long axis should shrink the view a lot, "
                + "which is the same thing as the course growing on screen");
        }

        [Test]
        public void ACourseLimitedByItsLongAxis_ShrinksByExactlyTheBand()
        {
            var tall = new Vector2(1f, 8f);
            float band = CameraFramer.TopChrome - CameraFramer.BottomChrome;
            float full = CameraFramer.OrthographicSizeFor(tall, PhoneAspect, 1f, rolled: false);
            float banded = CameraFramer.OrthographicSizeFor(tall, PhoneAspect, band, rolled: false);

            Assert.That(banded, Is.EqualTo(full / band).Within(1e-4f));
        }

        [Test]
        public void ADegenerateAspectOrBand_DoesNotDivideByZero()
        {
            float size = CameraFramer.OrthographicSizeFor(new Vector2(3f, 3f), 0f, 0f, rolled: false);
            Assert.That(float.IsFinite(size), Is.True);
            Assert.That(size, Is.GreaterThan(0f));
        }
    }
}
