using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.CourseGen
{
    /// <summary>
    /// The v1/v2 boundary of the 2026-08 element wave: v1 must never place a
    /// new element (journey levels and archived dailies depend on it), v2 must
    /// actually place each of them somewhere, within budget, with portal twins
    /// mirroring each other. Seeds run in parallel — generation is per-seed
    /// deterministic, so parallelism cannot affect outcomes.
    /// </summary>
    [TestFixture]
    public class NewElementGenerationTests
    {
        [Test]
        public void V1_NeverPlacesNewElements()
        {
            var failures = new ConcurrentBag<string>();
            Parallel.For(1, 61, seedInt =>
            {
                ulong seed = (ulong)seedInt;
                var result = CourseGenerator.Generate(
                    seed, GeneratorConfig.V1, SimConfig.Default, SolverConfig.Default);
                var c = result.Course;
                if (c.Gates.Length + c.Ramps.Length + c.Portals.Length + c.Windmills.Length != 0)
                {
                    failures.Add($"seed {seed}: v1 placed a new element");
                }
            });

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void V2_PlacesEveryElementTypeSomewhere_WithinBudget_TwinsMirrored()
        {
            var failures = new ConcurrentBag<string>();
            int gates = 0, ramps = 0, portalPairs = 0, mills = 0;

            Parallel.For(1, 201, seedInt =>
            {
                ulong seed = (ulong)seedInt;
                GenerationResult result;
                try
                {
                    result = CourseGenerator.Generate(
                        seed, GeneratorConfig.V2, SimConfig.Default, SolverConfig.Default);
                }
                catch (System.InvalidOperationException)
                {
                    return; // bounded failure is the property suite's concern
                }

                var c = result.Course;
                if (c.Gates.Length > GeneratorConfig.V2.MaxGates
                    || c.Ramps.Length > GeneratorConfig.V2.MaxRamps
                    || c.Portals.Length / 2 > GeneratorConfig.V2.MaxPortals
                    || c.Windmills.Length > GeneratorConfig.V2.MaxWindmills)
                {
                    failures.Add($"seed {seed}: element budget exceeded");
                }

                if (c.Portals.Length % 2 != 0)
                {
                    failures.Add($"seed {seed}: portals must come in pairs");
                }

                for (int i = 0; i + 1 < c.Portals.Length; i += 2)
                {
                    if (!c.Portals[i].Exit.Equals(c.Portals[i + 1].Entry)
                        || !c.Portals[i].Entry.Equals(c.Portals[i + 1].Exit))
                    {
                        failures.Add($"seed {seed}: portal twins do not mirror");
                    }
                }

                Interlocked.Add(ref gates, c.Gates.Length);
                Interlocked.Add(ref ramps, c.Ramps.Length);
                Interlocked.Add(ref portalPairs, c.Portals.Length / 2);
                Interlocked.Add(ref mills, c.Windmills.Length);
            });

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            Assert.That(gates, Is.GreaterThan(0), "no gate ever generated in 200 seeds");
            Assert.That(ramps, Is.GreaterThan(0), "no ramp ever generated in 200 seeds");
            Assert.That(portalPairs, Is.GreaterThan(0), "no portal pair ever generated in 200 seeds");
            Assert.That(mills, Is.GreaterThan(0), "no windmill ever generated in 200 seeds");
        }
    }
}
