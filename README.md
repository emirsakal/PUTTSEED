# PUTTSEED

[![CI](https://github.com/emirsakal/PUTTSEED/actions/workflows/ci.yml/badge.svg)](https://github.com/emirsakal/PUTTSEED/actions/workflows/ci.yml)

**One mini-golf hole per day. Everyone on Earth plays the same course.**

PUTTSEED is a daily-seed, bit-deterministic 2D mini-golf game for Android
(Unity 6 + a pure C# core). Each UTC day derives a seed; the seed generates a
course that is machine-verified solvable before any player sees it; and
because the physics is fixed-point and fully deterministic, a finished run
compresses to a ~30-character code that replays the exact same shots on any
device — no backend, no accounts, no uploads.

```
PUTTSEED — 2 strokes (par 2). Watch: PUTT-AQMAAAAAAAAAAmD_A2B8Ag
```

<table>
  <tr>
    <td align="center"><img src="docs/media/menu.png" width="200" alt="Main menu: today's hole, the journey campaign, practice and archive"></td>
    <td align="center"><img src="docs/media/daily-hole.png" width="200" alt="A daily hole: sand and ice zones, bumpers, the cup tucked into sand"></td>
    <td align="center"><img src="docs/media/journey.png" width="200" alt="Journey level select: a paged grid of 100 levels with per-level stars"></td>
    <td align="center"><img src="docs/media/collection.png" width="200" alt="Collection: ball skins in a grid, the equipped one wearing an accent ring"></td>
  </tr>
  <tr>
    <td align="center"><b>Today's hole</b><br>one course, everyone</td>
    <td align="center"><b>The elements</b><br>sand, ice, bumpers</td>
    <td align="center"><b>Journey</b><br>100 curated seeds</td>
    <td align="center"><b>Collection</b><br>progression-gated skins</td>
  </tr>
</table>

## A course, as the debug viewer prints it

```
       #                    seed        3
      ###                   par         2   difficulty Hard
     ##  #                  walls       10
    ##    #                 bumpers 2 · sand 2 · ice 2 · water 1
   #       #
  #   S    ##               S ball start
  #         ##              H hole
   #         ##             # wall      o bumper
   ##         #             : sand      * ice     ~ water
    ##        #
     ##   *oo~#
      #****oo~##
      ##***oo~~#
       #***~~~~#
       #*****  ##
       #**     :##
        #     :o:##
        #    :oo::#
        ##   :oo:::#
         ##   ::::::#####
          ##   :::: *** #
           #    ::  *** #
            #       *** #
             #      :H: #
             ##     ::: #
              ##    ::: #
               #    ::: #
                #########
```

`dotnet run --project tools/CourseViewer -c Release -- <seed|yyyy-mm-dd>`

## Architecture

```
+--------------------------------------------------------------+
|  Unity layer (repo root: Assets/)                            |
|  rendering · drag input · UI · audio/haptics · interpolation |
|                                                              |
|  InputQuantizer   -> drag becomes a 10-bit angle + 8-bit     |
|                      power index — THE quantization boundary |
|  FixView          -> Fix64 becomes float — render-only,      |
|                      one-way, never flows back               |
|  FixedStepper     -> frame time becomes whole 120 Hz ticks   |
+--------------------------------------------------------------+
|  core/src/PuttSeed.Core (netstandard2.1 — ZERO UnityEngine)  |
|                                                              |
|  FixedMath   Fix64 (Q32.32) · Vec2Fix · xorshift128 RNG ·    |
|              committed 1024-entry sine table                 |
|  Sim         GolfSim: 120 Hz fixed tick, circle-segment      |
|              walls with sub-stepping, bumpers, sand, ice,    |
|              water, gates, ramps, portals, windmills, hole   |
|              capture, rest, FNV-1a StateHash                 |
|  CourseGen   corridor growth -> hazard decoration ->         |
|              SolvabilityChecker (bounded BFS over the        |
|              quantized shot space) -> DifficultyRater;       |
|              versioned configs freeze published courses      |
|  Replay      [seed + timed shots] <-> PUTT- base64url codes  |
|  Daily       UTC date -> seed (FNV-1a + SplitMix64, salted)  |
+--------------------------------------------------------------+
```

The Unity project sits at the repo root; core sources are linked into
`Assets/PuttSeedCore/src` via a directory junction under an asmdef with
`noEngineReferences: true`, so the compiler itself enforces the layering.
(Unity writes `.meta` files into `core/src` through that junction; they are
committed for stable asset GUIDs and are inert to `dotnet build` and the
purity grep — the C# sources themselves stay UnityEngine-free.)

## The determinism proof

The whole product depends on one property: **the same seed and the same
inputs produce the same bits on every device.** That claim is enforced
mechanically, not by care:

- **No floats exist in core.** `scripts\check-purity.bat` greps `core/src`
  for `float`, `double`, `System.Random`, `DateTime` and `UnityEngine` and
  fails the build if any appear. All math is `Fix64` — Q32.32 fixed point on
  `long`, with 128-bit multiply intermediates and Newton square root. Trig is
  a committed 1024-entry table; angles only ever exist as table indices.
- **The 10k-tick golden hash** (`DeterminismTests`): a fixture course
  holding every element — walls, bumpers, sand, ice, water, gates,
  ramps, portals and windmills — runs a scripted 16-shot, 10,000-tick
  session twice in-process, and the final FNV-1a state hash must equal a
  committed constant: `11426007175965104957`. Any accidental change to
  sim math fails it. A companion test guards the guard: removing any one
  element must move the hash, so nothing can sit in the fixture without
  being met. That test found two elements the previous fixture never
  touched, water among them.
- **Golden replay fixtures** (`GoldenReplayTests`): three seeds run
  end-to-end — generate, replay the author solution — against frozen final
  hashes and frozen `PUTT-` codes.
- **The 1000-seed property suite** (`PropertySuiteTests`): every seed
  generates within bounded attempts, every accepted course's author solution
  replays to a capture within par, and every replay code round-trips.
- **Quantization boundary tests** (Unity EditMode): the drag-to-ShotInput
  mapping and the frame-time-to-ticks stepper are tested where analog meets
  discrete; floats stop existing at that line.
- **A replay that desyncs is a bug by definition** — the codec stores only
  `(seed, shots)`, plus the blade phase each shot was taken at once
  windmills started turning while the ball rests; playback is
  re-simulation, never recorded motion.

## Running things

| What | How |
|---|---|
| Core test suite (275 tests) | `dotnet test core` — or `scripts\test.bat` (purity grep + Release run) |
| Unity EditMode tests (103 tests) | `scripts\unity-tests.bat` |
| ASCII course viewer | `dotnet run --project tools/CourseViewer -c Release -- 3 --stats` |
| Screenshot for the README | enter Play mode, then **PuttSeed → Capture Screenshot** (writes `docs/media/`) |
| Debug Android build | `scripts\build-android.bat` (`apk` arg for an installable APK) |
| Release .aab (signed) | `scripts\build-release.bat` |

Requirements: .NET SDK 8, Unity 6000.3.x with the Android module (open the
repo root in Unity Hub).

### Signing (release builds)

Create `keystore.properties` in the repo root — it is gitignored and must
never be committed:

```
storeFile=puttseed.keystore
storePassword=<store password>
keyAlias=puttseed
keyPassword=<key password>
```

Generate a keystore once with:

```bash
keytool -genkeypair -v -keystore puttseed.keystore -alias puttseed -keyalg RSA -keysize 2048 -validity 10000
```

`scripts\build-release.bat` picks it up automatically and warns loudly if it
builds unsigned.

## Game rules in one breath

Drag to aim (slingshot or direct — your pick), release to shoot; ball must
rest before the next stroke. Walls bounce, bumpers boost, sand drags, ice
slides, water costs a stroke and a reset; gates let you through one way
only, ramps push, portals teleport, and windmill blades sweep whether or
not you are ready. One day in eighteen adds a twist of its own — ice
underfoot, springier bumpers, or a crosswind — proven solvable under the
twist before it ships. Capture needs a slow ball over the
cup — fast attempts rim out. Stroke limit is par + 3; holing out scores
stars — 3 at par or better, 2 one over, 1 within the limit. Feel tuning
lives in one ScriptableObject (`Assets/PuttSeed/Resources/FeelConfig.asset`).

## Four modes, one generator

- **Daily** — the headline mode: one UTC hole, identical worldwide, with a
  local streak. Your best run rides along as a translucent ghost on the next
  attempt, and the archive reopens any past day — courses regenerate from
  the date alone, so browsing history needs no storage. A random-day button
  picks an unplayed date for you.
- **Journey** — a 100-level campaign of curated seeds, unlocked in order,
  three stars per level. Levels are literally just seeds, picked from
  generator scans on a difficulty ramp — the solvability proof and the
  replay codec apply unchanged.
- **Practice** — unlimited courses by difficulty (Easy / Normal / Hard),
  per-difficulty personal bests, an undo mulligan, and course invites: any
  course shares as a code a friend can play on their own device.
- **Tutorial** — four fixed courses teach the elements; the first launch
  walks straight into them.

## Meta — local by design

No backend, no accounts, no IAP. Everything below lives in the local save
and travels between devices as a single `PUTTSAVE-` code (export/import in
the stats panel):

- eight achievements, plus a stats panel with a stroke-distribution
  histogram;
- ten ball skins gated by achievements and Journey progress — the Collection
  grid spells out each locked skin's unlock condition;
- watchable replays: paste any `PUTT-` code to watch that run in-game;
- EN/TR localization, and settings for sound, haptics, aim style, a
  colorblind palette and a 60/120 FPS battery mode;
- every sound is synthesized: the 12 WAV clips are generated by a committed
  editor tool (`SfxSynth`) — zero recorded audio in the repo.

Out-of-scope ideas live in [LATER.md](LATER.md) — deliberately.

## Workflow note

This repo was built with an AI-assisted workflow (Claude Code); the phase
prompts that drove each week's work are committed verbatim in
[prompts/PROMPTS.md](prompts/PROMPTS.md). The guardrails those prompts
enforce — TDD for the core, the purity grep, committed golden hashes — are
exactly what makes an AI-assisted codebase auditable: correctness is proven
by machine-checked tests, not by trust in the author, human or otherwise.

## License

[MIT](LICENSE).
