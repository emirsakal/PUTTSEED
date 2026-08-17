# PUTTSEED — Game Design Document

## Concept

One mini-golf hole per day. Everyone on Earth plays the same
procedurally generated course. Beat par, chase the fewest strokes,
share your run as a ~20-character replay code. Wordle loop, golf body.

## Pillars

1. **Fair by proof** — every generated course is machine-verified solvable
   at or under par before it ever reaches a player.
2. **One more try** — attempts are 10–20 seconds; restart is instant.
3. **Show, don't upload** — sharing needs no backend: a replay code pasted
   anywhere replays the exact run on any device.

## Controls & shot rules

- Drag anywhere: aim line appears from the ball, opposite to drag
  direction (slingshot). Release to shoot.
- Power: drag distance, clamped; power bar shown on the aim line.
- Aim angle and power are quantized (e.g. 1024 angle steps × 256 power
  steps) — this is what makes replays tiny and deterministic.
- Ball must come to rest before the next shot.
- Stroke limit per course: `par + 3`; hitting the limit = course failed
  (retry allowed, attempt counter shown).

## Course elements

The MVP set was "exactly these, no more"; it has since grown twice, each
time deliberately and dated. Everything below obeys the same contract:
fixed-point, deterministic, and provable by the solver before a player
sees it.

| Element | Behavior |
|---|---|
| Wall segment | static collision, restitution ~0.8 |
| Bumper (circle) | restitution > 1, slight speed boost, satisfying *boing* |
| Sand zone (polygon) | high rolling friction while inside |
| Ice zone (polygon) | near-zero rolling friction: the ball slides far (added post-MVP, 2026-08-16) |
| Water zone (polygon) | ball sinks: +1 stroke, ball returns to last rest position |
| Hole | capture when ball overlaps at low enough speed; fast overlap rims out |

The 2026-08-18 wave (generator v2 — see below):

| Element | Behavior |
|---|---|
| One-way gate (segment) | a valve: crossed freely along its pass normal, a solid wall against it |
| Ramp zone (polygon) | constant acceleration while inside — downhill lengthens the roll, uphill repels gentle shots |
| Portal pair (two discs) | entering one mouth reappears at its twin, velocity untouched |
| Windmill (rotating blades) | blades sweep a pivot; the phase advances with ticks since the current shot and re-arms every stroke, so the mill turns only while the ball rolls |

The windmill is the one that had to bend to fit: a real-time obstacle
would break both the solver (a rest state would no longer determine the
future) and the replay codec (codes would need timing). Tying its phase
to the shot clock keeps it a planning puzzle rather than a reflex test,
and keeps every guarantee intact.

## Generator versions

Adding elements changes what a seed generates — which would silently
rewrite every curated Journey level and every archived daily. So the
generator is versioned: **v1** (the five-element set) is frozen forever
and still regenerates Journey, the tutorial and every daily before the
cutover; **v2** (the wave above) runs practice and dailies from day 2430
(2026-08-27). Replay codes carry their version, so a code always
regenerates the course it was played on.

## Modes

- **Daily:** seed = f(UTC date). One course. Stats: strokes, attempts,
  best replay saved locally. The archive (added 2026-08-16) reopens any
  past date — courses regenerate from the date, so it needs no storage.
- **Practice:** random seed courses, unlimited. Same generator, labeled
  difficulty (generator's difficulty score → Easy/Normal/Hard).
- **Journey** (added 2026-08-17 at 50 levels, doubled the same day): a
  campaign of curated fixed seeds unlocked in order, three stars per
  level. Levels are ordinary generator seeds hand-picked from scan CSVs
  on a difficulty ramp — no new content pipeline, no new rules.
- **Tutorial:** four fixed courses (see FTUE).

## Scoring & retention surface

- Stars: 3 = par or better, 2 = one over par, 1 = finished within limit.
  (Recalibrated 2026-08-18. The original 3 = under par was written for
  courses of varying par; in practice generation certifies par 2 for
  every seed, so "under par" meant an ace — a tier most layouts have no
  line for at all, while one star covered three, four and five strokes.
  The ace keeps its own reward: the hole-out vocabulary and the Ace
  achievement. See LATER.md "Deeper pars" for the root-cause fix.)
- Local streak counter (played N days in a row) and per-day best.
- Ghost: the day's best run and any imported replay render as a
  translucent ghost ball with trail.
- Cosmetics (added post-MVP): ten ball skins unlocked by achievements
  and Journey progress — pure progression rewards, never purchases.

## Share format (UX)

`PUTT-<base64url>` — tapping "Share" copies text like:
`PUTTSEED day 214 — 2 strokes (par 3). Watch: PUTT-xK3f9aQ...`
Pasting a code into the app's import field plays the ghost.

## FTUE

First launch: fixed tutorial courses (hand-authored seeds) teaching
shot, bumper, sand — grown to four when ice shipped. No text walls; one
hint line per course.

## Out of scope (do not build in MVP)

Online leaderboards, accounts, cloud save, IAP/skins, iOS release,
additional element types, level editor, multiplayer of any kind,
notifications. If a task drifts toward these, stop.

Two entries have since graduated deliberately, without breaking the
spirit of the list: ice became a fifth element post-MVP (2026-08-16),
and skins shipped as local progression-gated cosmetics — the **IAP**
half of "IAP/skins" remains permanently out. Everything else above is
still out; new ideas go to LATER.md.
