#nullable enable
using System;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.Sim;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Finding a practice course: draw seeds, grow candidates, keep the one
    /// whose rated bucket the player asked for.
    ///
    /// It lives here rather than inside <see cref="ModeController"/> because
    /// two places need it. The game scene grows the NEXT course while the
    /// player is on the current one, and the menu grows the FIRST one while
    /// the player is still deciding — a search is up to eight generations, and
    /// a v4 generation is not free.
    /// </summary>
    public static class PracticeCourses
    {
        /// <summary>How many candidates a search will grow before settling.</summary>
        public const int CandidateTries = 8;

        /// <summary>
        /// Practice runs the newest generator — the fresh-course firehose is
        /// where new elements meet players first.
        /// </summary>
        public const int Version = 4;

        /// <summary>A found course and the physics it was proven under.</summary>
        public readonly struct Candidate
        {
            /// <summary>The seed that grew it.</summary>
            public readonly ulong Seed;

            /// <summary>The sim config it was solved under (the day's twist included).</summary>
            public readonly SimConfig Config;

            /// <summary>The course, or null when every candidate missed.</summary>
            public readonly GenerationResult? Result;

            /// <summary>Creates a candidate.</summary>
            public Candidate(ulong seed, SimConfig config, GenerationResult? result)
            {
                Seed = seed;
                Config = config;
                Result = result;
            }
        }

        /// <summary>
        /// Draws the seeds a search will try. Called on the MAIN thread so the
        /// search itself touches nothing but pure core.
        /// </summary>
        public static ulong[] DrawSeeds()
        {
            var entropy = new System.Random();
            var seeds = new ulong[CandidateTries];
            var buffer = new byte[8];
            for (int i = 0; i < seeds.Length; i++)
            {
                entropy.NextBytes(buffer);
                seeds[i] = BitConverter.ToUInt64(buffer, 0);
            }

            return seeds;
        }

        /// <summary>
        /// Grows candidates until one lands in the requested bucket, keeping
        /// the closest miss so an unlucky bucket never hands back an arbitrary
        /// course. Pure core work — safe on a thread pool thread.
        /// </summary>
        public static Candidate Search(ulong[] seeds, Difficulty want, SimConfig baseConfig)
        {
            var best = new Candidate(0, baseConfig, null);
            int bestDistance = int.MaxValue;
            foreach (ulong seed in seeds)
            {
                // Each seed carries its own themed twist, so a candidate is
                // solved under the physics it will be played under.
                var config = DailyMutators.Apply(baseConfig, seed, Version);
                GenerationResult candidate;
                try
                {
                    candidate = CourseGenerator.Generate(
                        seed, GeneratorConfig.ForVersion(Version), config,
                        SolverConfig.ForVersion(Version));
                }
                catch (InvalidOperationException)
                {
                    continue; // bounded generation missed; the next seed will do
                }

                int distance = Math.Abs((int)candidate.Difficulty - (int)want);
                if (distance < bestDistance)
                {
                    best = new Candidate(seed, config, candidate);
                    bestDistance = distance;
                }

                if (distance == 0)
                {
                    break;
                }
            }

            return best;
        }
    }
}
