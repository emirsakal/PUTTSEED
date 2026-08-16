using System.Collections.Generic;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>Outcome of a solvability search.</summary>
    public readonly struct SolveResult
    {
        /// <summary>True when a capture was found within the solver bounds.</summary>
        public bool Solved { get; }

        /// <summary>The found shot sequence (empty when unsolved).</summary>
        public ShotInput[] AuthorSolution { get; }

        /// <summary>Strokes at capture, including water penalties.</summary>
        public int AuthorStrokes { get; }

        /// <summary>Capturing shots found from the penultimate state (tightness numerator).</summary>
        public int CaptureShotCount { get; }

        /// <summary>Shots sampled per state (tightness denominator).</summary>
        public int SampledShotCount { get; }

        /// <summary>Creates a result.</summary>
        public SolveResult(bool solved, ShotInput[] authorSolution, int authorStrokes,
            int captureShotCount, int sampledShotCount)
        {
            Solved = solved;
            AuthorSolution = authorSolution;
            AuthorStrokes = authorStrokes;
            CaptureShotCount = captureShotCount;
            SampledShotCount = sampledShotCount;
        }
    }

    /// <summary>
    /// Proof-of-fairness search (ARCHITECTURE.md): breadth-first over shot
    /// sequences. From each rest state the quantized shot space is sampled on a
    /// coarse grid; each sampled shot is simulated with a tick cap; resulting
    /// rest states are deduplicated on a position grid. A course is accepted
    /// only if the hole is reached within the depth and stroke bounds. The found
    /// sequence is the author solution (fallback ghost, difficulty input).
    /// Angles are tried nearest-to-the-hole-direction first and powers gentle
    /// first, which finds captures early without changing what is reachable.
    /// </summary>
    public static class SolvabilityChecker
    {
        private readonly struct Node
        {
            public readonly Vec2Fix Position;
            public readonly int Strokes;
            public readonly int Depth;
            public readonly int Parent;
            public readonly ShotInput ShotFromParent;

            public Node(Vec2Fix position, int strokes, int depth, int parent, ShotInput shotFromParent)
            {
                Position = position;
                Strokes = strokes;
                Depth = depth;
                Parent = parent;
                ShotFromParent = shotFromParent;
            }
        }

        /// <summary>Runs the bounded BFS. Deterministic for identical inputs.</summary>
        public static SolveResult Solve(CourseData course, SimConfig simConfig, SolverConfig cfg)
        {
            int angleCount = FixTrig.AngleSteps / cfg.AngleStride;
            int powerCount = 256 / cfg.PowerStride;
            int sampledPerState = angleCount * powerCount;

            var sim = new GolfSim(course, simConfig);
            var nodes = new List<Node> { new Node(course.StartPosition, 0, 0, -1, default) };
            var visited = new HashSet<long> { CellKey(course.StartPosition, cfg.DedupeCell) };

            int expanded = 0;
            long tickBudget = cfg.MaxTotalSimTicks;
            var pending = new List<int> { 0 };
            while (pending.Count > 0 && expanded < cfg.MaxExpandedStates && tickBudget > 0)
            {
                // Expansion order: shallowest depth first (breadth-first over shot
                // counts), nearest the hole within a depth level, index as the
                // deterministic tie-break. Order only affects how fast a solution
                // is found within the budget, not which sequences are reachable.
                int bestPending = 0;
                for (int c = 1; c < pending.Count; c++)
                {
                    var cand = nodes[pending[c]];
                    var best = nodes[pending[bestPending]];
                    if (cand.Depth < best.Depth
                        || (cand.Depth == best.Depth
                            && (cand.Position - course.HolePosition).LengthSq()
                             < (best.Position - course.HolePosition).LengthSq()))
                    {
                        bestPending = c;
                    }
                }

                int i = pending[bestPending];
                pending.RemoveAt(bestPending);
                var node = nodes[i];
                if (node.Depth >= cfg.MaxDepth || node.Strokes >= cfg.MaxPar)
                {
                    continue;
                }

                expanded++;
                int aimIndex = BestSampledAngleTowardHole(course, node.Position, cfg.AngleStride);

                int captures = 0;
                ShotInput captureShot = default;
                int captureStrokes = 0;

                for (int k = 0; k < angleCount; k++)
                {
                    // 0, +1, -1, +2, -2, ... stride multiples around the aim angle.
                    int offset = (k + 1) / 2 * ((k & 1) == 1 ? 1 : -1);
                    int angle = (aimIndex + offset * cfg.AngleStride) & (FixTrig.AngleSteps - 1);

                    for (int p = cfg.PowerStride - 1; p < 256; p += cfg.PowerStride)
                    {
                        if (tickBudget <= 0)
                        {
                            break;
                        }

                        var shot = new ShotInput(angle, p);
                        sim.RestoreRest(node.Position, node.Strokes);
                        sim.Shoot(shot);
                        for (int t = 0; t < cfg.TickCapPerShot && !sim.IsAtRest && !sim.IsHoled; t++)
                        {
                            sim.Tick();
                            tickBudget--;
                        }

                        if (sim.IsHoled && sim.Strokes <= cfg.MaxPar)
                        {
                            if (captures == 0)
                            {
                                captureShot = shot;
                                captureStrokes = sim.Strokes;
                            }

                            captures++;
                            continue; // keep sweeping this node for the tightness stat
                        }

                        if (!sim.IsAtRest || sim.Strokes >= cfg.MaxPar || node.Depth + 1 >= cfg.MaxDepth)
                        {
                            continue; // discarded: still rolling at cap, or no shots left
                        }

                        var key = CellKey(sim.Ball.Position, cfg.DedupeCell);
                        if (visited.Add(key))
                        {
                            pending.Add(nodes.Count);
                            nodes.Add(new Node(sim.Ball.Position, sim.Strokes, node.Depth + 1, i, shot));
                        }
                    }
                }

                if (captures > 0)
                {
                    return new SolveResult(
                        solved: true,
                        authorSolution: ReconstructPath(nodes, i, captureShot),
                        authorStrokes: captureStrokes,
                        captureShotCount: captures,
                        sampledShotCount: sampledPerState);
                }
            }

            return new SolveResult(false, System.Array.Empty<ShotInput>(), 0, 0, sampledPerState);
        }

        /// <summary>Sampled angle whose unit vector points closest to the hole.</summary>
        private static int BestSampledAngleTowardHole(CourseData course, Vec2Fix from, int stride)
        {
            var toHole = course.HolePosition - from;
            int best = 0;
            var bestDot = Fix64.MinValue;
            for (int a = 0; a < FixTrig.AngleSteps; a += stride)
            {
                var dot = Vec2Fix.Dot(FixTrig.UnitVector(a), toHole);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = a;
                }
            }

            return best;
        }

        private static ShotInput[] ReconstructPath(List<Node> nodes, int nodeIndex, ShotInput finalShot)
        {
            int depth = nodes[nodeIndex].Depth;
            var path = new ShotInput[depth + 1];
            path[depth] = finalShot;
            int at = nodeIndex;
            for (int d = depth - 1; d >= 0; d--)
            {
                path[d] = nodes[at].ShotFromParent;
                at = nodes[at].Parent;
            }

            return path;
        }

        /// <summary>Quantizes a position to a dedupe grid cell key.</summary>
        private static long CellKey(Vec2Fix position, Fix64 cell)
        {
            long cx = FloorDiv(position.X.Raw, cell.Raw);
            long cy = FloorDiv(position.Y.Raw, cell.Raw);
            return (cx << 32) ^ (cy & 0xFFFFFFFFL);
        }

        private static long FloorDiv(long a, long b)
        {
            long q = a / b;
            if ((a % b != 0) && ((a < 0) != (b < 0)))
            {
                q--;
            }

            return q;
        }
    }
}
