using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The framing guarantee: no part of a course is ever drawn under the
    /// chrome. A ball resting beneath a button is not a tidiness problem — the
    /// button takes the touch that was meant to aim it — so the free band is
    /// arithmetic, not eyeballing.
    /// </summary>
    public class CameraFramerTests
    {
        // A 20:9 phone held upright.
        private const float PhoneAspect = 0.462f;

        /// <summary>Where a world Y lands on screen, 0 at the bottom, 1 at the top.</summary>
        private static float ScreenFraction(float worldY, float camY, float size)
            => (worldY - (camY - size)) / (2f * size);

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
                        float size = CameraFramer.OrthographicSizeFor(
                            half, PhoneAspect, top - CameraFramer.BottomChrome);
                        float camY = CameraFramer.CameraOffsetFor(size, CameraFramer.BottomChrome, top);

                        Assert.That(ScreenFraction(half.y, camY, size),
                            Is.LessThanOrEqualTo(top + 1e-4f),
                            $"half {half}, top {top}: the course runs under the top bar");
                        Assert.That(ScreenFraction(-half.y, camY, size),
                            Is.GreaterThanOrEqualTo(CameraFramer.BottomChrome - 1e-4f),
                            $"half {half}, top {top}: the course runs under the button row");
                    }
                }
            }
        }

        [Test]
        public void TheCourseAlsoFitsAcrossTheScreen()
        {
            var half = new Vector2(6f, 2f);
            float size = CameraFramer.OrthographicSizeFor(
                half, PhoneAspect, CameraFramer.TopChrome - CameraFramer.BottomChrome);

            Assert.That(size * PhoneAspect, Is.GreaterThanOrEqualTo(half.x + 0.8f - 1e-4f));
        }

        [Test]
        public void AWideCourse_PaysNothingForTheChrome()
        {
            // Width-limited: the band only ever constrains the vertical fit, so
            // reserving chrome must not shrink a hole that was never height
            // limited in the first place.
            var wide = new Vector2(9f, 1.5f);
            float full = CameraFramer.OrthographicSizeFor(wide, PhoneAspect, 1f);
            float banded = CameraFramer.OrthographicSizeFor(
                wide, PhoneAspect, CameraFramer.TopChrome - CameraFramer.BottomChrome);

            Assert.That(banded, Is.EqualTo(full).Within(1e-5f));
        }

        [Test]
        public void ATallCourse_ShrinksByExactlyTheBand()
        {
            var tall = new Vector2(1f, 8f);
            float band = CameraFramer.TopChrome - CameraFramer.BottomChrome;
            float full = CameraFramer.OrthographicSizeFor(tall, PhoneAspect, 1f);
            float banded = CameraFramer.OrthographicSizeFor(tall, PhoneAspect, band);

            Assert.That(banded, Is.EqualTo(full / band).Within(1e-4f));
        }

        [Test]
        public void ADegenerateAspectOrBand_DoesNotDivideByZero()
        {
            float size = CameraFramer.OrthographicSizeFor(new Vector2(3f, 3f), 0f, 0f);
            Assert.That(float.IsFinite(size), Is.True);
            Assert.That(size, Is.GreaterThan(0f));
        }
    }
}
