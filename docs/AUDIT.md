# PUTTSEED — game audit

Studio-style design, feel and product audit. Analysis only: no code,
scene or asset was changed to produce it. Every claim about the current
build is backed by a file, a screenshot, a generator scan or a commit.

Date: 2026-08-18 · Build audited: `3be2f6d` (core 275 tests, EditMode 103)

---

## A. Executive summary

    Current level of the game            6.5 / 10

    Core Gameplay        6/10     Progression           7/10
    UI/UX                6/10     Retention Potential   5/10
    Visual Quality       6/10     Shareability          3/10
    Animation            7/10     Accessibility         7/10
    VFX                  7/10     Technical Quality     9/10
    Sound                6/10     Portfolio Impression  8/10
    Game Feel            7/10     Market Potential      4/10
    Onboarding           5/10     ------------------------
                                  Overall               6.5/10

**The single biggest weakness, in one sentence:** the daily — the whole
product's headline — has no stakes and no artifact, because you may
retry it forever and what you share afterwards is an unreadable code
instead of a scorecard.

### The 5 greatest strengths

1. **The determinism proof is real and mechanically enforced** — purity
   grep, a 10k-tick golden hash with an `EveryElement_ChangesTheHash`
   guard, 1000-seed property suite, CI on every push. Almost no hobby
   game can make this claim; it is the portfolio's spine.
2. **Generation is honest.** Every course is machine-proven solvable
   before shipping, under the day's mutators. Competitors in this exact
   niche cannot say that.
3. **Backend-free sharing that actually re-simulates.** A ~30-character
   code reproduces a run bit-exactly. That is a genuinely rare
   engineering property and the seed of every social feature the game
   will ever need.
4. **The juice layer is much better than the screenshots suggest** —
   putter swing at the contact point, oriented squash, staged star
   reveal in step with rising notes, capture zoom, slow-mo, confetti,
   three-tier haptics, twelve synthesized SFX (`FeedbackController.cs`).
5. **Content depth per hour of work is high**: 10 elements, 100 curated
   Journey levels, archive calendar, weekly gauntlet, themed days, 10
   skins, 13 achievements — all from one generator and one save file.

### The 5 biggest problems

1. **The daily has no stakes.** Retries are unlimited and the best run
   is kept ([StatsStore.cs:16](Assets/PuttSeed/Runtime/StatsStore.cs:16)
   `attempts` + `bestStrokes`); the menu screenshot proudly reads
   "Today: 34 attempt(s)". Everyone's shared score converges to the
   same number given patience, so the score means nothing. The direct
   competitor putt.day solves this with "your first finish counts".
2. **The share is a code, not a scorecard.** `BuildShareText`
   ([ModeController.cs:186](Assets/PuttSeed/Runtime/ModeController.cs:186))
   emits `PUTTSEED day 214 — 2 strokes (par 2). Watch: PUTT-…`. Wordle's
   virality came from a spoiler-free *visual* grid, invented by a player
   and adopted by the developer. We have the data for one and print
   none of it.
3. **Par is always 2.** A 300-seed scan just now: **par 2 in 300 of 300
   courses**, hazards averaging 5.8, difficulty split Easy 104 / Normal
   106 / Hard 90. Consequences you can see in the product: `GolfTerms`
   can never print "Birdie" or "Eagle" — both branches are unreachable
   while par is 2 ([GolfTerms.cs](Assets/PuttSeed/Runtime/GolfTerms.cs));
   every day poses the same two-decision question. Puttdle ships five
   holes at par 17 per day.
4. **A third of the screen does the work.** `CameraFramer` fits the
   course into the full viewport with an aspect divide
   ([CameraFramer.cs:34](Assets/PuttSeed/Runtime/CameraFramer.cs:34)); on
   a 0.46-aspect phone a wide course leaves the top and bottom thirds
   empty (`docs/media/daily-hole.png`). The ball — the thing you stare
   at — is among the smallest objects on screen.
