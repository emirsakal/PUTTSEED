# PUTTSEED

**One mini-golf hole per day. Everyone on Earth plays the same course.**

PUTTSEED is a daily-seed, bit-deterministic 2D mini-golf game for Android
(Unity 6 + a pure C# core). Each UTC day derives a seed; the seed generates a
course that is machine-verified solvable before any player sees it; and
because the physics is fixed-point and fully deterministic, a finished run
compresses to a ~30-character code that replays the exact same shots on any
device — no backend, no accounts, no uploads.

```
PUTTSEED — 2 strokes (par 2). Watch: PUTT-AQMAAAAAAAAAAkD_AyB8Ag
```

## A course, as the debug viewer prints it

```
       #                    seed        3
      ###                   par         2   difficulty Hard
     ##  #                  walls       16
    ##    #                 bumpers 2 · sand 2 · water 1
   #       #
  #   S     #               S ball start
  #         ##              H hole
   #         #              # wall      o bumper
   ##         #             : sand      ~ water
    ##        #
     ##    oo~#
      ##   oo~##
      ##   oo~~#
       #   ~~~~#
       #       ##
       #       ###
       ##     :o:#
        #    :oo::#
        ##   :oo:::#
         ##   ::::#######
          #    ::::###  #
           #    ::      #
            #           #
            ##      :H: #
             ##     ::: #
              ##    ::: #
               ##   ::: #
                 ########
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
|              walls with sub-stepping, bumpers, sand, water,  |
|              hole capture, rest detection, FNV-1a StateHash  |
|  CourseGen   corridor growth -> hazard decoration ->         |
|              SolvabilityChecker (bounded BFS over the        |
|              quantized shot space) -> DifficultyRater        |
|  Replay      [seed + shots] <-> PUTT- base64url codes        |
|  Daily       UTC date -> seed (FNV-1a + SplitMix64, salted)  |
+--------------------------------------------------------------+
```

The Unity project sits at the repo root; core sources are linked into
`Assets/PuttSeedCore/src` via a directory junction under an asmdef with
`noEngineReferences: true`, so the compiler itself enforces the layering.

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
  exercising every element runs a scripted 8-shot, 10,000-tick session twice
  in-process, and the final FNV-1a state hash must equal a committed
  constant: `531089411828813883`. Any accidental change to sim math fails it.
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
  `(seed, shots)`; playback is re-simulation.

## Running things

| What | How |
|---|---|
| Core test suite (169 tests) | `dotnet test core` — or `scripts\test.bat` (purity grep + Release run) |
| Unity EditMode tests | `scripts\unity-tests.bat` |
| ASCII course viewer | `dotnet run --project tools/CourseViewer -c Release -- 3 --stats` |
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

Drag to aim (slingshot), release to shoot; ball must rest before the next
stroke. Walls bounce, bumpers boost, sand drags, water costs a stroke and a
reset. Capture needs a slow ball over the cup — fast attempts rim out.
Stroke limit is par + 3. Daily mode tracks local stats and a streak;
practice mode serves unlimited courses by difficulty; three fixed tutorial
courses teach the elements. Feel tuning lives in one ScriptableObject
(`Assets/PuttSeed/Resources/FeelConfig.asset`).

Out-of-scope ideas live in [LATER.md](LATER.md) — deliberately.
