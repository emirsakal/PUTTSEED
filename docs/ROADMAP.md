# PUTTSEED — Roadmap (4 weeks × 10–15 h)

Rule: a week's Acceptance items are verified by the developer running the
commands himself, not by claims in chat. If Week 3's feel check fails,
Week 4 store tasks slip — feel wins.

## Week 1 — Deterministic core

- [ ] Repo scaffolding: core solution, NUnit test project, scripts/, CI-less
      but scriptable (`dotnet test core` from repo root).
- [ ] `Fix64`, `Vec2Fix`, `FixRng`, sin/cos table + tests.
- [ ] `GolfSim`: ball integration, friction, walls (circle–segment),
      sub-stepping, rest detection, `StateHash()`.
- [ ] Bumper, sand, water, hole capture rules + tests.
- [ ] Determinism test: 10k ticks, golden hash committed.

**Acceptance:** `dotnet test core` fully green; determinism + golden hash
tests exist and pass; zero float/double usages in core (grep check).

## Week 2 — Generation, solvability, replay

- [ ] Corridor generator + decoration + clearance rules.
- [ ] SolvabilityChecker (BFS over quantized shot space, tick-capped) +
      author solution storage.
- [ ] DifficultyRater with 3 buckets.
- [ ] `ReplayCodec` + round-trip tests; golden replay fixtures.
- [ ] `DailySeed`.
- [ ] Property test suite: 1000 seeds → all accepted courses solvable,
      generation bounded, codec round-trips.

**Acceptance:** property suite green; a console harness prints an ASCII
render of any seed's course + author solution (debug tool, also a nice
README artifact).

## Week 3 — Unity layer & feel

- [ ] Unity 6 project created via CLI, core linked (junction/asmdef,
      `noEngineReferences: true`).
- [ ] SimRunner with interpolation; drag input → quantized ShotInput.
- [ ] Course rendering: walls as line meshes, zones as flat polys, flat
      color palette; ball trail; camera framing per course bounds.
- [ ] Ghost playback (author solution + imported replays).
- [ ] Minimal UI: aim line + power, stroke counter, retry, share/import.
- [ ] Feel pass with the developer: friction, power curve, bumper punch,
      hole capture threshold. Time-boxed daily on-device sessions.

**Acceptance:** playable Android build on a real device at 60 fps; the
developer rates feel ≥ "good enough to keep playing"; replay import from
a pasted code works device-to-device.

## Week 4 — Modes, polish, release

- [ ] Daily mode (UTC date seed), practice mode, local stats + streak.
- [ ] 3 tutorial courses (fixed seeds) + one-line hints.
- [ ] Audio (purchased pack), haptics on hit/capture, minor juice
      (squash on impact, hole-in celebration).
- [ ] App icon, splash, Android signing, .aab, Play Console internal
      testing track.
- [ ] README.md for the repo: architecture overview, determinism proof
      section (test output screenshots), ASCII course render, GIFs.

**Acceptance:** signed .aab on internal testing; README presentable to a
technical reviewer.

## Explicit non-goals

See GDD "Out of scope". Any new idea goes to a `LATER.md`, not into the
sprint.
