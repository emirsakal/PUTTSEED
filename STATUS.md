# PUTTSEED — Status (end of Week 1)

Date: 2026-08-15. Scope executed: ROADMAP Week 1 (deterministic core) only.
Unity, generation and replay were intentionally not touched.

## What exists

```
core/PuttSeed.sln
core/src/PuttSeed.Core/            netstandard2.1, C# 9, nullable, warnings-as-errors
  FixedMath/Fix64.cs               Q32.32 on long; mul via 128-bit hi/lo split;
                                   shift-subtract div (round-to-nearest, saturating);
                                   Newton-iteration sqrt; Abs/Sign/Min/Max/Clamp
  FixedMath/Vec2Fix.cs             value struct; ops, dot, lengthSq/length, perp
  FixedMath/FixRng.cs              xorshift128, state seeded via SplitMix64; the only
                                   randomness source in core
  FixedMath/FixTrig.cs             sin/cos by 1024-entry table index; UnitVector
  FixedMath/FixTrigTable.cs        committed generated table (long[] constants)
  Sim/GolfSim.cs                   fixed 1/120 s tick; damping -> sub-stepped move ->
                                   bumper -> wall -> water -> hole; rest detection;
                                   FNV-1a StateHash over raw state fields
  Sim/SimConfig.cs                 all tuning constants (fixed point), Default
  Sim/CourseData.cs                start, hole, par, walls, bumpers, sand, water
  Sim/{BallState,ShotInput,WallSegment,Bumper,ZonePolygon}.cs
core/tests/PuttSeed.Core.Tests/    NUnit, net8.0 — 92 tests, all green
scripts/test.bat                   purity grep + dotnet test core
scripts/check-purity.bat           fails on float/double/System.Random/DateTime/
                                   UnityEngine anywhere in core/src
```

## Test counts (92 total, `dotnet test core` green, zero warnings)

| Area | Tests |
|---|---|
| Fix64 (raw constants, 128-bit mul, div rounding, sqrt, overflow edges) | 17 |
| Vec2Fix | 9 |
| FixRng (golden sequences from offline reference, ranges) | 9 |
| FixTrig (cardinal exactness, ±1 ulp vs reference sine, symmetry, sin²+cos²) | 7 |
| GolfSim integration + friction | 8 |
| Wall collision + sub-stepping (incl. full-power no-tunneling, corner) | 7 |
| Rest detection + StateHash | 9 |
| Bumper (boost, cap, no-penetration, deflection) | 5 |
| ZonePolygon (even-odd, concave, vertex-level rays) | 4 |
| Sand | 3 |
| Water (penalty, last-rest reset, no skip-over) | 5 |
| Hole capture (capture, rim-out, terminal state) | 6 |
| Determinism (10k ticks × 2 in-process + committed golden hash) | 2 |

Golden 10k-tick hash: `531089411828813883` (fixture course exercising walls,
bumpers, sand, water; 8-shot script; see `DeterminismTests.cs` for the
regeneration procedure).

## Deviations from ARCHITECTURE.md (with reasons)

1. **Sin/cos table generator is not committed as a repo tool.** The table was
   generated offline (scratch console tool using double, outside `core/`) and
   committed as constants per the doc. The doc's "test-verified tool" intent is
   covered differently: `FixTrigTests.Table_MatchesDoubleSine_WithinOneUlp`
   re-derives every entry from double sine in the test project, so the committed
   table is pinned bit-for-bit and trivially regenerable from the formula in
   that test. The repo layout in CLAUDE.md has no `tools/` directory; say the
   word and I'll add one with the generator project.
2. **Fix64 div saturates on overflow instead of wrapping** (mul wraps,
   documented in XML docs). Any fixed choice is determinism-safe; saturation is
   the safer failure mode for physics.
3. **"Overlap" for hole capture = ball center inside cup radius** (not
   circle-overlap). Standard mini-golf reading; keeps capture strictly harder
   than grazing.
4. **Rim-out is a reduced-restitution reflection** off the cup edge
   (restitution 0.4). The doc says "rim-out impulse" without prescribing form;
   this is the simplest deterministic one. Tunable in `SimConfig`.
5. **Water triggers on center-in-polygon checked every sub-step**, which covers
   both "at rest in water" and "while crossing" from the doc with one rule.

Everything else follows the doc directly: semi-implicit Euler, exponential
damping (stronger in sand), circle–segment walls with sub-stepping instead of
swept collision, xorshift RNG, FNV-1a state hash.

## Environment note

.NET SDK 8.0.424 was installed via winget this session (none was present).

## Next (Week 2, not started)

Corridor generator, SolvabilityChecker, DifficultyRater, ReplayCodec,
DailySeed, 1000-seed property suite, ASCII console harness.
