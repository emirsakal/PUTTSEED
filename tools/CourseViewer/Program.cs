// PUTTSEED CourseViewer: ASCII debug render of a generated course.
//   dotnet run --project tools/CourseViewer -- <seed|yyyy-mm-dd> [--stats]
// Lives outside core/, so floating point is allowed here (render math only).
using System;
using System.Diagnostics;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.Daily;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Replay;
using PuttSeed.Core.Sim;

if (args.Length < 1)
{
    Console.WriteLine("usage: CourseViewer <seed|yyyy-mm-dd> [--stats] [--v1]");
    Console.WriteLine("       CourseViewer --scan <count> [--v1|--v4] [--feel]   (CSV of seed stats, for curation)");
    Console.WriteLine("seeds default to the v2 generator; dates pick their own version by schedule.");
    return 1;
}

bool wantV1 = Array.Exists(args, a => a == "--v1");
bool wantFeel = Array.Exists(args, a => a == "--feel");

// The physics the GAME generates under. Acceptance depends on solvability and
// solvability depends on friction, so a seed scanned under core's defaults can
// grow a DIFFERENT course in the app — which is exactly how a tutorial lesson
// curated as "ice and water" turned up carrying three bumpers.
// Mirrors Assets/PuttSeed/Resources/FeelConfig.asset through the same 1/10000
// quantization FeelConfig.BuildSimConfig uses; the tutorial tests, which load
// the real asset, are what keep the two honest.
static Fix64 Q(int tenThousandths) => Fix64.FromFraction(tenThousandths, 10000);
SimConfig PlayConfig() => SimConfig.Create(
    dt: Fix64.FromFraction(1, 120),
    ballRadius: Fix64.FromFraction(1, 10),
    maxShotSpeed: Q(80000),
    rollDamping: Q(9880),
    sandDamping: Q(9400),
    iceDamping: Q(9985),
    wallRestitution: Q(8000),
    maxTravelPerSubStep: Fix64.FromFraction(1, 20),
    bumperRestitution: Q(12000),
    bumperMaxExitSpeed: Q(80000),
    holeRadius: Q(1500),
    holeCaptureSpeedSq: Q(15000) * Q(15000),
    rimRestitution: Q(4000),
    restSpeedEpsSq: Q(200) * Q(200),
    restTicksRequired: 6);
SimConfig PickSim() => wantFeel ? PlayConfig() : SimConfig.Default;
bool wantV4 = Array.Exists(args, a => a == "--v4");
GeneratorConfig PickConfig() => wantV1 ? GeneratorConfig.V1 : wantV4 ? GeneratorConfig.V4 : GeneratorConfig.V2;
SolverConfig PickSolver() => wantV4 ? SolverConfig.V4 : SolverConfig.Default;

// Curation support: sweep seeds 1..N and emit one CSV row per generatable
// course — the Journey level list is picked from this output.
if (args[0] == "--scan")
{
    int count = args.Length > 1 && int.TryParse(args[1], out int n) ? n : 1000;
    var scanCfg = PickConfig();
    var scanSolver = PickSolver();
    Console.WriteLine("seed,par,difficulty,walls,bumpers,sand,ice,water,gates,ramps,portals,mills,hazards,authorStrokes,attempts,score");
    for (ulong s = 1; s <= (ulong)count; s++)
    {
        GenerationResult r;
        try
        {
            r = CourseGenerator.Generate(s, scanCfg, PickSim(), scanSolver);
        }
        catch (InvalidOperationException)
        {
            continue;
        }

        var c = r.Course;
        int hazards = c.Bumpers.Length + c.SandZones.Length + c.IceZones.Length + c.WaterZones.Length
            + c.Gates.Length + c.Ramps.Length + c.Portals.Length / 2 + c.Windmills.Length;
        Console.WriteLine($"{s},{c.Par},{r.Difficulty},{c.Walls.Length},{c.Bumpers.Length}," +
            $"{c.SandZones.Length},{c.IceZones.Length},{c.WaterZones.Length}," +
            $"{c.Gates.Length},{c.Ramps.Length},{c.Portals.Length / 2},{c.Windmills.Length}," +
            $"{hazards},{r.AuthorStrokes},{r.Attempts},{r.DifficultyScore}");
    }

    return 0;
}

