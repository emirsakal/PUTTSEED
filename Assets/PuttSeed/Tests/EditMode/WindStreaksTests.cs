using NUnit.Framework;
using PuttSeed.Unity;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The streak dressing on a windy day: sparse by contract, aligned with
    /// the wind, and born and dying softly at the course edges.
    /// </summary>
    public class WindStreaksTests
    {
        [Test]
        public void FadeIsSoftAtBothEndsAndFullInTheMiddle()
        {
            Assert.That(WindStreaks.FadeFor(0f), Is.EqualTo(0f));
            Assert.That(WindStreaks.FadeFor(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(WindStreaks.FadeFor(0.5f), Is.EqualTo(1f));
            Assert.That(WindStreaks.FadeFor(0.1f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(WindStreaks.FadeFor(0.9f), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void BuildDressesTheCourse_SparselyAndAlongTheWind()
        {
            var go = new GameObject("test");
            try
            {
                var streaks = go.AddComponent<WindStreaks>();
                var wind = new Vector2(0f, 0.65f); // straight up
                streaks.Build(wind, new Vector2(-4f, -6f), new Vector2(4f, 6f), seed: 1234);

                Assert.That(go.transform.childCount, Is.EqualTo(WindStreaks.Count),
                    "sparse is the contract — cross-mown stripes died for being busy");

                foreach (Transform child in go.transform)
                {
                    float z = child.localEulerAngles.z;
                    Assert.That(Mathf.DeltaAngle(z, 90f), Is.EqualTo(0f).Within(0.5f),
                        "a streak that does not travel with the wind is pointing a lie");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
