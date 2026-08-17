# PUTTSEED — Roadmap (4 weeks × 10–15 h)

> **Status: completed.** Every acceptance gate below passed; the boxes
> are ticked where the plan was followed and annotated where reality
> diverged. Work since the roadmap is summarized at the bottom.

Rule: a week's Acceptance items are verified by the developer running the
commands himself, not by claims in chat. If Week 3's feel check fails,
Week 4 store tasks slip — feel wins.

## Week 1 — Deterministic core

- [x] Repo scaffolding: core solution, NUnit test project, scripts/, CI-less
      but scriptable (`dotnet test core` from repo root).
- [x] `Fix64`, `Vec2Fix`, `FixRng`, sin/cos table + tests.
- [x] `GolfSim`: ball integration, friction, walls (circle–segment),
      sub-stepping, rest detection, `StateHash()`.
- [x] Bumper, sand, water, hole capture rules + tests.
- [x] Determinism test: 10k ticks, golden hash committed.

**Acceptance:** `dotnet test core` fully green; determinism + golden hash
tests exist and pass; zero float/double usages in core (grep check).

## Week 2 — Generation, solvability, replay

- [x] Corridor generator + decoration + clearance rules.
- [x] SolvabilityChecker (BFS over quantized shot space, tick-capped) +
      author solution storage.
- [x] DifficultyRater with 3 buckets.
- [x] `ReplayCodec` + round-trip tests; golden replay fixtures.
- [x] `DailySeed`.
- [x] Property test suite: 1000 seeds → all accepted courses solvable,
      generation bounded, codec round-trips.

**Acceptance:** property suite green; a console harness prints an ASCII
render of any seed's course + author solution (debug tool, also a nice
README artifact).

## Week 3 — Unity layer & feel

- [x] Unity 6 project created via CLI, core linked (junction/asmdef,
      `noEngineReferences: true`). *(Deviation: the Unity project lives
      at the repo root, not in a `unity/` subfolder.)*
- [x] SimRunner with interpolation; drag input → quantized ShotInput.
- [x] Course rendering: walls as line meshes, zones as flat polys, flat
      color palette; ball trail; camera framing per course bounds.
- [x] Ghost playback (author solution + imported replays).
- [x] Minimal UI: aim line + power, stroke counter, retry, share/import.
- [x] Feel pass with the developer: friction, power curve, bumper punch,
      hole capture threshold. Time-boxed daily on-device sessions.

**Acceptance:** playable Android build on a real device at 60 fps; the
developer rates feel ≥ "good enough to keep playing"; replay import from
a pasted code works device-to-device.

## Week 4 — Modes, polish, release

- [x] Daily mode (UTC date seed), practice mode, local stats + streak.
- [x] 3 tutorial courses (fixed seeds) + one-line hints. *(Shipped as
      four once ice landed.)*
- [x] Audio (purchased pack), haptics on hit/capture, minor juice
      (squash on impact, hole-in celebration). *(Deviation: no purchased
      pack — all 12 SFX are synthesized by a committed editor tool.)*
- [x] App icon, splash, Android signing, .aab, Play Console internal
      testing track. *(In-repo parts shipped — adaptive icon, keystore
      flow, `build-release.bat`; the Play Console upload itself is a
      store-side step outside the repo.)*
- [x] README.md for the repo: architecture overview, determinism proof
      section (test output screenshots), ASCII course render, GIFs.
      *(GIF/screenshot capture still pending — tracked below.)*

**Acceptance:** signed .aab on internal testing; README presentable to a
technical reviewer.

## Explicit non-goals

See GDD "Out of scope". Any new idea goes to a `LATER.md`, not into the
sprint.

## After the roadmap (2026-08, post-MVP)

The four weeks above ended with a playable, signed daily-golf MVP. The
work since, in rough order:

- **Fifth element:** ice zones (near-zero friction) across generator,
  sim, golden fixtures and the tutorial.
- **Retention surface:** star scoring in core, best-run ghost, streak,
  FTUE, fail card, share sheet, next-hole countdown, golf vocabulary on
  hole-out, stats panel + stroke histogram, eight achievements.
- **Archive:** any past day regenerates from its date (no storage) +
  a random-unplayed-day picker.
- **Feel & polish passes:** synthesized SFX + 3-tier haptics, particles,
  slow-mo + letterbox, camera work, scene fades, living menu. Two loops
  were tried and deliberately removed (rolling loop, ambient pad) — the
  felt stays silent between events.
- **Meta & UX:** mulligan/undo, practice PBs, course invite codes,
  save transfer (`PUTTSAVE-`), colorblind palette, battery FPS mode,
  clipboard replay offer, EN/TR localization, menu overhaul.
- **Journey:** a 100-level campaign of curated seeds (two scan-and-curate
  passes) with sequential unlock, per-level stars and skin gates; the
  cosmetics Collection grew to ten skins behind a square-grid UI.
- **CI:** purity grep + full core suite on every push.

Still open: README capture assets (GIFs/screenshots) and a WebGL demo.
