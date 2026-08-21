# STATUS — measured numbers

Every feel pass adds draw calls, particles and meshes, and until this file
existed none of it was measured. These are the numbers later decisions are
allowed to argue with.

Nothing here is an estimate. A row without a measurement says so.

## Generation (desktop)

Measured with `dotnet run --project tools/CourseViewer -c Release -- --bench 40 --v4 --feel`,
which grows real courses under the game's own physics (`FeelConfig`) rather
than core's defaults.

| What | v4 (shipping) | v2 (legacy codes) |
|---|---|---|
| Generation | **1647 ms/course** | 71 ms/course |
| Attempts per accepted course | 2.23 | 2.33 |
| Sim throughput | 4.1 M ticks/s | 3.2 M ticks/s |
| Solver budget | 4 M ticks (≈966 ms) | 400 k ticks (≈124 ms) |

The jump is the point of v4, not a regression: par 3 costs what it costs
because the solver must prove no shorter solution exists, and that proof is
the product. It is also why the menu grows today's hole while the player
reads it and the game grows the next practice course during play — a
generation the player waits through is the thing to avoid, not the
generation itself.

Machine: the development desktop. A phone is slower; see below.

## On device — NOT YET MEASURED

`PerfProbe` (in every build, `Assets/PuttSeed/Runtime/PerfProbe.cs`) writes
one line per hole-out with all three numbers:

```
PuttSeed perf · capture frame: median 8.4 ms, 95th 11.2 ms, worst 18.3 ms · generation 412 ms · memory 128 MB after 7 courses
```

The capture celebration is sampled deliberately: zoom, slow-motion replay,
letterbox, confetti and the star reveal all run at once, so if anything drops
a frame it is that moment.

To fill this table, build, play until a few holes are finished, and read the
log:

```bash
adb logcat -s Unity | grep "PuttSeed perf"
```

| What | Target | Measured |
|---|---|---|
| Capture frame, median | ≤ 8.3 ms (120 fps) | — |
| Capture frame, worst | ≤ 16.6 ms (no dropped frame at 60) | — |
| Generation, v4 | (desktop 1647 ms) | — |
| Memory after 20 courses | flat, no growth per course | — |

Memory is the one to read across a session rather than at a point: a number
that climbs with every course is a leak, and a number that sits still is
fine wherever it sits.
