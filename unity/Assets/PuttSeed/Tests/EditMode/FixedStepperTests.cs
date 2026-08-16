using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class FixedStepperTests
    {
        private const double Tick = FixedStepper.TickSeconds;

        [Test]
        public void ZeroDelta_ProducesNoTicks()
        {
            var stepper = new FixedStepper();
            Assert.That(stepper.Advance(0.0), Is.EqualTo(0));
            Assert.That(stepper.Alpha, Is.EqualTo(0f));
        }

        [Test]
        public void ExactlyOneTick()
        {
            var stepper = new FixedStepper();
            Assert.That(stepper.Advance(Tick), Is.EqualTo(1));
            Assert.That(stepper.Alpha, Is.LessThan(0.001f));
        }

        [Test]
        public void FractionsAccumulateAcrossFrames()
        {
            var stepper = new FixedStepper();
            // 0.6 ticks per frame: ticks come out 0,1,1,0,1,... totaling 3 per 5 frames.
            int total = 0;
            for (int i = 0; i < 10; i++)
            {
                total += stepper.Advance(Tick * 0.6);
            }

            Assert.That(total, Is.EqualTo(6));
        }

        [Test]
        public void SixtyFpsFrame_YieldsTwoTicks()
        {
            var stepper = new FixedStepper();
            int ticks = stepper.Advance(1.0 / 60.0);
            Assert.That(ticks, Is.EqualTo(2));
        }

        [Test]
        public void HugeHitch_IsClampedByCatchUpCap()
        {
            var stepper = new FixedStepper(maxCatchUpTicks: 12);
            Assert.That(stepper.Advance(5.0), Is.EqualTo(12), "catch-up must be capped");
            // Backlog beyond the cap is dropped; alpha stays a valid fraction.
            Assert.That(stepper.Alpha, Is.InRange(0f, 1f));
            Assert.That(stepper.Advance(0.0), Is.EqualTo(0), "dropped backlog must not resurface");
        }

        [Test]
        public void Alpha_IsAlwaysInUnitRange()
        {
            var stepper = new FixedStepper();
            var deltas = new[] { 0.016, 0.007, 0.033, 0.001, 0.2, 0.0083 };
            foreach (var d in deltas)
            {
                stepper.Advance(d);
                Assert.That(stepper.Alpha, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void NegativeDelta_IsIgnored()
        {
            var stepper = new FixedStepper();
            Assert.That(stepper.Advance(-1.0), Is.EqualTo(0));
            Assert.That(stepper.Alpha, Is.EqualTo(0f));
        }
    }
}
