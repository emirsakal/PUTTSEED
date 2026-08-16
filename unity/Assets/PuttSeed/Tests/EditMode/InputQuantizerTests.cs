using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    public class InputQuantizerTests
    {
        [Test]
        public void CardinalDirections_MapToExactAngleIndices()
        {
            Assert.That(InputQuantizer.FromDrag(Vector2.right, 1f, 1f).AngleIndex, Is.EqualTo(0));
            Assert.That(InputQuantizer.FromDrag(Vector2.up, 1f, 1f).AngleIndex, Is.EqualTo(256));
            Assert.That(InputQuantizer.FromDrag(Vector2.left, 1f, 1f).AngleIndex, Is.EqualTo(512));
            Assert.That(InputQuantizer.FromDrag(Vector2.down, 1f, 1f).AngleIndex, Is.EqualTo(768));
        }

        [Test]
        public void FullDrag_GivesMaxPower()
        {
            var shot = InputQuantizer.FromDrag(Vector2.right * 2.5f, 2.5f, 1.35f);
            Assert.That(shot.PowerIndex, Is.EqualTo(255));
        }

        [Test]
        public void OverDrag_ClampsToMaxPower()
        {
            var shot = InputQuantizer.FromDrag(Vector2.right * 99f, 2.5f, 1.35f);
            Assert.That(shot.PowerIndex, Is.EqualTo(255));
        }

        [Test]
        public void ZeroDrag_GivesZeroPower()
        {
            var shot = InputQuantizer.FromDrag(Vector2.zero, 2.5f, 1.35f);
            Assert.That(shot.PowerIndex, Is.EqualTo(0));
        }

        [Test]
        public void Power_IsMonotonicInDragLength()
        {
            int last = -1;
            for (int i = 1; i <= 20; i++)
            {
                var shot = InputQuantizer.FromDrag(Vector2.right * (i * 0.125f), 2.5f, 1.35f);
                Assert.That(shot.PowerIndex, Is.GreaterThanOrEqualTo(last));
                last = shot.PowerIndex;
            }
        }

        [Test]
        public void PowerCurve_GivesFineLowEndControl()
        {
            // With exponent > 1, half drag maps below half power.
            var shot = InputQuantizer.FromDrag(Vector2.right * 1.25f, 2.5f, 2f);
            Assert.That(shot.PowerIndex, Is.LessThan(128));
        }

        [Test]
        public void AngleIndex_IsAlwaysInRange()
        {
            for (int deg = -360; deg <= 720; deg += 15)
            {
                float rad = deg * Mathf.Deg2Rad;
                var v = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                var shot = InputQuantizer.FromDrag(v, 1f, 1f);
                Assert.That(shot.AngleIndex, Is.InRange(0, 1023), $"deg {deg}");
            }
        }

        [Test]
        public void SameDrag_SameShot_Deterministic()
        {
            var v = new Vector2(1.234f, -0.567f);
            var a = InputQuantizer.FromDrag(v, 2.5f, 1.35f);
            var b = InputQuantizer.FromDrag(v, 2.5f, 1.35f);
            Assert.That(a.AngleIndex, Is.EqualTo(b.AngleIndex));
            Assert.That(a.PowerIndex, Is.EqualTo(b.PowerIndex));
        }
    }
}
