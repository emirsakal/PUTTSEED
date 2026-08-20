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
| Windmill (rotating blades) | blades sweep a pivot on a free-running clock — they keep turning while you line up, so WHEN you shoot is part of the shot |

The windmill is the one that cost something. It first shipped with its
phase re-armed on every stroke, which kept replays timing-free but left
the blades frozen while a player lined up — a windmill that stops is a
broken windmill. It now runs on a free clock, and the price is paid in
two places, deliberately:

- **Replays carry timing.** A code records the blade phase each shot was
  taken at (codec v3, one extra byte per shot). Playback still
  re-simulates; it just also reproduces the pauses that mattered.
- **The solver still proves par without waiting.** Expanding a rest
  state re-arms the clock, so generation certifies a solution that shoots
  immediately. A player who waits for a better blade angle has strictly
  more options than the proof used, so the guarantee holds.

The clock wraps every 1024 ticks, which is why ten bits per shot is
enough and why a ghost never idles longer than one turn of the blades.

## Themed days

One day in eighteen carries a twist, derived from the seed alone: an
**icy day** (the whole green plays slick), a **bouncy day** (bumpers
kick harder) or a **windy day** (a steady crosswind bends every roll).
The cheapest content in the game — no geometry, no art, just the physics
knobs the sim already had, turned.

Two rules keep it honest. The twist is a function of the seed, so a
replay code reproduces it for free and no timing or extra payload is
needed. And generation runs UNDER the twist: the solver proves the
course solvable in the same wind the player will meet, so a themed day
is never an unfair day. The HUD names the day, because ice underfoot
with no explanation reads as the game misbehaving.

Themed days ride on generator v2, like every other post-MVP rule: v1
regenerates Journey and the archive, and a twist there would rewrite
finished history.

## Generator versions

Adding elements changes what a seed generates — which would silently
rewrite every curated Journey level and every archived daily. So the
generator is versioned, and a replay code carries its version, so a code
always regenerates the course it was played on.

**v4** runs everything today: the whole calendar, Journey, the tutorial
and practice. It is the first generator whose corridors are long enough
to be worth three strokes. Moving the entire game onto it — rewriting
every curated level and every past day — was possible exactly once,
before release, and this was that once; the next change will need a real
cutover in `GeneratorSchedule`. **v1** (the five-element MVP set) and
**v2** (the element wave) are frozen and kept because a decoder must
understand every version byte it ever emitted. **v3** is v2's courses
with shot timing, not a generator of its own.

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
- **Tutorial:** five fixed courses covering all nine elements (see FTUE).
- **Weekly gauntlet** (added 2026-08-18): the seven dailies of the last
  fully elapsed week, played back to back for one cumulative stroke
  total. No new content — the seven holes already shipped as dailies,
  and every player runs the same week. A hole that runs out of strokes
  banks its limit and the week carries on: one bad hole should cost a
  week, not end it. The whole run shares as a single `PUTTWK-` code, the
  seven seeds derived from the week index rather than stored.

## Scoring & retention surface

- Par is 2 or 3, roughly two holes in three against one (v4). Every hole
  was a par 2 until 2026-08-19 — not by rule, but because a corridor
  capped at twelve units is two shots of winding progress.
- Stars: 3 = par or better, 2 = one over par, 1 = finished within limit.
  (Recalibrated 2026-08-18, when par was still always 2 and "under par"
  therefore meant an ace. With par 3 in the mix, under par is reachable
  again — two strokes on a par 3 — and the curve now works as written
  rather than as a workaround.)
- **The day's answer is its first finish.** Retries are unlimited and
  instant, as the one-more-try pillar requires, but they feed the
  personal best rather than the day: a score reached on the thirty-fourth
  attempt is not comparable with anyone's, and comparability is what a
  daily is for. The first finish fills the streak, the par streak, the
  calendar, the closing card and the share.
- Local streak counter (played N days in a row), a par streak (days whose
  first finish reached par — the streak that can actually break), and a
  per-day best.
- Ghost: the day's best run and any imported replay render as a
  translucent ghost ball with trail.
- Cosmetics (added post-MVP): ten ball skins unlocked by achievements
  and Journey progress — pure progression rewards, never purchases.

## Share format (UX)

`PUTT-<base64url>` — tapping "Share" copies text like:
`PUTTSEED day 214 — 2 strokes (par 3). Watch: PUTT-xK3f9aQ...`
Pasting a code into the app's import field plays the ghost.

## FTUE

First launch: fixed tutorial courses (hand-picked seeds), one hint line
each, no text walls. Rebuilt 2026-08-19, when four elements (gate, ramp,
portal, windmill) were shipping with nothing teaching them anywhere and
water had gone untaught since the MVP despite being the only element that
costs a stroke.

Nine elements do not mean nine lessons. The ones that share an idea share
a course — the two that change your speed, the slide and the penalty, the
two the arrows point through, the two that act on their own — so each
pair is one sentence instead of two facts, and the tutorial came out
**shorter than before it grew**: five lessons, nine elements.

| Lesson | Teaches |
|---|---|
| 1 | the shot, on bare ground |
| 2 | bumpers · sand |
| 3 | ice · water |
| 4 | gates · ramps |
| 5 | portals · windmills |

Three rules keep the lessons honest, all enforced by tests rather than by
care. A lesson's course carries **exactly** what its hint names — every
element mentioned, and none that is not; the opening lesson's seed had
drifted into carrying water and ice, so the first hole a new player ever
saw opened with two elements the tutorial had not reached yet. Every
element in the game is taught by some lesson. And no lesson is a themed
day: the paired wave lessons are v2 seeds, where one seed in eighteen
turns icy, bouncy or windy, and a beginner cannot tell a themed day from
a broken one.

## Out of scope (do not build in MVP)

Online leaderboards, accounts, cloud save, IAP/skins, iOS release,
additional element types, level editor, multiplayer of any kind,
notifications. If a task drifts toward these, stop.

Two entries have since graduated deliberately, without breaking the
spirit of the list: ice became a fifth element post-MVP (2026-08-16),
and skins shipped as local progression-gated cosmetics — the **IAP**
half of "IAP/skins" remains permanently out. Everything else above is
still out; new ideas go to LATER.md.
