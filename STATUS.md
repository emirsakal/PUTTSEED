# PUTTSEED — Status (end of Week 4)

Date: 2026-08-16. Scope executed: ROADMAP Weeks 1–4 (all).

## Week 4 — modes, polish, release

- **Modes** (`ModeController`): Daily (UTC seed, stats + streak recorded only
  while today's actual seed is loaded), Practice (device-entropy seeds
  filtered by difficulty bucket, ≤8 candidates, closest kept), Tutorial
  (fixed seeds 56/10/8 — scanned so each isolates its element — with one
  hint line each). FTUE: first launch opens Tutorial 1.
- **Stats/streak** (`StatsStore`): JSON at persistentDataPath, plain class,
  9 EditMode tests (streak increment/gap/reset, best-of-day, corrupt-file
  recovery). Attempts counted per run start/retry.
- **Feedback** (`FeedbackController`): audio hookup slots for the purchased
  pack (empty = silent), driven by new deterministic core counters
  (`WallHitCount`/`BumperHitCount`/`WaterEntryCount` — tested to stay OUT of
  StateHash; goldens unchanged), plus haptics, ball squash, hole-in ring.
- **Release packaging**: generated flat app icon + splash color in
  BuildTools; Android signing read from gitignored `keystore.properties`
  (loud warning when absent → debug key); `scripts/build-release.bat`
  produces `artifacts/PuttSeed-release.aab`.
- **README.md** (pitch, architecture, determinism proof, run matrix, ASCII
  render) and **LATER.md** (idea parking per the GDD non-goal rule).

Counts: core 160 (`scripts\test.bat`, Release, ~1.5 min) · Unity EditMode 27.

Week 4 deviations/notes:
- Practice generation runs on the main thread with coroutine yields between
  candidates (sub-second hitches possible on device); async version parked
  in LATER.md.
- Haptics use coarse `Handheld.Vibrate` (no amplitude API without plugins).
- The Play Console internal-testing upload itself is a human step: create
  the keystore per README, run build-release.bat, upload the .aab.

## Week 3 — Unity layer

The Unity project lives at the REPO ROOT (moved from unity/ by request,
2026-08-16 — open the repo root in Unity Hub).

```
Assets/ Packages/ ProjectSettings/  Unity 6000.3.22f1 (Android module), built-in pipeline
  Assets/PuttSeedCore/              PuttSeed.Core.asmdef (noEngineReferences: true)
    src -> junction to core/src/PuttSeed.Core (git-ignored; sources tracked once)
  Assets/PuttSeed/Runtime/          PuttSeed.Unity asmdef —
    SimRunner                       fixed 120 Hz stepping + ghost lockstep + snapshots
    FixedStepper                    accumulator logic as a plain testable class
    InputQuantizer                  drag -> ShotInput; THE input quantization boundary
    FixView                         Fix64 -> float, render-side only, one-way
    DragAimController               slingshot drag, aim line = power bar
    CourseRenderer/MeshFactory      flat-color runtime meshes (walls/zones/discs)
    BallView (trail), GhostViewManager, CameraFramer, GameUI, GameBootstrap
    FeelConfig                      ScriptableObject: ALL feel knobs; quantizes to
                                    Fix64 on a 1/10000 grid at the boundary
  Assets/PuttSeed/Editor/BuildTools scene creation + Android build entry points
  Assets/PuttSeed/Tests/EditMode/   19 tests: stepper, quantizer, FeelConfig
  Assets/Scenes/Main.unity          single scene: one Bootstrap GameObject
scripts/unity-tests.bat             batch EditMode run (artifacts/editmode-results.xml)
scripts/build-android.bat           batch build; default .aab, "apk" arg for APK
artifacts/PuttSeed.apk              35.5 MB IL2CPP ARM64+ARMv7 build — SUCCEEDED
```

Core additions this week (both TDD, 154 dotnet tests green):
- `SimConfig.Create(...)` public factory (FeelConfig's target).
- **Stroke limit rule in core** (game rules never live in Unity): `Shoot`
  refuses beyond par+3, `GolfSim.IsFailed` exposed. The determinism
  fixture's par was raised so its golden hash predates the rule unchanged.

Week 3 notes / deviations:
- Unity writes `.meta` files into `core/src` via the junction; they are
  committed (stable GUIDs) and harmless to `dotnet build` and the purity grep.
- Bare built-in render pipeline (no URP): flat-color vertex meshes on a single
  Sprites/Default material; the build script pins it in Always Included Shaders.
- UI is legacy uGUI built in code (no TMP, no scene-authored prefabs) so the
  whole game bootstraps from one GameObject and diffs stay reviewable.
- Editor 6000.3.22f1 chosen over 6000.4.7f1 because only it has the Android
  module installed.
- The GDD "feel pass" itself is yours: tune `Assets/PuttSeed/Resources/
  FeelConfig.asset` on device; every knob (friction, power curve, restitution,
  capture threshold, rest detection) is there. Defaults mirror SimConfig.Default.
- Not yet done (Week 4 per roadmap): signed .aab, daily/practice mode split,
  stats/streak, tutorials, audio/haptics.

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

## Remaining human steps

- On-device feel pass (tune FeelConfig.asset; R in the editor reloads it).
- Drop the purchased audio pack's clips onto the Feedback object's slots.
- Create the keystore, run `scripts\build-release.bat`, upload the .aab to
  the Play Console internal testing track.
