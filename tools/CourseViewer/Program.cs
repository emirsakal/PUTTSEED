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
    Console.WriteLine("usage: CourseViewer <seed|yyyy-mm-dd> [--stats]");
    return 1;
}

ulong seed;
if (args[0].Contains('-') && DateTime.TryParse(args[0], out var date))
{
    seed = DailySeed.FromUtcDate(date.Year, date.Month, date.Day);
    Console.WriteLine($"date {date:yyyy-MM-dd} -> seed {seed}");
}
else if (!ulong.TryParse(args[0], out seed))
{
    Console.WriteLine($"not a seed or date: {args[0]}");
    return 1;
}

var sw = Stopwatch.StartNew();
GenerationResult result;
try
{
    result = CourseGenerator.Generate(seed, GeneratorConfig.Default, SimConfig.Default, SolverConfig.Default);
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
Console.WriteLine($"walls       {course.Walls.Length}   bumpers {course.Bumpers.Length}   sand {course.SandZones.Length}   water {course.WaterZones.Length}");
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
        double ax = ToDouble(wall.A.X), ay = ToDouble(wall.A.Y);
        double bx = ToDouble(wall.B.X), by = ToDouble(wall.B.Y);
        double len = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
        int steps = Math.Max(1, (int)(len / (cell * 0.5)));
        for (int s = 0; s <= steps; s++)
        {
            double t = (double)s / steps;
            int c = (int)((ax + (bx - ax) * t - minX) / cell);
            int r = (int)((ay + (by - ay) * t - minY) / cell);
            if (r >= 0 && r < rows && c >= 0 && c < cols) { grid[r, c] = '#'; }
        }
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
