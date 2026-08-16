# PUTTSEED — Status (end of Week 2)

Date: 2026-08-16. Scope executed so far: ROADMAP Weeks 1–2. Unity untouched.

## What exists

```
core/PuttSeed.sln                  (core lib + tests + tools/CourseViewer)
core/src/PuttSeed.Core/            netstandard2.1, C# 9, nullable, warnings-as-errors
  FixedMath/                       Fix64 (Q32.32), Vec2Fix, FixRng (xorshift128),
                                   FixTrig + committed 1024-entry sine table
  Sim/                             GolfSim (120 Hz fixed tick, sub-stepping, walls,
                                   bumpers, sand, water, hole, rest detection,
                                   FNV-1a StateHash, RestoreRest solver API),
                                   SimConfig, CourseData + element types
  CourseGen/                       CorridorBuilder (growth + widening + caps),
                                   CourseDecorator (clearance rules), GeomFix,
                                   SolvabilityChecker (bounded BFS + author
                                   solution + tightness), DifficultyRater,
                                   CourseGenerator (attempts, sub-seeds,
                                   decoration relaxation), GeneratorConfig,
                                   SolverConfig
  Replay/ReplayCodec.cs            PUTT- base64url codes, version byte
  Daily/DailySeed.cs               FNV-1a(salt + yyyyMMdd) -> SplitMix64
core/tests/PuttSeed.Core.Tests/    NUnit — 149 tests, all green
tools/CourseViewer/                console ASCII renderer (seed or date input)
scripts/test.bat                   purity grep + dotnet test core -c Release
scripts/check-purity.bat           float/double/System.Random/DateTime/UnityEngine grep
```

## Test counts (149 total; `scripts\test.bat` ≈ 1.5 min, Release)

| Area | Tests |
|---|---|
| Week 1: FixedMath + Sim + determinism golden hash | 92 |
| ReplayCodec (round-trip incl. 1000 random cases, tamper, golden string) | 10 |
| DailySeed (distinctness, golden cross-device values) | 5 |
| GolfSim.RestoreRest (bit-exact vs naturally reached states) | 4 |
| CorridorBuilder (bounds, self-avoidance, enclosure by simulation) | 9 |
| CourseDecorator (counts, passage clearance, start/hole clearance) | 7 |
| SolvabilityChecker (solvable/sealed/L fixtures, replay, determinism) | 6 |
| DifficultyRater (buckets, monotonicity) | 6 |
| CourseGenerator (solvable + replayable per seed, determinism, bounds) | 6 |
| Property suite: 1000 seeds in parallel (bounded generation, author
  solution captures within par, codec round-trips) | 1 |
| Golden replay fixtures (3 seeds: frozen final hash + code) | 3 |

Golden values: 10k-tick sim hash `531089411828813883`; replay fixtures in
`GoldenReplayTests.cs`; daily seeds in `DailySeedTests.cs`.

## CourseViewer

`dotnet run --project tools/CourseViewer -c Release -- <seed|yyyy-mm-dd> [--stats]`
prints an ASCII map (`#` walls, `o` bumper, `:` sand, `~` water, `S`/`H`),
the author solution shot list and the shareable replay code. Generation
averages ~0.9 s (Release), worst observed ~2 s.

## Deviations / interpretation choices (Week 2)

1. **Solver is BFS with a twist.** ARCHITECTURE.md prescribes breadth-first
   over shot sequences with a coarse grid; implemented exactly that, plus two
   bounds it doesn't mention: a total sim-tick budget (hard cost ceiling per
   solve) and nearest-hole-first ordering *within* a depth level. Ordering
   affects only how fast a solution is found inside the budget, never which
   sequences are reachable. Budget cuts may reject a solvable course; that
   only costs a re-roll (solvability is never loosened).
2. **Reachability pre-check.** Corridors whose centerline exceeds ~12 units
   are rejected before solving (a shot advances ~4 units; budget explores ~3
   levels). Cheap, conservative, deterministic.
3. **Par is derived, not targeted:** author-solution strokes clamped to 2..5.
   With current tuning most courses land at par 2–3 — flag for the Week 3
   feel pass if deeper courses are wanted (raise budget/depth, accept slower
   generation).
4. **Segment lengths tuned to 1.25–2.5** (architecture gives counts and turn
   constraints but no lengths) so 4–8 segments stay solvable within the
   depth cap.
5. **`scripts\test.bat` runs Release** so the property suite finishes in
   ~1.5 min; plain `dotnet test core` (Debug) stays green, just slower.
6. **Golden replay fixtures are committed constants**, not separate fixture
   files — same guarantee, less plumbing.
7. Week 1 deviations still apply (sqrt/rim-out/hole-overlap choices, offline
   table generator not committed as a tool — `tools/` now exists, say the
   word and I'll move a generator project in).

## Environment note

.NET SDK 8.0.424 installed via winget in the Week-1 session.

## Next (Week 3, not started)

Unity 6 project, SimRunner + interpolation, drag input quantization,
course rendering, ghost playback, minimal UI, on-device feel pass.
