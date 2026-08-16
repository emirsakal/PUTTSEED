# PUTTSEED

Daily-seed deterministic 2D mini-golf for Android (Unity 6, C#).
One course per day, generated from a date-derived seed, identical on every
device. Physics is fixed-point and bit-deterministic; a replay is just
`(seed + shot inputs)` encoded as a short shareable code.

This repo is a portfolio piece: engineering quality is a first-class goal.
Read `docs/ARCHITECTURE.md`, `docs/GDD.md`, `docs/ROADMAP.md` before
starting any phase. Work phase by phase as prompted; do not skip ahead.

## Repo layout

```
Assets/                     # Unity 6 project lives at the REPO ROOT
  PuttSeedCore/src          #   junction -> core/src/PuttSeed.Core (asmdef:
                            #   noEngineReferences) — git-ignores the junction path
  PuttSeed/                 #   Unity layer (rendering, input, UI only)
Packages/ ProjectSettings/  # Unity project files (Library/ etc. git-ignored)
core/
  src/PuttSeed.Core/        # pure C# (netstandard2.1). ZERO UnityEngine refs.
  tests/PuttSeed.Core.Tests/# NUnit, runs via `dotnet test`
docs/                       # GDD, architecture, roadmap
scripts/                    # win batch: test.bat, build-android.bat, unity-tests.bat
tools/                      # CourseViewer console app
prompts/                    # working prompts (not part of the product)
```

Open the repo root itself in Unity Hub — there is no `unity/` subfolder.

## Commands

- `dotnet test core` — full core test suite. Must be green before any commit.
- `scripts\unity-tests.bat` — Unity EditMode tests in batch mode.
- `scripts\build-android.bat` — batch-mode .aab build.

## Hard rules (non-negotiable)

1. **Determinism:** inside `core/` there is NO `float`/`double`, no
   `System.Random`, no `DateTime.Now`, no dictionary-iteration-order
   dependence, no LINQ in the tick path. All math uses `Fix64` (Q32.32 on
   `long`). RNG is a seeded xorshift implemented in core. All external
   inputs (aim angle, power) are quantized to fixed-point AT THE BOUNDARY
   before entering the sim.
2. **Layering:** `core/` never references UnityEngine. Unity code never
   contains game rules — it renders state and forwards quantized input.
   If a rule needs Unity data, the design is wrong; stop and rethink.
3. **TDD for core:** write the failing test first for every core feature.
   Golden tests, property tests and the determinism hash test are part of
   the deliverable, not an afterthought.
4. **Fixed tick:** sim runs at 120 Hz fixed dt. Rendering interpolates
   between the last two sim states; never step the sim from
   `Update()`/`deltaTime`.
5. **Small commits:** conventional commits (`feat:`, `fix:`, `test:`,
   `refactor:`, `docs:`). Commit after each green test cycle.

## Code style

- C# 9 compatible with netstandard2.1. Nullable enabled in core.
- Structs for hot-path types (`Fix64`, `Vec2Fix`, `BallState`); no
  per-tick heap allocation in the sim loop (verify: zero GC alloc in the
  tick under profiler).
- Public core APIs get XML doc comments — this repo will be read by
  reviewers.

## Definition of done (per task)

- Tests green (`dotnet test core`), no warnings.
- No rule above violated.
- README section updated if the public surface changed.