5. **The category is browser-first and we are an unlisted Android app.**
   putt.day (hole #98), Puttdle, Minigolfle and wordlegolf.com all ship
   the same premise as an instant-play web page; PC Gamer covered one of
   them. A WebGL build stopped being a portfolio nicety and became the
   distribution model of the entire niche.

### First 10 things to do

1. `SHARE-1` Emoji scorecard share (the Wordle mechanism, our data).
2. `DAILY-1` First finish counts — the daily gets a real result.
3. `CAM-1` Rotate wide courses 90° in the view; reclaim the screen.
4. `UI-1` HUD hierarchy: strokes as the hero number, score vs par as a chip.
5. `DAILY-3` Show the day's difficulty — the capture rule already changes with it.
6. `AUD-1`/`VFX-1` Give failure a body; give ramps any feedback at all.
7. `META-1` One "next goal" line on the menu.
8. `UI-2` Demote Ghost/Watch out of the primary button row.
9. `TEACH-1` Teach the six elements the tutorial never mentions.
10. `PRES-1` WebGL build + hero GIF — the only two things standing
    between this repo and an audience.

### The 10 features most worth adding

`SHARE-1` emoji scorecard · `DAILY-1` first-finish result · `DAILY-2`
par streak (the streak that can actually break) · `MECH-1` deeper pars ·
`UI-3` end-of-day card that closes the ritual · `MENU-1` today's hole as
a live thumbnail on the menu · `TEACH-2` first-encounter element hints ·
`VIS-1` the rough — visually separate in-play from out-of-play ·
`AUD-2` power-mapped shot pitch · `ACC-1` reduce-motion switch.

### The 10 things to learn from competitors

1. **putt.day** — "unlimited attempts, your first finish counts". The
   exact fix for our stakes problem, already validated in this niche.
2. **putt.day** — a first shot into water doesn't count. Forgiveness at
   the start of a run keeps the ritual friendly.
3. **Puttdle** — a *round* (5 holes, par 17), not a hole. Par variety
   comes free from summing holes.
4. **Puttdle** — "share your scorecard", not "share your score".
5. **Wordle** — spoiler-free emoji grid; it was a player's invention
   that the developer adopted, and it is what made the game spread.
6. **NYT Games** — the ritual is 30–90 seconds and *closes* with a card:
   result, streak, stats, share, come-back-tomorrow.
7. **NYT Games** — badges celebrate streak milestones; 5.6M Wordle
   streaks broke in 2024, and the fragility is the drama.
8. **Slay the Spire daily** — the daily's identity is its *modifiers*;
   ours fire one day in eighteen.
9. **Desert Golfing** — permanence is a feature: no undo, the score
   only grows. Our infinite retry is the opposite choice, unexamined.
10. **Cursed to Golf** (complaint) — players hate not being able to see
    where the ball is going; camera control is table stakes in golf.

### 5 things to definitely not add

Online leaderboards or accounts · IAP or rewarded ads · continuous
background music · a level editor with hosting · more course elements
before par variety lands.

---

## B. Diagnosis

### Scorecard, with evidence

| Axis | Score | Why |
|---|---|---|
| First 90 seconds | 5 | FTUE walks into 4 tutorial holes teaching shot/bumper/sand/ice; the other six elements are never taught. |
| Shot-to-shot feel | 7 | Putter swing, squash, speed-aware impacts, 3-tier haptics; `FeelConfig` is well-tuned (power exponent 1.35 for fine control). |
| Course readability | 5 | Inside and outside the walls are the same striped felt; the ball is tiny; no camera control. |
| UI hierarchy | 5 | The HUD is one text line; the bottom bar is five identical buttons. |
| Visual identity | 6 | Clean flat green, one amber accent, red flag — recognizable but not distinctive; panels are navy-black against a green game. |
| Audio character | 6 | 12 coherent synthesized clips, rising star notes; ramps silent, gates and mills reuse the wall voice, no power mapping. |
| Hole-out payoff | 8 | Ring, flash, zoom, slow-mo, staged stars with notes, confetti at 3★. |
| Failure payoff | 3 | `Play(failClip, 0.8f)` and a pop-in panel — nothing else. |
| Reason to return tomorrow | 5 | Streak + countdown exist; nothing states the next goal, and no notification is allowed. |
| Reason to share | 3 | An unreadable code with no visual. |
| Accessibility | 7 | Colorblind palette, EN/TR, sound/haptic/aim/battery toggles; no reduce-motion, no text scale. |
| Performance | 8 | 381 ms generation, async on device, 60/120 battery mode. |
| What the repo says about its author | 8 | Excellent — determinism proof, CI, TDD, honest docs. |

### The known open wound: par 2

Verified this session, not quoted from the docs:

```
dotnet run --project tools/CourseViewer -c Release -- --scan 300
→ par 2 ×300 (100%)   avg hazards 5.78   Easy 104 / Normal 106 / Hard 90
→ avg per course: bumpers 1.38 · sand 0.93 · ice 1.00 · water 0.46
                  gates 0.51 · ramps 0.45 · portals 0.60 · mills 0.46
```

Two shots is the entire decision space of a daily. The element wave did
its job — the new elements appear in roughly half of all courses — but
they decorate a two-decision puzzle. Every proposal below carries an
**After deeper pars** line for this reason.

---

## C. The field

### The direct competitors — this niche is occupied

| Game | Shape | What it does better | What we do better |
|---|---|---|---|
| **putt.day** (hole #98) | One browser hole a day, midnight PT | **First finish counts** for the leaderboard; water forgiveness on shot 1; calendar archive; party mode; Discord; "make a hole" UGC; drag/pinch camera | Proven solvability, deterministic replay codes, 100-level campaign, offline play |
| **Puttdle** (#135) | 5 holes a day, par 17, accounts | A real *round* and a real scorecard; leaderboard; challenge-a-friend; past rounds | No account required, no backend, archive is free |
| **Minigolfle** | 3 procedural levels a day | Multi-level daily, share score | (page unreachable during audit — **[Assumption]** based on its own description) |
| **wordlegolf.com** | Wordle/golf hybrid | Name occupies the obvious search term | — |

The strategic read: **our premise is no longer unique, our engineering
is.** Everything that makes PUTTSEED special (proof, determinism,
replay) is invisible in a screenshot and absent from the share text. And
all four competitors are one URL away from a player while we are an
unlisted `.aab`.

### The craft references

- **Wordle** — the share grid was invented by a player and adopted by
  the developer; it is *the* mechanism that spread the game, and it
  works because it is spoiler-free and tells a story in six rows.
- **NYT Games** — daily cadence as habit, a 30–90 second ritual, a
  closing card with stats and badges, streaks whose fragility is the
  drama (5.6M broke in 2024).
- **Slay the Spire / Spelunky dailies** — same seed for everyone,
  *modifiers* give each day an identity, and one-run permadeath is what
  makes the shared result comparable.
- **Desert Golfing** — the most-loved minimalist golf game strips away
  menus entirely and refuses undo; permanence is the design.
- **Golf Peaks** (praised: "everything just right"; complained about:
  trial-and-error) and **Cursed to Golf** (complained about: can't see
  where the ball is going, cramped levels) — the two failure modes of
  puzzle golf, both of which our tiny courses and fixed camera risk.
- **Mobile mini-golf reviews at large** — the recurring complaints are
  inconsistent physics, pay-to-win and ad load. We are structurally
  immune to all three. That is worth *saying* in the store listing.

---

## D. Feature gap analysis

**They have, we don't:** first-finish scoring · multi-hole rounds and
real par · visual scorecard share · camera zoom/pan · friend challenge ·
UGC holes · instant browser play · store presence.

**We have, they don't:** machine-proven solvability · bit-deterministic
replay codes that re-simulate · a 100-level curated campaign · offline
play with no account · a weekly gauntlet · themed days proven solvable
under their own twist · ten interacting elements · ball skins and
achievements · full EN/TR.

**Their features that genuinely add value:** first-finish scoring,
scorecard sharing, camera control, multi-hole rounds.
**Their features that are just complexity (for us):** accounts,
leaderboards, UGC hosting, party mode — each needs a server, and the
no-backend constraint is a feature, not a limitation.

**Our USP, honestly stated:** *the only daily golf game that can prove
its holes are fair and hand you a 30-character code that replays your
run exactly, anywhere, forever.* A player feels none of that today. The
proof is in the README; the replay is behind a button labeled "Watch".

**"Why play this instead of the tab already open?"** Today the honest
answer is "because it's an app and it has a campaign". After `SHARE-1`,
`DAILY-1` and `MECH-1` it becomes "because the hole is provably fair,
the result is comparable, and the run is watchable".

---

## E. Proposals

Scores are Player / Retention / UX / Visual-Feel / Portfolio impact,
then Cost and Risk (both 10 = cheap / safe).

### Meta & retention

#### [SHARE-1] The emoji scorecard
- **Problem today:** the share is `PUTTSEED day 214 — 2 strokes (par 2).
  Watch: PUTT-AQMA…` ([ModeController.cs:186](Assets/PuttSeed/Runtime/ModeController.cs:186)).
  It is unreadable, carries no streak, no attempt count, no picture of
  the run, and nothing a reader can compare against their own.
- **Proposal:** build a spoiler-free scorecard from data the sim already
  observes (`Achievements.RunFacts` tracks wall hits and hazard touches
  per run). One row per stroke, one glyph per event:
  `🟩` clean roll · `🟫` sand · `🧊` ice · `💧` water · `🔴` bumper ·
  `🌀` portal · `🌬` windmill · `⛳` holed. Header line
  `PUTTSEED #214 · 2 strokes (par 2) · 🔥12`, then the rows, then the
  `PUTT-` code on its own last line so it still imports.
- **Reference:** Wordle's grid — a player's invention that the developer
  adopted; spoiler-free, comparable at a glance, and it is what made the
  game spread. Puttdle's "share your scorecard" is the same instinct.
- **Felt when:** the share moment, and on every reader's timeline.
- **Scores:** P9 / R8 / UX7 / V6 / Port8 — **Priority 9.0**
- **Cost:** M · **Risk:** none (presentation only; the code line is unchanged)
- **Touches:** `ModeController.BuildShareText`, `Achievements.RunFacts`, `Loc`
- **Done when:** an EditMode test asserts the scorecard for a scripted
  run, and the text still round-trips through the import field.
- **After deeper pars:** strictly better — more rows, more variety.

#### [DAILY-1] First finish counts
- **Problem today:** `RecordDailyAttempt` counts retries and
  `bestStrokes` keeps the best of unlimited attempts
  ([StatsStore.cs:16](Assets/PuttSeed/Runtime/StatsStore.cs:16)); the menu
  reads "Today: 34 attempt(s) · done in 3". A result achieved on attempt
  34 is not comparable with anyone's, so the daily is a private grind.
- **Proposal:** the **first finish of the day is the official result** —
  it fills the scorecard, the streak and the share. Retries stay
  unlimited and instant (pillar intact) but are labeled *practice on
  today's hole* and feed only the personal best. Two numbers on the
  end-of-day card: "Today: 3 (first) · best 2".
- **Reference:** putt.day, verbatim: "unlimited attempts, your first
  finish counts"; Slay the Spire's daily is one run for the same reason.
- **Felt when:** the first shot of every day — it now matters.
- **Scores:** P8 / R9 / UX6 / V3 / Port6 — **Priority 8.5**
- **Cost:** M · **Risk:** save format (new `firstStrokes`/`firstReplay`
  fields; `JsonUtility` leaves absent fields at their initializers, so
  old saves load — needs a test)
- **Touches:** `StatsStore`, `ModeController`, `GameUI`, `MenuBootstrap`
- **Done when:** `StatsStoreTests` proves the first finish is immutable
  across later retries and that a pre-change save file still loads.
- **After deeper pars:** better — a first-finish round score is exactly
  what a scorecard wants.

#### [DAILY-2] The par streak
- **Problem today:** the streak counts days played
  ([StatsStore.cs:25](Assets/PuttSeed/Runtime/StatsStore.cs:25)). With
  unlimited retries it cannot break through skill, only absence, so it
  carries no tension.
- **Proposal:** keep the played-streak, add a **par streak** — days
  whose *first finish* was at or under par. That is the number worth
  bragging about, and the one that can break.
- **Reference:** NYT's streak fragility as drama (5.6M ended in 2024).
- **Felt when:** the moment the first finish resolves.
- **Scores:** P7 / R8 / UX5 / V3 / Port5 — **Priority 7.5**
- **Cost:** S · **Risk:** save format (additive) · depends on `DAILY-1`
- **Touches:** `StatsStore`, share text, menu chip
- **Done when:** an EditMode test drives 3 days and asserts the par
  streak resets on a bogey first finish.
- **After deeper pars:** better.

#### [META-1] One "next goal" line
- **Problem today:** the menu's stats chip reads "Streak 2 · Today: 34
  attempt(s) · Practice: 39" — three unrelated numbers in small grey
  text ([MenuBootstrap.cs:1054](Assets/PuttSeed/Runtime/MenuBootstrap.cs:1054)),
  none of which is a goal. 13 achievements and 10 skins exist and the
  menu never points at the nearest one.
- **Proposal:** replace that chip with the single nearest unfinished
  goal, computed from the save: "2 more levels → Coral ball" / "3 more
  three-star dailies → Perfectionist" / "5 days → Seven Days". One line,
  one accent colour, tappable to the relevant panel.
- **Reference:** NYT badges; every progression game's "next unlock" line.
- **Felt when:** every menu visit, which is every session.
- **Scores:** P6 / R8 / UX8 / V5 / Port5 — **Priority 8.0**
- **Cost:** S · **Risk:** none
- **Touches:** `MenuBootstrap`, `Achievements`, `BallSkins`, `Loc`
- **Done when:** an EditMode test maps three save states to three
  expected goal strings.
- **After deeper pars:** unchanged.

#### [UI-3] The end-of-day card
- **Problem today:** holing out shows a status line + staged stars
  ([GameUI.cs:200](Assets/PuttSeed/Runtime/GameUI.cs:200)); the streak,
  the histogram, the countdown and the share button live in three other
  places. The ritual has no close.
- **Proposal:** one card on the daily's first finish: result and stars,
  first-vs-best, par streak, the stroke histogram already built in the
  stats panel, the countdown to the next hole, and one primary Share
  button carrying `SHARE-1`. Retry stays available underneath.
- **Reference:** NYT/Wordle's post-solve modal — result, stats, streak,
  share, timer, in that order.
- **Felt when:** the last five seconds of every session — the memory the
  player leaves with.
- **Scores:** P7 / R9 / UX8 / V6 / Port6 — **Priority 8.0**
- **Cost:** M · **Risk:** scene rebuild; must not overlap the bottom bar
- **Touches:** `UiConstruction`, `GameUI`, `DailyCountdown`, `StatsStore`
- **Done when:** the card appears once per day on first finish, and the
  overlap sweep passes in EN and TR at 20:9 and 4:3.
- **After deeper pars:** better — a round scorecard belongs here.

### Core gameplay & mechanics

#### [MECH-1] Deeper pars (the root fix)
- **Problem today:** par 2 in 300/300 scanned seeds; `MaxPar: 5` is dead
  configuration; the corridor length pre-check keeps every hole two
  shots deep; `GolfTerms`' Birdie and Eagle branches are unreachable.
- **Proposal:** the project already parked in LATER.md — longer
  corridors plus a bigger solver budget, shipped as **generator v3** so
  v1 and v2 history stays frozen. Target a par distribution of roughly
  2/3/4, not a uniform 3.
- **Reference:** Puttdle's par-17 round; MacKenzie's "every hole should
  be different in character" — variety is the point of golf design.
- **Felt when:** every day, and every star the player earns.
- **Scores:** P9 / R8 / UX5 / V6 / Port7 — **Priority 7.0** (impact is
  the highest in this document; cost is what holds it back)
- **Cost:** L (multi-day: generation time, solver budget, on-device
  profiling, star-curve recalibration, new golden fixtures)
- **Risk:** determinism + generator version — the highest-risk item here,
  and the only one that needs a v3 config
- **Touches:** `GeneratorConfig`, `CourseGenerator`, `SolverConfig`,
  `Scoring`, golden fixtures, `JourneyConfig` (stays v1)
- **Done when:** a 1000-seed scan shows at least three par values with
  none above 60%, the property suite is green, and generation stays
  under ~1 s on device.
- **After deeper pars:** this *is* deeper pars.

#### [DAILY-3] Tell the player which rule they are playing under
- **Problem today:** `touchCaptureBelowHard = true`
  ([FeelConfig.cs](Assets/PuttSeed/Runtime/FeelConfig.cs)) means Easy and
  Normal courses capture on *any* touch while Hard keeps the speed
  threshold — the cup's rule silently changes from day to day, and the
  daily HUD never shows the difficulty (only Practice does,
  [GameUI.cs:165](Assets/PuttSeed/Runtime/GameUI.cs:165)).
- **Proposal:** show the day's rating in the daily HUD next to the mode
  label. Then decide deliberately: either keep the split and name it, or
  unify capture for everyone. A hidden per-day rule change is worse than
  either choice.
- **Reference:** mobile golf reviews' single loudest complaint is
  inconsistent physics; ours is consistent but *undisclosed*.
- **Felt when:** every rim-out that "should have dropped".
- **Scores:** P7 / R4 / UX8 / V3 / Port5 — **Priority 7.0**
- **Cost:** S · **Risk:** none (label only; unifying capture would be
  determinism-relevant and is a separate decision)
- **Touches:** `GameUI.Refresh`, `Loc`
- **Done when:** the daily HUD reads `Daily · Hard` and the string is
  localized.
- **After deeper pars:** unchanged.

#### [MECH-2] Near-miss recognition
- **Problem today:** the lip-out — golf's best emotional beat — either
  cannot happen (touch capture on Easy/Normal) or happens with no
  acknowledgement at all; `FeedbackController` has no rim event.
- **Proposal:** a render-only near-miss: when the ball passes within
  ~2 cup radii without capturing, play a short wood-rim tick, pulse the
  cup ring, and tighten the camera a touch. No rule change, no
  determinism exposure — it reads the sim, it does not alter it.
- **Reference:** Peggle's escalation on near-misses; every golf
  broadcast's lip-out.
- **Felt when:** several times per session.
- **Scores:** P7 / R5 / UX5 / V8 / Port4 — **Priority 7.0**
- **Cost:** S · **Risk:** none
- **Touches:** `FeedbackController`, `SfxSynth` (one new clip)
- **Done when:** a scripted near-miss shot triggers exactly one event
  and a capture triggers none.
- **After deeper pars:** better — more shots per hole, more near misses.

#### [MECH-3] Water forgiveness on the opening shot
- **Problem today:** water costs a stroke and a reset; on a par-2 hole
  with a 5-stroke limit, an opening shot into water spends 40% of the
  day's budget before the player has read the course.
- **Proposal:** the first shot of a daily attempt that finds water
  replays without cost, announced by a toast. This is a *rule*, so it
  belongs in core behind a config flag and needs its own solver-neutral
  proof (it can only ever help the player, so solvability is unaffected).
- **Reference:** putt.day: "a first shot that finds water doesn't count".
- **Felt when:** the worst opening of the day.
- **Scores:** P6 / R5 / UX6 / V2 / Port3 — **Priority 5.5**
- **Cost:** M · **Risk:** determinism (a core rule; needs a version
  flag and replay-codec thought — hold until after `MECH-1`)
- **Touches:** `GolfSim`, `SimConfig`, replay tests
- **Done when:** core tests cover both the forgiven first shot and an
  unforgiven later one, and old replays still decode.
- **After deeper pars:** less needed — a 4-stroke budget absorbs one
  bad opening.

### Camera, visual & readability

#### [CAM-1] Rotate wide courses into the phone
- **Problem today:** `CameraFramer.Frame` divides the horizontal half-
  extent by the aspect ([CameraFramer.cs:34](Assets/PuttSeed/Runtime/CameraFramer.cs:34));
  at 1170×2532 (aspect 0.46) a wide course sets the ortho size from
  width and leaves the top and bottom thirds of the screen empty —
  visible in `docs/media/daily-hole.png`, where the playfield occupies
  roughly a third of the display.
- **Proposal:** when a course's bounds are wider than tall, rotate the
  **camera** 90° about Z. The course fills the long axis of the phone;
  the ball roughly doubles in apparent size at no cost. Determinism is
  untouched — the drag vector is converted through the camera before
  quantization, so the same finger gesture yields the same quantized
  angle it always did, and replays are world-space.
- **Reference:** every portrait mobile golf game orients the hole along
  the long axis; Cursed to Golf's loudest complaint is not seeing where
  the ball goes.
- **Felt when:** every single shot.
- **Scores:** P8 / R5 / UX9 / V9 / Port6 — **Priority 8.5**
- **Cost:** M · **Risk:** input mapping — needs an `InputQuantizerTests`
  case proving a rotated drag quantizes identically
- **Touches:** `CameraFramer`, `DragAimController`, `InputQuantizer` tests
- **Done when:** a wide course fills ≥70% of screen height, and the
  quantization test passes at both orientations.
- **After deeper pars:** more important — longer holes are wider.

#### [VIS-1] The rough: separate in-play from out-of-play
- **Problem today:** `CourseRenderer` draws mowed stripes across the
  course bounds plus a margin ([CourseRenderer.cs:56](Assets/PuttSeed/Runtime/CourseRenderer.cs:56)),
  so the felt inside the walls and the felt outside are the same
  colour. The hole reads as line-art floating on wallpaper rather than
  as a green with an edge.
- **Proposal:** darken and desaturate everything outside the wall loop
  (a "rough" tone ~12% darker), keep the stripes inside only, and give
  the wall chain a soft outer shadow so the play area lifts off the
  background.
- **Reference:** every real mini-golf course photo; flat-design games
  read depth through value separation, not texture.
- **Felt when:** the first half-second of every hole.
- **Scores:** P5 / R3 / UX8 / V9 / Port7 — **Priority 7.5**
- **Cost:** M (may need the interior polygon; a stencil mask off the
  wall loop is the cheap route) · **Risk:** none
- **Touches:** `CourseRenderer`, `MeshFactory`, `PaletteMaterials`
- **Done when:** a screenshot at 20:9 shows an unmistakable green edge,
  and the colorblind palette still separates the tones.
- **After deeper pars:** unchanged.

#### [VIS-2] The ball wins the contrast fight
- **Problem today:** the ball is the smallest object on screen and, with
  the Rose skin equipped, sits in the same hue family as the red
  bumpers (`docs/media/daily-hole.png`). It also competes with the ready
  halo for attention.
- **Proposal:** a permanent 1-px dark rim and a tightened drop shadow on
  the ball at all times, plus a rule in `BallSkins` that no skin may
  share the bumper hue band; re-tint Rose or Coral if they collide.
- **Reference:** Desert Golfing's single white ball on sand — maximum
  value contrast, always.
- **Felt when:** every frame the ball is moving.
- **Scores:** P6 / R3 / UX8 / V7 / Port4 — **Priority 7.0**
- **Cost:** S · **Risk:** none
- **Touches:** `BallView`, `BallSkins`, `PaletteMaterials`
- **Done when:** the ball is identifiable at a glance on a 400 px-wide
  screenshot in both palettes.
- **After deeper pars:** unchanged.

#### [MENU-1] Today's hole on the menu
- **Problem today:** the menu is a title, an emblem and a stack of dark
  buttons with a large empty band (`docs/media/menu.png`). Nothing on it
  is *today-specific* except a date string.
- **Proposal:** render today's course as a small silhouette thumbnail
  inside the daily button's card — the generator already runs in 381 ms
  on a background thread. Every day the menu looks different, and the
  hole's shape becomes the day's identity ("the S-curve day").
- **Reference:** NYT's per-puzzle art; putt.day showing the hole
  immediately with no menu at all.
- **Felt when:** every launch.
- **Scores:** P7 / R7 / UX6 / V9 / Port8 — **Priority 7.5**
- **Cost:** M · **Risk:** none (async generation already exists)
- **Touches:** `MenuBootstrap`, `CourseRenderer` (a thumbnail path)
- **Done when:** the menu shows the day's shape within ~1 s of launch
  and never blocks the UI thread.
- **After deeper pars:** better — more distinctive shapes.

#### [VIS-3] One visual family for panels
- **Problem today:** the game is felt green; every panel is navy-black
  (`docs/media/journey.png`, `collection.png`). Two visual identities in
  one product.
- **Proposal:** retint panel backgrounds to a dark felt-green with the
  amber accent kept; keep the contrast ratio at or above today's.
- **Reference:** basic art direction consistency.
- **Felt when:** every panel open.
- **Scores:** P3 / R2 / UX5 / V8 / Port6 — **Priority 6.0**
- **Cost:** S · **Risk:** scene rebuild
- **Touches:** `PaletteMaterials`, `UIFactory`
- **Done when:** menu and panels share one palette, contrast verified.
- **After deeper pars:** unchanged.

#### [VIS-4] Locked content that advertises itself
- **Problem today:** locked Journey cells are near-black on black and
  locked skins are dark circles (`docs/media/journey.png`,
  `collection.png`); the unlock condition appears only after a tap
  ([MenuBootstrap.cs:731](Assets/PuttSeed/Runtime/MenuBootstrap.cs:731)).
  Locked content that cannot be seen cannot pull.
- **Proposal:** locked skins show the real colour at 35% with a dim
  ring, and the unlock line renders under the grid permanently for the
  *nearest* locked item. Locked Journey cells keep their number legible.
  (Note: a padlock icon was tried and rejected — this proposes value
  contrast, not iconography.)
- **Felt when:** every Collection visit.
- **Scores:** P6 / R7 / UX7 / V6 / Port4 — **Priority 7.0**
- **Cost:** S · **Risk:** none
- **Touches:** `MenuBootstrap`, `BallSkins`
- **Done when:** every locked cell is legible at arm's length and states
  its condition without a tap.
- **After deeper pars:** unchanged.

### UI/UX

#### [UI-1] HUD hierarchy
- **Problem today:** one string carries everything —
  `"{mode}   Strokes {n}/{limit}   Par {p}   Streak {s}"`
  ([GameUI.cs:184](Assets/PuttSeed/Runtime/GameUI.cs:184)) — all at one
  size and weight, so the mode name shouts as loudly as the score.
- **Proposal:** golf's own hierarchy: the stroke count as a large
  numeral, a score-to-par chip beside it (`E`, `+1`, `+2`) which is what
  a golfer actually reads, then mode and streak as small secondary
  chips. Same information, three tiers.
- **Reference:** any golf scorecard; NYT's puzzle headers.
- **Felt when:** continuously.
- **Scores:** P6 / R4 / UX9 / V7 / Port5 — **Priority 7.5**
- **Cost:** M · **Risk:** scene rebuild; watch overlap in TR
- **Touches:** `UiConstruction`, `GameUI`
- **Done when:** the stroke number is the largest element in the bar and
  nothing overlaps in EN/TR at 20:9 and 4:3.
- **After deeper pars:** better — the to-par chip finally varies.

#### [UI-2] The bottom bar earns its space
- **Problem today:** five equal-weight buttons — Menu, Retry, Share,
  Ghost, Watch ([GameUI.cs:255](Assets/PuttSeed/Runtime/GameUI.cs:255)) —
  where Retry is pressed dozens of times a day and Watch is for pasting
  a friend's code. Two of five slots serve advanced users.
- **Proposal:** Retry becomes the wide primary; Menu stays small; Share
  appears only after the hole is finished (it already refuses to work
  before: "Finish the hole to share your run"); Ghost and Watch move
  into a single "⋯" overflow.
- **Reference:** thumb-zone convention; Desert Golfing ships no buttons
  at all.
- **Felt when:** every retry.
- **Scores:** P6 / R4 / UX9 / V5 / Port4 — **Priority 7.5**
- **Cost:** M · **Risk:** scene rebuild
- **Touches:** `GameUI.LayoutBottomBar`, `UiConstruction`
- **Done when:** the reflow test covers 4/5/6-button states with no
  overlap.
- **After deeper pars:** unchanged.

#### [TEACH-1] Teach the six elements nobody teaches
- **Problem today:** the tutorial is four holes for shot, bumper, sand
  and ice (`TutorialConfig`, GDD FTUE). Gates, ramps, portals, windmills,
  water and themed days ship untaught, and the trajectory preview is
  restricted to Tutorial and Easy practice
  ([ModeController.cs:165](Assets/PuttSeed/Runtime/ModeController.cs:165)).
- **Proposal:** don't grow the tutorial — use the hint chip that already
  exists (`GameUI.hintChip`). The first time a player meets an element
  in any mode, one line appears for a few seconds: "one-way gate — it
  only opens this way". Fire once per element, persisted in the save.
- **Reference:** Golf Peaks' reviews praise clarity; its complaint is
  trial-and-error — exactly what an untaught portal produces.
- **Felt when:** the first encounter with each of six elements.
- **Scores:** P8 / R6 / UX9 / V4 / Port5 — **Priority 8.0**
- **Cost:** M · **Risk:** save format (a small seen-elements set)
- **Touches:** `ModeController`, `GameUI`, `StatsStore`, `Loc` (12 strings)
- **Done when:** each element's hint fires exactly once, ever, per save.
- **After deeper pars:** unchanged.

#### [ACC-1] Reduce motion
- **Problem today:** slow-mo, letterbox, camera zoom, shake and confetti
  are unconditional (`FeedbackController`, `CameraJuice`); settings
  cover sound, haptics, aim style, colorblind and battery only.
- **Proposal:** one "reduced motion" switch that disables shake, slow-mo,
  letterbox and confetti while keeping every informational effect
  (splash, puff, star reveal). Ships in the existing settings pop-up.
- **Reference:** platform accessibility guidance; motion sensitivity is
  the most common accessibility need in juice-heavy games.
- **Felt when:** by the players who need it, every session.
- **Scores:** P4 / R3 / UX7 / V3 / Port6 — **Priority 6.5**
- **Cost:** S · **Risk:** save format (additive bool)
- **Touches:** `StatsStore`, settings panel, `FeedbackController`, `CameraJuice`
- **Done when:** the switch suppresses all four effects and nothing else.
- **After deeper pars:** unchanged.

### Audio, animation & VFX

#### [VFX-1] Failure gets a body
- **Problem today:** running out of strokes plays one clip
  ([FeedbackController.cs:~430](Assets/PuttSeed/Runtime/FeedbackController.cs))
  and pops a panel. Compare hole-out: ring, flash, zoom, slow-mo, staged
  stars, notes, confetti. Failure happens nearly as often.
- **Proposal:** a short desaturation of the felt, the flag drooping for
  a beat, a heavy haptic thump, and the fail clip pitched down a tone.
  Under a second, skippable by tapping Retry.
- **Reference:** Vlambeer's screenshake principle applies to losing too;
  an unacknowledged failure reads as a bug.
- **Felt when:** every failed run.
- **Scores:** P7 / R5 / UX5 / V8 / Port4 — **Priority 7.5**
- **Cost:** S · **Risk:** none
- **Touches:** `FeedbackController`, `FlagView`, `PaletteMaterials`
- **Done when:** failure is unmistakable with the sound off.
- **After deeper pars:** unchanged.

#### [AUD-1] The silent element
- **Problem today:** `grep -c Ramp FeedbackController.cs` → **0**. Ramps
  render a chevron ([CourseRenderer.cs:156](Assets/PuttSeed/Runtime/CourseRenderer.cs:156))
  and are the only one of ten elements with no sound, no particle and no
  haptic. Gates and windmills borrow the wall voice.
- **Proposal:** one new synthesized clip for ramp entry (a rising
  filtered noise sweep, 120 ms), one for gate pass-through (a short
  wooden click, distinct from a wall hit), and a low whoosh for a
  windmill blade near-miss. Three clips in `SfxSynth`.
- **Reference:** event-only mobile audio practice — each interaction has
  one voice, and loops are unnecessary.
- **Felt when:** roughly half of all v2 courses (ramps appear in 0.45
  courses per seed).
- **Scores:** P6 / R3 / UX6 / V7 / Port5 — **Priority 7.0**
- **Cost:** S · **Risk:** none (SfxSynth is an editor tool; Unity must
  be closed to run it)
- **Touches:** `Editor/SfxSynth.cs`, `FeedbackController`
- **Done when:** every element in the golden fixture produces a distinct
  sound during a manual pass.
- **After deeper pars:** unchanged.

#### [AUD-2] The shot sounds like the shot
- **Problem today:** `Play(shotClip, 1f)` fires at a fixed pitch for a
  10% tap and a 100% smash
  ([FeedbackController.cs:268](Assets/PuttSeed/Runtime/FeedbackController.cs:268)).
- **Proposal:** map pitch to the quantized power index (roughly ±3
  semitones across the range) and impact volume to speed, which the
  bounce path already does. One line for the shot, and it makes every
  putt feel authored.
- **Reference:** pitch ladders are how event-only mobile audio conveys
  intensity without loops; the game already does this for stars.
- **Felt when:** every shot.
- **Scores:** P6 / R3 / UX5 / V8 / Port4 — **Priority 7.5**
- **Cost:** S · **Risk:** none
- **Touches:** `FeedbackController.OnShotFired`
- **Done when:** minimum and maximum power are audibly different.
- **After deeper pars:** unchanged.

#### [ANIM-1] Celebrations stay short at speed
- **Problem today:** capture runs a 0.35 s zoom-in, 0.9 s hold, 0.35 s
  zoom-out ([CameraJuice.cs:12](Assets/PuttSeed/Runtime/CameraJuice.cs:12))
  plus slow-mo and a staged star reveal (0.5 s lead + 0.16 s steps).
  That is fine on the day's first finish and long on the thirty-fourth
  attempt.
- **Proposal:** full celebration on the first finish of a course; on
  subsequent retries of the same course, halve the hold and skip the
  confetti. Any tap skips to the end.
- **Reference:** restart-heavy arcade design — the celebration must not
  tax the "one more try" pillar.
- **Felt when:** from the third attempt onward, which is most attempts.
- **Scores:** P6 / R5 / UX8 / V4 / Port3 — **Priority 7.0**
- **Cost:** S · **Risk:** none
- **Touches:** `FeedbackController`, `GameUI`
- **Done when:** a repeat capture resolves in under a second and a tap
  always skips.
- **After deeper pars:** unchanged.

#### [ANIM-2] The aim line tells the truth about power
- **Problem today:** the trajectory preview is limited to Tutorial and
  Easy practice ([ModeController.cs:165](Assets/PuttSeed/Runtime/ModeController.cs:165)) —
  correct, it should not solve the daily. But the remaining feedback is
  a dashed arrow whose only power cue is length, and the power curve is
  non-linear (`powerCurveExponent 1.35`), so the mapping between finger
  distance and outcome is unstated.
- **Proposal:** keep the preview restricted; add a discrete power ladder
  on the aim line — small ticks at 25/50/75/100% — so a player can
  repeat a shot deliberately. Repeatability is what turns a retry into
  learning.
- **Reference:** Golf Clash-style power meters; Golf Peaks' complaint
  about trial-and-error is what happens without a repeatable input.
- **Felt when:** every aim, especially on retries.
- **Scores:** P7 / R6 / UX8 / V5 / Port4 — **Priority 7.5**
- **Cost:** S · **Risk:** none (render-only; quantization unchanged)
- **Touches:** `DragAimController`
- **Done when:** the same tick position reproduces the same quantized
  power index in a test.
- **After deeper pars:** better — more shots to repeat.

### Presentation & distribution

#### [PRES-1] WebGL — the category's actual front door
- **Problem today:** every direct competitor is an instant-play web page
  (putt.day, Puttdle, Minigolfle, wordlegolf); PUTTSEED is an unlisted
  Android `.aab` (ROADMAP: the Play Console upload is still store-side)
  and the WebGL demo is listed as "still open".
- **Proposal:** ship a WebGL build of the daily (daily + archive is
  enough; Journey can stay app-only), on a domain, with the `PUTT-`
  import field visible. It is also the fastest way for a reviewer to try
  the thing the README describes.
- **Reference:** the entire competitive set; PC Gamer covered a browser
  one, not an app one.
- **Felt when:** discovery — the moment that currently never happens.
- **Scores:** P5 / R6 / UX5 / V4 / Port10 — **Priority 8.0**
- **Cost:** L (Unity WebGL: input, audio unlock, file size, hosting)
- **Risk:** none to the game; WebGL determinism must be verified — run
  the golden hash test in the browser build, it is the perfect proof
- **Touches:** build scripts, a WebGL entry scene
- **Done when:** the golden hash matches in-browser and a `PUTT-` code
  produced on Android replays identically on the web build.
- **After deeper pars:** unchanged.

#### [PRES-2] The hero GIF and the store listing
- **Problem today:** the README shows four static screenshots; there is
  no GIF, no trailer, no feature graphic, and no store listing. A daily
  game's pitch is motion — the ball, the bank, the drop.
- **Proposal:** a 6–8 second loop: aim, bank off two walls, drop, star
  reveal. Same clip becomes the store's first asset. Capture with
  ScreenToGif (no ffmpeg on this machine). Then a 5-screenshot store
  set, each with one caption: proven-fair holes · ten elements · a
  hundred levels · shareable replays · no ads, no accounts.
- **Reference:** the recurring complaints about competing mobile golf
  games are ads and pay-to-win; "no ads, no accounts, no IAP" is a
  genuine differentiator worth putting on the first screenshot.
- **Felt when:** first impression, forever.
- **Scores:** P4 / R3 / UX3 / V7 / Port9 — **Priority 7.0**
- **Cost:** M · **Risk:** none
- **Touches:** `docs/media`, README, store assets
- **Done when:** the README's first screen contains a moving image.
- **After deeper pars:** unchanged.

#### [PRES-3] Say the proof out loud, in the game
- **Problem today:** the game's one true differentiator — proven-solvable
  holes and exactly reproducible replays — appears only in the README.
  A player never learns it.
- **Proposal:** one line under the daily button: "hole #214 · proven
  solvable in 2". And on the end-of-day card: "this run is stored in 30
  characters". Two strings, and they turn the engineering into a feature
  the player can feel.
- **Reference:** competitors cannot claim it; unclaimed advantages are
  not advantages.
- **Felt when:** every launch, subliminally.
- **Scores:** P5 / R4 / UX4 / V3 / Port8 — **Priority 6.5**
- **Cost:** S · **Risk:** none
- **Touches:** `MenuBootstrap`, `Loc`
- **Done when:** both lines are localized and truthful.
- **After deeper pars:** better — "proven solvable in 3" varies.

### Performance

#### [PERF-1] Measure before optimizing
- **Problem today:** no numbers exist on device. Generation is 381 ms on
  a desktop; the battery mode toggles 60/120 FPS; nothing else is
  measured. Every proposal above adds draw calls or particles.
- **Proposal:** one profiling session on the target device recording
  frame time during a capture celebration (the heaviest moment),
  generation time for v2 seeds, and memory after 20 courses. Record the
  numbers in STATUS.md so future feel work has a baseline.
- **Felt when:** by every later decision.
- **Scores:** P2 / R2 / UX3 / V2 / Port6 — **Priority 5.5**
- **Cost:** S · **Risk:** none
- **Done when:** three numbers exist in STATUS.md.
- **After deeper pars:** essential — deeper pars raise generation cost.

---

## F. Priorities

**P0 — must have**
`SHARE-1` emoji scorecard · `DAILY-1` first finish counts · `CAM-1`
rotate wide courses · `PRES-1` WebGL

**P1 — high**
`META-1` next-goal line · `UI-3` end-of-day card · `TEACH-1` element
hints · `UI-1` HUD hierarchy · `UI-2` bottom bar · `VIS-1` the rough ·
`DAILY-2` par streak · `DAILY-3` show difficulty · `VFX-1` failure body ·
`AUD-2` power pitch · `ANIM-2` power ladder · `MENU-1` hole thumbnail

**P2 — medium**
`MECH-1` deeper pars (highest impact, largest cost — start it once the
P0 list has landed) · `VIS-2` ball contrast · `VIS-4` locked content ·
`AUD-1` ramp and gate voices · `ANIM-1` shorter repeat celebrations ·
`MECH-2` near-miss · `PRES-2` hero GIF and store · `ACC-1` reduce motion

**P3 — nice to have**
`VIS-3` panel retint · `PRES-3` proof lines · `MECH-3` water
forgiveness · `PERF-1` baseline profiling

---

## G. Quick wins and big bets

**Quick wins (an evening or less, high impact):** `META-1`, `DAILY-3`,
`VFX-1`, `AUD-1`, `AUD-2`, `ANIM-2`, `VIS-2`, `VIS-4`, `ACC-1`,
`ANIM-1`, `PRES-3`.

**Big bets:**
- **`MECH-1` deeper pars** — pays off if a 1000-seed scan yields a real
  par distribution without pushing generation past ~1 s on device. It
  unlocks Birdie/Eagle, makes stars meaningful again, and makes the
  scorecard worth reading. This is the game's ceiling.
- **`PRES-1` WebGL** — pays off if the daily is playable in a browser in
  under 5 s on a mid-range phone. It is the only proposal that changes
  the audience rather than the product.
- **The daily round (3 holes, one score)** — *considered and not
  recommended now.* The gauntlet machinery already derives N seeds from
  a period index, so a 3-hole daily is mostly wiring, and it would buy
  par variety immediately. But it rewrites the product's headline ("one
  mini-golf hole per day"), the archive's semantics and the streak's
  meaning. Do `MECH-1` first; revisit only if deeper pars prove
  impossible within the solver budget.

---

## H. Cut list

1. **Ghost and Watch leave the primary bar** (`UI-2`) — 40% of the most
   valuable row serves the smallest use case.
2. **The menu's three-number stats chip** — replaced by `META-1`'s single
   goal line. "Today: 34 attempt(s)" in particular shames the player for
   the exact behaviour the game encourages.
3. **`GolfTerms`' Eagle and Birdie branches** — unreachable while par is
   2. Either land `MECH-1` or delete them; shipping dead vocabulary is
   the tell that the par problem was never confronted.
4. **The "Par 2" label in the HUD** — redundant once `UI-1`'s to-par chip
   exists; two numbers saying one thing.
5. **The Easy/Normal vs Hard capture split** — pick one rule
   (`DAILY-3`). A cup that behaves differently on Tuesday than on
   Wednesday, unannounced, is a bug the player will blame on physics.

---

## I. Do not build

- **Online leaderboards, accounts, cloud save.** Puttdle has them and
  needs a server for it; our no-backend property is what makes replay
  codes elegant. Adding a server would cost the project its spine.
- **UGC hole submission** (putt.day has it). Without hosting it is just
  invite codes, which already exist.
- **More course elements before `MECH-1`.** Ten elements decorating a
  two-shot puzzle is already past the point of diminishing returns; the
  next element makes courses busier, not deeper.
- **Continuous music or ambience.** Tried twice, removed twice
  (`b012776`, `23cd2b2`). Event audio is the house style.
- **Ads or IAP in any form.** See Part J.
- **A second currency, XP levels, or daily missions.** The game's meta
  is already three systems deep (achievements, skins, Journey stars);
  a fourth would be feature bloat with no new player fantasy.
- **Multiplayer or real-time duels.** Ghosts already deliver asynchronous
  competition at zero infrastructure cost.

---

## J. Monetization and non-goals worth revisiting

**Monetization — recommendation: stay free.** The game is MIT-licensed,
portfolio-first, has no server costs and a 20-second session. Rewarded
ads would poison the one thing the product sells (a clean daily ritual)
and the competing mobile golf games' reviews are dominated by complaints
about exactly that. IAP is a permanent GDD non-goal and nothing in this
audit argues against it. If revenue ever becomes a goal, the least
damaging order is: a one-time "support the developer" tip that unlocks
nothing → a paid cosmetic pack that unlocks nothing gameplay-related →
never ads. The honest cost of staying free is zero revenue and no
marketing budget, which is consistent with the project's stated purpose.

**The one non-goal worth revisiting: a local daily reminder.** Push
notifications are on the non-goal list, and remote push would indeed
need a backend. But a **local** notification does not: Android can fire
an alarm at a user-chosen time with no server, no account and no data
leaving the device. For a daily-ritual game this is the single largest
D1 retention lever in mobile, and the no-backend principle survives
intact. Recommendation: opt-in, off by default, one line in settings,
one string. Cost S–M. Everything else on the non-goal list should stay
where it is.

---

## K. Roadmap

**Update 1 — Polish** (`VFX-1`, `AUD-1`, `AUD-2`, `ANIM-1`, `ANIM-2`,
`VIS-2`, `DAILY-3`) — the build feels better with no new systems and no
save changes. Complexity: low. Effect: every shot and every failure
lands.

**Update 2 — The daily becomes a daily** (`DAILY-1`, `SHARE-1`,
`DAILY-2`, `UI-3`) — stakes, an artifact, a streak that can break, and a
ritual that closes. Complexity: medium; one additive save migration.
Effect: the largest retention change available without touching core.

**Update 3 — Readability and screen** (`CAM-1`, `UI-1`, `UI-2`, `VIS-1`,
`MENU-1`, `VIS-4`) — the game stops wasting two-thirds of the display
and starts looking authored. Complexity: medium; scene rebuilds.

**Update 4 — Content depth** (`MECH-1` deeper pars as generator v3, then
`TEACH-1`, `MECH-2`, `MECH-3`) — the ceiling raise. Complexity: high;
new golden fixtures; v1/v2 stay frozen.

**Update 5 — Reach** (`PRES-1` WebGL, `PRES-2` hero GIF and store set,
`PRES-3` proof lines) — the first update aimed at people who have never
heard of the game.

**Update 6 — Backend-free live ops** — themed days move from 1-in-18 to
a named weekly rotation (Ice Week, Windy Weekend), the gauntlet gets a
seasonal badge, and the archive calendar marks event days. All derived
from the seed, all offline, no server. Complexity: low once `MECH-1`
lands.

### Three months, one developer, evenings

**Month 1 — feel and the daily.** Update 1 in the first two weeks
(quick wins, no save risk), then Update 2. Ship `SHARE-1` before
anything else in the month: it is the highest impact-per-hour item in
this document and it makes every later change visible to other people.

**Month 2 — the screen and the ceiling.** Update 3 in the first half
(scene-rebuild-heavy, best done in one stretch), then begin `MECH-1`.
Budget the whole second half for deeper pars: a scan, a solver-budget
experiment, a device profile, then the v3 config. Do not start it before
`PERF-1` gives a baseline.

**Month 3 — finish and show.** Finish `MECH-1` (star curve, golden
fixtures, Journey untouched on v1), then Update 5. End the quarter with
a WebGL link, a hero GIF, a store listing and a daily that is worth
sharing — which is the shortest description of what this game is
missing today.

---

## L. Open questions

1. **Is "one hole a day" load-bearing identity, or a starting point?**
   It decides whether the 3-hole daily round stays rejected.
2. **How attached are you to unlimited-retry best-of scoring?**
   `DAILY-1` is the highest-leverage change in this audit and it is the
   one that changes what your existing save means.
3. **Should the cup's capture rule be unified**, or stay
   difficulty-dependent and merely disclosed (`DAILY-3`)?
4. **Is the local daily reminder acceptable** under the spirit of the
   no-notifications non-goal (Part J)?
5. **Does WebGL count as "shipping the game"**, or is Play Store first
   still the plan? The competitive read says the browser is where this
   category actually lives.

---

*Assumptions are marked **[Assumption]** in place. Minigolfle's page
returned 403 during this audit; its description is taken from search
results rather than the source. Everything else was verified against
this repo, its screenshots, a live 300-seed generator scan, or a cited
external source.*

Sources consulted: [PC Gamer on the daily minigolf
game](https://www.pcgamer.com/games/sports/my-new-daily-game-is-like-wordle-but-for-minigolf/) ·
[putt.day](https://putt.day/) · [Puttdle](https://puttdle.com/) ·
[Minigolfle](https://minigolfle.com/) · [Josh Wardle on the share
grid](https://x.com/powerlanguish/status/1471493886031773707) ·
[Slate on why Wordle went
viral](https://slate.com/culture/2022/01/wordle-game-creator-wardle-twitter-scores-strategy-stats.html) ·
[Wikipedia: Desert
Golfing](https://en.wikipedia.org/wiki/Desert_Golfing) · [Slay the Spire
Daily Climb](https://slaythespire-archive.fandom.com/wiki/Daily_Climb) ·
[AOL: 5.6M Wordle streaks ended in
2024](https://www.aol.com/exclusive-wordle-puzzle-ended-5-013624721.html) ·
[Golf Peaks review](https://www.destructoid.com/reviews/review-golf-peaks/) ·
[Cursed to Golf review](https://www.thatvideogameblog.com/review-cursed-to-golf-pc/) ·
[LINKS Magazine on variety in course
design](https://linksmagazine.com/the-value-of-variety-in-golf-course-design/)
