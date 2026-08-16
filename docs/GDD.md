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

## Course elements (MVP set — exactly these, no more)

| Element | Behavior |
|---|---|
| Wall segment | static collision, restitution ~0.8 |
| Bumper (circle) | restitution > 1, slight speed boost, satisfying *boing* |
| Sand zone (polygon) | high rolling friction while inside |
| Ice zone (polygon) | near-zero rolling friction: the ball slides far (added post-MVP, 2026-08-16) |
| Water zone (polygon) | ball sinks: +1 stroke, ball returns to last rest position |
| Hole | capture when ball overlaps at low enough speed; fast overlap rims out |

## Modes

- **Daily:** seed = f(UTC date). One course. Stats: strokes, attempts,
  best replay saved locally.
- **Practice:** random seed courses, unlimited. Same generator, labeled
  difficulty (generator's difficulty score → Easy/Normal/Hard).

## Scoring & retention surface

- Stars: 3 = under par, 2 = par, 1 = finished within limit.
- Local streak counter (played N days in a row) and per-day best.
- Ghost: yesterday's best run and any imported replay render as a
  translucent ghost ball with trail.

## Share format (UX)

`PUTT-<base64url>` — tapping "Share" copies text like:
`PUTTSEED day 214 — 2 strokes (par 3). Watch: PUTT-xK3f9aQ...`
Pasting a code into the app's import field plays the ghost.

## FTUE

First launch: 3 fixed tutorial courses (hand-authored seeds), teaching
shot, bumper, sand. No text walls; one hint line per course.

## Out of scope (do not build in MVP)

Online leaderboards, accounts, cloud save, IAP/skins, iOS release,
additional element types, level editor, multiplayer of any kind,
notifications. If a task drifts toward these, stop.
