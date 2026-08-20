using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// Bounds and sampling grid of the <see cref="SolvabilityChecker"/> BFS.
    /// Every knob is a hard bound, so solving always terminates.
    /// </summary>
    public sealed class SolverConfig
    {
        /// <summary>Angle sampling stride (1024 / stride angles tried per state).</summary>
        public int AngleStride { get; }

        /// <summary>Power sampling stride (256 / stride powers tried per state).</summary>
        public int PowerStride { get; }

        /// <summary>Max ticks a sampled shot may run before being discarded.</summary>
        public int TickCapPerShot { get; }

        /// <summary>Max BFS states expanded before giving up.</summary>
        public int MaxExpandedStates { get; }

        /// <summary>
        /// Total sim-tick budget for one solve. The dominant cost bound: an
        /// unsolvable course burns at most this many ticks before rejection.
        /// A conservative cut — a solvable course may be reported unsolvable
        /// when the budget runs out, which only costs a re-roll.
        /// </summary>
        public long MaxTotalSimTicks { get; }

        /// <summary>Max shots in a solution (BFS depth cap).</summary>
        public int MaxDepth { get; }

        /// <summary>Max strokes (shots + water penalties) a solution may cost.</summary>
        public int MaxPar { get; }

        /// <summary>Grid cell size for rest-state deduplication.</summary>
        public Fix64 DedupeCell { get; }

        private SolverConfig(
            int angleStride, int powerStride, int tickCapPerShot,
            int maxExpandedStates, long maxTotalSimTicks, int maxDepth, int maxPar, Fix64 dedupeCell)
        {
            AngleStride = angleStride;
            PowerStride = powerStride;
            TickCapPerShot = tickCapPerShot;
            MaxExpandedStates = maxExpandedStates;
            MaxTotalSimTicks = maxTotalSimTicks;
            MaxDepth = maxDepth;
            MaxPar = maxPar;
            DedupeCell = dedupeCell;
        }

        /// <summary>The tuned default configuration (32 angles x 8 powers).</summary>
        public static SolverConfig Default { get; } = new SolverConfig(
            angleStride: 32,
            powerStride: 32,
            tickCapPerShot: 700,
            maxExpandedStates: 16,
            maxTotalSimTicks: 400_000,
            maxDepth: 5,
            maxPar: 5,
            dedupeCell: Fix64.Half);

        /// <summary>
        /// The budget a par-3 hole needs. Finding a three-shot solution is not
        /// what costs: PROVING no two-shot solution exists is, and that means
        /// exhausting a whole level of the search. Cut this back and the solver
        /// gives up before the proof lands, the course is discarded as
        /// unsolvable, and the generator quietly hands back a par 2 instead —
        /// at 1.5M ticks a 150-seed scan produced 1% par 3 where 4M produces
        /// 36%. Par is capped at 3 deliberately: par 4 asks for a fourth level
        /// of search and a 32-unit corridor still only yielded 3% of them, at
        /// nine times the cost and one seed in five failing outright.
        /// </summary>
        public static SolverConfig V4 { get; } = new SolverConfig(
            angleStride: 32,
            powerStride: 32,
            tickCapPerShot: 700,
            maxExpandedStates: 80,
            maxTotalSimTicks: 4_000_000,
            maxDepth: 3,
            maxPar: 3,
            dedupeCell: Fix64.Half);

        /// <summary>The solver budget a generator version is proven under.</summary>
        public static SolverConfig ForVersion(int version) => version >= 4 ? V4 : Default;
    }
}