// Where the generator's time actually goes. Par variety needs a deeper
// solver search, the search is bounded in SIM TICKS, so the tick rate is the
// exchange rate between search depth and the wait a player sees.
if (args[0] == "--bench")
{
    int courses = args.Length > 1 && int.TryParse(args[1], out int bn) ? bn : 60;
    var benchCfg = PickConfig();
    var benchSolver = PickSolver();

    // 1. Raw tick throughput on a real course, full-power shots to rest.
    var warm = CourseGenerator.Generate(3UL, benchCfg, SimConfig.Default, benchSolver);
    var benchSim = new GolfSim(warm.Course, SimConfig.Default);
    long ticks = 0;
    var tickWatch = Stopwatch.StartNew();
    for (int shot = 0; tickWatch.ElapsedMilliseconds < 2000; shot++)
    {
        benchSim.RestoreRest(warm.Course.StartPosition, 0);
        benchSim.Shoot(new ShotInput((shot * 37) & (FixTrig.AngleSteps - 1), 200 + (shot % 55)));
        for (int t = 0; t < 700 && !benchSim.IsAtRest && !benchSim.IsHoled; t++)
        {
            benchSim.Tick();
            ticks++;
        }
    }

    tickWatch.Stop();
    double ticksPerSecond = ticks / tickWatch.Elapsed.TotalSeconds;

    // 2. End-to-end generation, which is mostly solving.
    var genWatch = Stopwatch.StartNew();
    long attempts = 0;
    int made = 0;
    for (ulong s = 1; made < courses; s++)
    {
        try
        {
            var r = CourseGenerator.Generate(s, benchCfg, SimConfig.Default, benchSolver);
            attempts += r.Attempts;
            made++;
        }
        catch (InvalidOperationException)
        {
        }
    }

    genWatch.Stop();
    Console.WriteLine($"sim throughput   {ticksPerSecond / 1_000_000.0:F2} M ticks/s  ({ticks:N0} ticks measured)");
    Console.WriteLine($"generation       {genWatch.Elapsed.TotalMilliseconds / made:F1} ms/course over {made} courses");
    Console.WriteLine($"attempts         {attempts / (double)made:F2} avg per accepted course");
    Console.WriteLine($"solver budget    {benchSolver.MaxTotalSimTicks:N0} ticks"
        + $" = {benchSolver.MaxTotalSimTicks / ticksPerSecond * 1000.0:F0} ms of ticking at this rate");
    return 0;
}

ulong seed;
GeneratorConfig cfg;
if (args[0].Contains('-') && DateTime.TryParse(args[0], out var date))
{
    seed = DailySeed.FromUtcDate(date.Year, date.Month, date.Day);
    // Dates carry their own generator version, mirroring the game exactly.
    int dayNumber = (int)(date.Date - new DateTime(2020, 1, 1)).TotalDays;
    int version = GeneratorSchedule.VersionForDay(dayNumber);
    cfg = GeneratorConfig.ForVersion(version);
    wantV4 = version >= 4;
    Console.WriteLine($"date {date:yyyy-MM-dd} -> seed {seed} (generator v{version})");
}
else if (ulong.TryParse(args[0], out seed))
{
    cfg = PickConfig();
}
else
{
    Console.WriteLine($"not a seed or date: {args[0]}");
    return 1;
}

var sw = Stopwatch.StartNew();
GenerationResult result;
try
{
    result = CourseGenerator.Generate(seed, cfg, PickSim(), PickSolver());
}
catch (InvalidOperationException e)
{
    Console.WriteLine($"GENERATION FAILED: {e.Message}");
    return 2;
}

sw.Stop();

var course = result.Course;
Render(course);

Console.WriteLine();
Console.WriteLine($"seed        {seed}");
Console.WriteLine($"par         {course.Par}   difficulty {result.Difficulty}");
Console.WriteLine($"walls       {course.Walls.Length}   bumpers {course.Bumpers.Length}   sand {course.SandZones.Length}   water {course.WaterZones.Length}   ice {course.IceZones.Length}");
if (course.Gates.Length + course.Ramps.Length + course.Portals.Length + course.Windmills.Length > 0)
{
    Console.WriteLine($"gates       {course.Gates.Length}   ramps {course.Ramps.Length}   portals {course.Portals.Length / 2}   mills {course.Windmills.Length}");
}
Console.WriteLine($"author solution ({result.AuthorSolution.Length} shots, {result.AuthorStrokes} strokes):");
for (int i = 0; i < result.AuthorSolution.Length; i++)
{
    var s = result.AuthorSolution[i];
    double degrees = s.AngleIndex * 360.0 / 1024.0;
    double power = (s.PowerIndex + 1) / 256.0;
    Console.WriteLine($"  {i + 1}. angle {s.AngleIndex,4} ({degrees,5:F1} deg)   power {s.PowerIndex,3} ({power:P0})");
}

Console.WriteLine($"replay code {ReplayCodec.Encode(seed, result.AuthorSolution)}");
if (Array.Exists(args, a => a == "--stats"))
{
    Console.WriteLine($"generated in {sw.Elapsed.TotalMilliseconds:F0} ms, attempts {result.Attempts}, relaxation level {result.RelaxationLevel}");
}

return 0;

