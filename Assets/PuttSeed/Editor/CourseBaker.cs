#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Solves every course the game ships with, once, here — so the phone can
    /// read them instead.
    ///
    /// Measured on a 2018 mid-range phone (Kirin 710): a tutorial or journey
    /// level took fifteen to thirty seconds to open, and practice — which
    /// searches up to eight candidates for a difficulty bucket — took two
    /// minutes. The desktop number is 1.6 seconds per course, nearly all of it
    /// the solver proving no shorter solution exists. The phone is simply ten
    /// times slower at it.
    ///
    /// Since the simulation is bit-deterministic, that proof is portable. This
    /// runs it on the desktop, in parallel, against the REAL FeelConfig asset
    /// — not a mirror of its numbers, which is how a tutorial lesson once
    /// ended up with three bumpers nobody expected.
    /// </summary>
    public static class CourseBaker
    {
        private const string OutputDirectory = "Assets/PuttSeed/Resources/Courses";

        /// <summary>Practice pool size. Roughly a third lands in each bucket.</summary>
        private const int PracticePoolSize = 900;

        /// <summary>The stream practice seeds are drawn from — fixed, so a rebake is a rebake.</summary>
        private const ulong PracticeSeedStream = 0x50555454_53454544UL;

        /// <summary>First day baked. Nothing before this has ever been playable.</summary>
        private static readonly DateTime DailyFrom = new DateTime(2026, 8, 1);

        /// <summary>Days baked from there: a little over two years.</summary>
        private const int DailyDays = 800;

        [MenuItem("PuttSeed/Bake Courses")]
        public static void BakeAll()
        {
            var feel = AssetDatabase.LoadAssetAtPath<FeelConfig>("Assets/PuttSeed/Resources/FeelConfig.asset");
            if (feel == null)
            {
                Debug.LogError("PuttSeed: no FeelConfig asset — cannot bake against the game's own physics.");
                return;
            }

            var baseConfig = feel.BuildSimConfig();
            Directory.CreateDirectory(OutputDirectory);

            Bake("tutorial", TutorialSeeds(), baseConfig);
            Bake("journey", JourneySeeds(), baseConfig);
            Bake("practice", PracticeSeeds(), baseConfig);
            Bake("daily", DailySeeds(), baseConfig);

            AssetDatabase.Refresh();
        }

        private static List<ulong> TutorialSeeds()
        {
            var seeds = new List<ulong>();
            foreach (var stage in TutorialConfig.Stages)
            {
                seeds.Add(stage.Seed);
            }

            return seeds;
        }

        private static List<ulong> JourneySeeds() => new List<ulong>(JourneyConfig.Seeds);

        private static List<ulong> PracticeSeeds()
        {
            // The same draw every time: a pack that changed on every bake would
            // hand returning players a different "endless" pool for no reason.
            var seeds = new List<ulong>(PracticePoolSize);
            ulong state = PracticeSeedStream;
            for (int i = 0; i < PracticePoolSize; i++)
            {
                seeds.Add(FixRng.SplitMix64(ref state));
            }

            return seeds;
        }

        private static List<ulong> DailySeeds()
        {
            var seeds = new List<ulong>(DailyDays);
            for (int i = 0; i < DailyDays; i++)
            {
                var date = DailyFrom.AddDays(i);
                seeds.Add(DailySeed.FromUtcDate(date.Year, date.Month, date.Day));
            }

            return seeds;
        }

        /// <summary>
        /// Grows every seed in the list and writes the pack. Generation is pure
        /// core with no shared state, so it parallelises across cores for free;
        /// the results are sorted by seed before writing, because a pack whose
        /// bytes depend on thread scheduling is a pack that shows up as a diff
        /// every time it is rebuilt.
        /// </summary>
        private static void Bake(string name, List<ulong> seeds, SimConfig baseConfig)
        {
            const int version = 4;
            var genConfig = GeneratorConfig.ForVersion(version);
            var solverConfig = SolverConfig.ForVersion(version);
            var results = new GenerationResult?[seeds.Count];
            int done = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();

            Parallel.For(0, seeds.Count, i =>
            {
                ulong seed = seeds[i];
                var config = DailyMutators.Apply(baseConfig, seed, version);
                try
                {
                    results[i] = CourseGenerator.Generate(seed, genConfig, config, solverConfig);
                }
                catch (InvalidOperationException)
                {
                    results[i] = null; // a seed that will not grow is skipped
                }

                System.Threading.Interlocked.Increment(ref done);
            });

            var entries = new List<CourseBake.Entry>(seeds.Count);
            int easy = 0, normal = 0, hard = 0, par3 = 0;
            for (int i = 0; i < seeds.Count; i++)
            {
                var result = results[i];
                if (result == null)
                {
                    continue;
                }

                entries.Add(new CourseBake.Entry(seeds[i], result));
                easy += result.Difficulty == Difficulty.Easy ? 1 : 0;
                normal += result.Difficulty == Difficulty.Normal ? 1 : 0;
                hard += result.Difficulty == Difficulty.Hard ? 1 : 0;
                par3 += result.Course.Par == 3 ? 1 : 0;
            }

            entries.Sort((a, b) => a.Seed.CompareTo(b.Seed));
            var bytes = CourseBake.Write(entries, version);
            string path = $"{OutputDirectory}/{name}.bytes";
            File.WriteAllBytes(path, bytes);

            clock.Stop();
            Debug.Log($"PuttSeed baked {name}: {entries.Count}/{seeds.Count} courses, "
                + $"{bytes.Length / 1024} KB, {clock.Elapsed.TotalSeconds:F0}s "
                + $"(easy {easy}, normal {normal}, hard {hard}, par3 {par3}) -> {path}");
        }
    }
}
