using System;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>A generated, machine-verified course with its metadata.</summary>
    public sealed class GenerationResult
    {
        /// <summary>The playable course (par included).</summary>
        public CourseData Course { get; }

        /// <summary>The solver's shot sequence proving solvability (fallback ghost).</summary>
        public ShotInput[] AuthorSolution { get; }

        /// <summary>
        /// The mill clock every author shot is taken at — always zero, because
        /// the solver expands each rest state with
        /// <see cref="Sim.GolfSim.RestoreRest"/>, which re-arms the clock. On a
        /// windmill course that timing is part of the solution: a replay must
        /// wait for the blades to come round to phase zero before each shot,
        /// or it meets a different blade angle and misses the cup.
        /// </summary>
        public int[] AuthorShotClocks { get; }

        /// <summary>Strokes of the author solution, including penalties.</summary>
        public int AuthorStrokes { get; }

        /// <summary>Rated difficulty bucket.</summary>
        public Difficulty Difficulty { get; }

        /// <summary>Total generation attempts spent (across relaxation levels).</summary>
        public int Attempts { get; }

        /// <summary>Decoration relaxation level used (0 = full decoration).</summary>
        public int RelaxationLevel { get; }

        /// <summary>The raw difficulty score behind <see cref="Difficulty"/>.</summary>
        public int DifficultyScore { get; }

        /// <summary>Creates a result.</summary>
        public GenerationResult(CourseData course, ShotInput[] authorSolution, int authorStrokes,
            Difficulty difficulty, int attempts, int relaxationLevel, int difficultyScore = 0)
        {
            Course = course;
            AuthorSolution = authorSolution;
            AuthorShotClocks = new int[authorSolution.Length];
            AuthorStrokes = authorStrokes;
            Difficulty = difficulty;
            Attempts = attempts;
            RelaxationLevel = relaxationLevel;
            DifficultyScore = difficultyScore;
        }
    }

    /// <summary>
    /// The full generation pipeline (ARCHITECTURE.md): corridor growth →
    /// decoration → solvability proof. Rejected candidates re-roll with the
    /// next SplitMix64 sub-seed; after each block of attempts the decoration
    /// budget is relaxed (bumpers/sand/water reduced, eventually none). The
    /// solvability requirement is never loosened. Par is the author solution's
    /// stroke count clamped to the GDD range 2..MaxPar.
    /// </summary>
    public static class CourseGenerator
    {
        private const int RelaxationLevels = 4;

        /// <summary>
        /// Generates the course for a seed. Deterministic; bounded by
        /// <c>RelaxationLevels * cfg.AttemptsPerLevel</c> attempts.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// No attempt produced a solvable course — statistically negligible with
        /// default configs (the final level is a bare corridor); the property
        /// suite guards this over 1000 seeds.
        /// </exception>
        public static GenerationResult Generate(
            ulong seed, GeneratorConfig cfg, SimConfig simConfig, SolverConfig solverConfig)
        {
            ulong subSeedState = seed;
            int attempts = 0;

            for (int level = 0; level < RelaxationLevels; level++)
            {
                int maxBumpers = Math.Max(0, cfg.MaxBumpers - level);
                int maxSand = Math.Max(0, cfg.MaxSand - level);
                int maxWater = Math.Max(0, cfg.MaxWater - level);
                int maxIce = Math.Max(0, cfg.MaxIce - level);
                int maxGates = Math.Max(0, cfg.MaxGates - level);
                int maxRamps = Math.Max(0, cfg.MaxRamps - level);
                int maxPortals = Math.Max(0, cfg.MaxPortals - level);
                int maxWindmills = Math.Max(0, cfg.MaxWindmills - level);

                for (int a = 0; a < cfg.AttemptsPerLevel; a++)
                {
                    attempts++;
                    var rng = new FixRng(FixRng.SplitMix64(ref subSeedState));

                    if (!CorridorBuilder.TryBuild(rng, cfg, out var corridor))
                    {
                        continue;
                    }

                    // Cheap pre-check before burning the solver's tick budget: a
                    // corridor longer than the depth cap can plausibly cover
                    // (~4 units of winding progress per shot) cannot be solved.
                    if (!IsPlausiblyReachable(corridor, cfg))
                    {
                        continue;
                    }

                    CourseDecorator.Decorate(rng, corridor, cfg,
                        maxBumpers, maxSand, maxWater, maxIce,
                        maxGates, maxRamps, maxPortals, maxWindmills,
                        out var bumpers, out var sand, out var water, out var ice,
                        out var gates, out var ramps, out var portals, out var windmills);

                    var walls = CorridorBuilder.BuildWalls(corridor);
                    var start = CorridorBuilder.StartPosition(corridor);
                    var hole = CorridorBuilder.HolePosition(corridor);

                    // Par does not influence physics; solve with the cap, then
                    // stamp the real par from the author solution.
                    var candidate = new CourseData(start, hole, solverConfig.MaxPar,
                        walls, bumpers, sand, water, ice, gates, ramps, portals, windmills);
                    var solve = SolvabilityChecker.Solve(candidate, simConfig, solverConfig);
                    if (!solve.Solved)
                    {
                        continue;
                    }

                    int par = Math.Min(Math.Max(solve.AuthorStrokes, 2), solverConfig.MaxPar);
                    var course = new CourseData(start, hole, par, walls,
                        bumpers, sand, water, ice, gates, ramps, portals, windmills);

                    int turns = corridor.SegmentAngles.Length - 1;

                    // A portal pair is one hazard: two array entries, one idea.
                    int hazards = bumpers.Length + sand.Length + water.Length + ice.Length
                        + gates.Length + ramps.Length + portals.Length / 2 + windmills.Length;
                    var difficulty = DifficultyRater.Rate(
                        solve.CaptureShotCount, solve.SampledShotCount, turns, hazards, par);
                    int score = DifficultyRater.Score(
                        solve.CaptureShotCount, solve.SampledShotCount, turns, hazards);

                    return new GenerationResult(course, solve.AuthorSolution, solve.AuthorStrokes,
                        difficulty, attempts, level, score);
                }
            }

            throw new InvalidOperationException(
                $"Course generation failed after {attempts} attempts for seed {seed}.");
        }

        private static bool IsPlausiblyReachable(Corridor corridor, GeneratorConfig cfg)
        {
            var total = Fix64.Zero;
            var c = corridor.Centerline;
            for (int i = 1; i < c.Length; i++)
            {
                total += (c[i] - c[i - 1]).Length();
            }

            // ~4 units of winding progress per shot: the cap is what decides
            // how many strokes a hole can be worth, so it belongs to the
            // generator version rather than to the solver's budget.
            return total <= cfg.MaxCorridorLength;
        }
    }
}