static void Render(CourseData course)
{
    const double cell = 0.25;
    double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
    foreach (var wall in course.Walls)
    {
        foreach (var p in new[] { wall.A, wall.B })
        {
            var (x, y) = (ToDouble(p.X), ToDouble(p.Y));
            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
        }
    }

    minX -= 0.5; minY -= 0.5; maxX += 0.5; maxY += 0.5;
    int cols = (int)Math.Ceiling((maxX - minX) / cell);
    int rows = (int)Math.Ceiling((maxY - minY) / cell);
    var grid = new char[rows, cols];
    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            grid[r, c] = ' ';
        }
    }

    // Zones first (walls draw over them).
    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            var probe = new Vec2Fix(FromDouble(minX + (c + 0.5) * cell), FromDouble(minY + (r + 0.5) * cell));
            foreach (var zone in course.IceZones)
            {
                if (zone.Contains(probe)) { grid[r, c] = '*'; }
            }

            foreach (var ramp in course.Ramps)
            {
                if (ramp.Area.Contains(probe)) { grid[r, c] = '%'; }
            }

            foreach (var zone in course.SandZones)
            {
                if (zone.Contains(probe)) { grid[r, c] = ':'; }
            }

            foreach (var zone in course.WaterZones)
            {
                if (zone.Contains(probe)) { grid[r, c] = '~'; }
            }
        }
    }

    // Bumpers.
    for (int r = 0; r < rows; r++)
    {
        for (int c = 0; c < cols; c++)
        {
            double px = minX + (c + 0.5) * cell, py = minY + (r + 0.5) * cell;
            foreach (var b in course.Bumpers)
            {
                double dx = px - ToDouble(b.Center.X), dy = py - ToDouble(b.Center.Y);
                if (dx * dx + dy * dy <= ToDouble(b.Radius) * ToDouble(b.Radius)) { grid[r, c] = 'o'; }
            }
        }
    }

    // Walls: rasterize by stepping along each segment.
    foreach (var wall in course.Walls)
    {
        RasterizeSegment(grid, wall.A, wall.B, '#', minX, minY, cell);
    }

    // One-way gates: '=' across the corridor (direction in the stats only).
    foreach (var gate in course.Gates)
    {
        RasterizeSegment(grid, gate.A, gate.B, '=', minX, minY, cell);
    }

    // Windmills: blades at their shot-start phase, 'X' on the pivot.
    foreach (var mill in course.Windmills)
    {
        int spacing = FixTrig.AngleSteps / mill.BladeCount;
        for (int b = 0; b < mill.BladeCount; b++)
        {
            int angle = ((mill.Phase0 + b * spacing) % FixTrig.AngleSteps + FixTrig.AngleSteps)
                % FixTrig.AngleSteps;
            var tip = mill.Pivot + FixTrig.UnitVector(angle) * mill.BladeLength;
            RasterizeSegment(grid, mill.Pivot, tip, 'x', minX, minY, cell);
        }

        Plot(grid, mill.Pivot, 'X', minX, minY, cell);
    }

    // Portals: '@' discs on both mouths.
    foreach (var portal in course.Portals)
    {
        Plot(grid, portal.Entry, '@', minX, minY, cell);
    }

    Plot(grid, course.StartPosition, 'S', minX, minY, cell);
    Plot(grid, course.HolePosition, 'H', minX, minY, cell);

    // Y grows upward in sim space: print top row last-to-first.
    for (int r = rows - 1; r >= 0; r--)
    {
        var line = new char[cols];
        for (int c = 0; c < cols; c++)
        {
            line[c] = grid[r, c];
        }

        Console.WriteLine(new string(line));
    }
}

static void RasterizeSegment(char[,] grid, Vec2Fix a, Vec2Fix b, char ch,
    double minX, double minY, double cell)
{
    double ax = ToDouble(a.X), ay = ToDouble(a.Y);
    double bx = ToDouble(b.X), by = ToDouble(b.Y);
    double len = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
    int steps = Math.Max(1, (int)(len / (cell * 0.5)));
    int rows = grid.GetLength(0), cols = grid.GetLength(1);
    for (int s = 0; s <= steps; s++)
    {
        double t = (double)s / steps;
        int c = (int)((ax + (bx - ax) * t - minX) / cell);
        int r = (int)((ay + (by - ay) * t - minY) / cell);
        if (r >= 0 && r < rows && c >= 0 && c < cols) { grid[r, c] = ch; }
    }
}

static void Plot(char[,] grid, Vec2Fix p, char ch, double minX, double minY, double cell)
{
    int c = (int)((ToDouble(p.X) - minX) / cell);
    int r = (int)((ToDouble(p.Y) - minY) / cell);
    if (r >= 0 && r < grid.GetLength(0) && c >= 0 && c < grid.GetLength(1))
    {
        grid[r, c] = ch;
    }
}

static double ToDouble(Fix64 v) => v.Raw / 4294967296.0;

static Fix64 FromDouble(double v) => Fix64.FromRaw((long)Math.Round(v * 4294967296.0));
