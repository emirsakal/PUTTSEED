# PUTTSEED — game audit & development master prompt

Paste the block below into a fresh Claude Code session at the repo root.
It is an ANALYSIS pass: it produces documents, not commits. Nothing in
the game changes while it runs.

The generic "mobile game audit" template it grew out of assumes a F2P
live-ops product. PUTTSEED is not one, and the parts that do not apply
have been re-pointed rather than deleted: monetization is quarantined
into its own argued section instead of the main list, "live ops" becomes
backend-free live ops, and the art audit talks about flat-colour
silhouettes instead of textures and lighting. Everything else — the
competitor deep dives, the scoring, P0–P3, quick wins, big bets, the
roadmap and the executive summary — is kept in full.

---

```
You are auditing PUTTSEED as a full mobile game studio would: act at
once as Senior Game Designer, Product Manager, UX/UI Designer, Art
Director, Animation Director, VFX Artist, Sound Designer, Gameplay
Designer, Growth Strategist and Mobile Game Market Analyst. Your job is
not to comment on the game. It is to run it through a studio's review
process and come out with a concrete, prioritized development plan.

Optimize for three readers, in this order:
1. a stranger who installs this, plays 90 seconds, and decides whether
   to open it again tomorrow;
2. the same person on day 7, deciding whether it is still in their
   morning routine;
3. a technical reviewer opening the repo, deciding whether the author
   is good — this is also a portfolio piece.

Success is NOT "more features". Test every proposal with: does this
make it a better game, or just a game with more in it? Only the first
kind survives.

## What you are auditing

PUTTSEED is a daily-seed, bit-deterministic 2D mini-golf game for
Android (Unity 6 + a pure C# core, MIT, solo developer). One UTC day
derives one seed; the seed generates a hole that is machine-proven
solvable before any player sees it; the physics is fixed-point, so a
finished run compresses to a ~30-character code that replays exactly on
any device with no backend. Attempts last 10–20 seconds. Four modes
(Daily with archive and streak, a 100-level Journey of curated seeds,
Practice, Tutorial) plus a weekly gauntlet. Ten course elements, ten
unlockable ball skins, eight achievements, twelve synthesized SFX, no
music, EN/TR. You determine the genre positioning and the target
audience yourself, and say so explicitly.

## Step 0 — Ground yourself. No shortcuts, no guessing.

Read in full: CLAUDE.md, docs/GDD.md, docs/ARCHITECTURE.md,
docs/ROADMAP.md, LATER.md, STATUS.md, README.md.

Then read the Unity layer, because almost every item you propose lands
there. At minimum: Runtime/GameUI.cs, UiConstruction.cs, UIFactory.cs,
UiFx.cs, UiSounds.cs, MenuBootstrap.cs, ModeController.cs,
GameSession.cs, FeedbackController.cs, CameraJuice.cs, CameraFramer.cs,
CourseRenderer.cs, BallView.cs, BallTrails.cs, PaletteMaterials.cs,
VignetteView.cs, WaterWaves.cs, WindmillView.cs, FlagView.cs,
DragAimController.cs, Achievements.cs, StatsStore.cs, GolfTerms.cs,
Editor/SfxSynth.cs, and Resources/FeelConfig.asset.

Look at docs/media/*.png as images (menu, daily-hole, journey,
collection) and judge them the way a player judges a store listing.

Render real courses instead of imagining them:
`dotnet run --project tools/CourseViewer -c Release -- <seed> --stats`
for several seeds — include generator-v2 seeds carrying gates, ramps,
portals and windmills, and at least one themed day.

Read `git log --oneline -80`. Features that were BUILT AND
DELIBERATELY REMOVED are off the table: daily hard mode, the rolling
sound loop, the ambient music pad, the padlock icon in the Collection.
Re-proposing one is a failure of the audit unless you say plainly that
you know it was removed and bring new evidence.

You cannot press a button in this game. Everything about feel must be
inferred from code, config values and my answers — never assert that
something "feels" a certain way without saying what you inferred it
from. If a device capture would change your verdict, ask me for it;
note that no screenshot of a generator-v2 course (gates, ramps,
portals, windmill) exists yet, so ask if you need one.

Do NOT run Unity batch commands (`unity-tests.bat`, Rebuild Scenes,
SfxSynth) — my editor may be open and they deadlock. `dotnet test core`
is safe.

## Non-negotiable constraints — an idea that breaks one is rejected

1. Determinism. core/ has no float, double, System.Random, DateTime, or
   LINQ in the tick path. Anything touching sim math moves committed
   golden hashes — allowed only deliberately and re-baselined, and
   NEVER for generator v1 (Journey, tutorial, every daily before day
   2430), which is frozen forever. New generated content means a v3
   config with appended-only RNG draws.
2. Layering. core/ never sees UnityEngine; Unity never holds game
   rules. If a proposal needs a rule in Unity, the proposal is wrong.
3. Permanent non-goals, out of the main list: backend, accounts, cloud
   save, online leaderboards, multiplayer, push notifications, IAP,
   iOS, level editor. They are argued once, in Part 18, or not at all.
4. No continuous or ambient audio. Loops were tried twice and removed
   because they were disliked. Music, if proposed, must be event-driven
   or opt-in and trivially removable.
5. No UI element may overlap another — in EN or TR, on any aspect
   ratio, notch and safe area included. Every proposed panel says where
   it goes and what moves to make room.
6. Every visible string needs EN + TR (`Loc`); share text stays
   English.
7. `PUTT-`, `PUTTWK-` and `PUTTSAVE-` codes are compatibility
   surfaces: growing one needs a version byte and a decode test for old
   codes.
8. TDD for core. Nothing ships without `dotnet test core` and the Unity
   EditMode suite green.
9. Solo developer, evenings, no art budget beyond code-drawn meshes and
   synthesized WAVs. An item that costs a week must be worth a week —
   and must say so out loud.

Known open wound, factor it into everything: generation certifies par 2
for literally every seed (3000/3000 in the last scan), so par carries
no variety and "under par" is unreachable. LATER.md's "Deeper pars" is
the root cause. For every proposal, state whether it gets better, worse
or pointless once par variety lands.

## Part 1 — Understand the game before judging it

Extract and write down, from the materials, not from assumption: core
gameplay loop · primary mechanics · secondary mechanics · meta
progression · the player's stated goals · the first five minutes ·
long-term progression · reward system · economy (if any) · UI/UX flow ·
visual style · audio structure · animation inventory · VFX inventory ·
monetization model · social features · replayability · retention
mechanics. Then name the genre and the target player.

Mark every inference you could not verify in the repo with
**[Assumption]**.

## Part 2 — Competitor and reference research

Use web search; this is current-research work, not recall. Identify
5–10 genuinely relevant games — same genre, same loop, same audience,
same art economy, or the same daily-ritual shape — mixing established
hits with recent risers. Do not pad the list with famous games that are
not actually comparable.

Start from these buckets, verify each title exists and is relevant, and
drop the ones that are not:
- minimalist and physics putting: Desert Golfing, What the Golf?,
  Wonderputt, OK Golf, Super Stickman Golf 3, Golf Peaks, Cursed to
  Golf;
- daily-loop retention and share culture: Wordle, NYT Connections and
  Strands, Puzzmo, Contexto/Globle, chess.com daily puzzle;
- seeded daily runs outside word games: Spelunky, Slay the Spire, Dead
  Cells dailies — how a seeded-run culture survives with no
  leaderboard, which is our situation by design;
- craft references, not competitors: Vlambeer's "Art of Screenshake",
  "Juice It or Lose It", Peggle's escalating hole-out, Angry Birds'
  slingshot readability.

For each game: name · why it is relevant here · core gameplay ·
strongest single feature · retention mechanics · UI/UX approach ·
visual approach · animation and VFX quality · sound and music approach
· progression · reward system · monetization · what players praise ·
what players complain about. Use store pages, gameplay videos, user
reviews, Reddit and YouTube; cite what you used. Player complaints are
the most valuable input here — quote real ones.

If the deep dives get long, put them in docs/AUDIT-COMPETITORS.md and
keep the condensed comparison in the main document.

## Part 3 — Feature gap analysis

Answer, in a table plus prose: what competitors do that we do not ·
what we do that they do not · which of their features genuinely add
value versus add complexity · where we are behind · where we are ahead
· what our USP is · whether that USP is actually felt by a player in
the first session or only visible in the README · how to sharpen it ·
and the one-sentence answer to "why would I play this instead of the
game already on my home screen?"

## Part 4 — Core gameplay analysis

Walk the loop: what the player does → why → what they get → what that
is good for → what it unlocks → what pulls them back. Rate fun factor,
skill expression, variety, challenge, feedback, risk/reward, decision
density, replayability and player agency.

Hunt specifically for: repetition, boredom onset, shallow depth, easy
to learn but nothing to master, decisions that do not matter, weak
action feedback, missing juice, unsatisfying inputs. A 10–20 second
attempt with two shots is a very small decision space — say honestly
whether it is enough, and if not, where the depth should come from.

## Part 5 — New mechanic proposals

Three tiers: **A — High impact** (materially moves quality or
retention), **B — Medium impact** (enriches, not critical), **C — Low
impact / optional**. For each: name · how it works · what the player
does · why it is fun · which problem it solves · how it connects to the
core loop · retention effect · development complexity · determinism and
generator-version cost (a new element means a v3 config, appended
draws, solver proof and golden-fixture work — price it) · priority.

## Part 6 — UI/UX audit

Audit the screens that exist: main menu, in-game HUD and the one-line
game bar, settings pop-up, Collection, Journey level select with its
pager, archive month calendar, weekly gauntlet, stats panel with the
histogram, achievements, tutorial hints, fail card, hole-out card,
share and import flow, next-hole countdown, toasts, loading overlay,
scene transitions.

Judge: modernity · professionalism · thumb reach on a phone held
one-handed · clarity of what to do next · wasted taps · information
hierarchy · CTA strength · consistency with the game's visual world ·
transition smoothness · anything that slows a decision. Note that the
UI is built entirely in code (UiConstruction + UIFactory) and baked
into scenes, so "redesign the menu" means writing layout code — price
accordingly.

Every problem in the format: Problem → why it is a problem → proposed
fix → expected benefit.

## Part 7 — Visual and art direction audit

This game is flat-colour, code-drawn 2D: there are no textures, no
lighting, no characters. Audit what actually carries the look — palette
and its colourblind variant, element silhouettes and instant
readability (can a player tell sand from ice from water at a glance,
mid-swipe?), contrast between playfield and hazards, composition and
camera framing per course, the ball and its trail, the flag, the
windmill, backgrounds and negative space, iconography, typography,
visual hierarchy, consistency, day-theme signalling.

Answer directly: **why does this not look like a more expensive game,
and what are the cheapest changes that would make it look like one?**
Split answers into quick wins · medium effort · major overhaul.

## Part 8 — Animation audit

Inventory and judge: ball motion and squash, aim line and power
readout, hole capture, rim-out, water sink, bumper reaction, windmill
blades, flag, button press, menu transitions, panel open/close, star
award, reward and unlock reveals, camera moves, scene fades.

Judge timing, responsiveness, easing, anticipation and follow-through,
and whether anything is too slow to repeat 20 times a session — this is
a restart-heavy game, so every celebration must be skippable or short.
Output a concrete juice/polish animation list.

## Part 9 — VFX audit

Impact effects, trails, particles, glows, screen flash, camera shake,
slow motion, letterbox, vignette, hole-out celebration, star burst,
unlock effects, UI effects, failure effects. Say precisely where the
game currently under-sells a moment, and what effect at what intensity
and duration fixes it. Intensity and duration are part of the proposal,
not an implementation detail.

## Part 10 — Sound design audit

Twelve synthesized SFX exist (bumper, capture, click, fail, ice,
jingle, ready, sand, shot, star, wall, water), all generated by a
committed editor tool, and there is no music by decision. Audit
coverage, quality, mutual coherence, and whether audio supports the
gameplay read. List the missing sounds — events that currently pass
silently — and, since new sounds must be synthesized, describe each one
in synthesis terms (waveform, envelope, pitch movement) so it can
actually be built. Pitch-ladder and stinger ideas are welcome; loops
are not.

## Part 11 — Game feel and juice audit

Take this section seriously: the gap between "works" and "feels good"
is the highest-leverage gap in a game this small. Check hit feedback,
camera shake, particles, impact sound, animation timing, screen flash,
slow motion, haptics (three tiers exist), UI transitions, number
popups, progress feedback, and the moment-to-moment loop of aim →
release → watch → rest. Name the exact moments that deserve to feel
better, ranked.

## Part 12 — Progression and retention

Evaluate the first 30 seconds · first 5 minutes · first 15 minutes ·
first session · day 1 · day 3 · day 7 · long term. Cover onboarding,
the four tutorial holes, FTUE, progression, unlocks, Collection,
achievements, streaks, the weekly gauntlet, themed days, Journey's
100 levels, and daily/weekly goals.

The central question: at every one of those moments, does the player
have a clear answer to "what is my next goal?" Where the answer is
weak, say what would give it teeth without a backend.

## Part 13 — Player journey map

Discovery → Install → First launch → Tutorial → First reward → First
goal → First session end → Return next morning → Progression → Sharing
→ Long-term engagement. For each stage: friction, drop-off risk,
improvement opportunity. Be specific about where players are lost.

## Part 14 — Player psychology

Which emotions does the game actually produce, and which are missing:
curiosity, satisfaction, mastery, progress, anticipation, competition,
collection, completion, reward, surprise. For each missing one, name
the mechanism that would create it here.

## Part 15 — Social and live ops without a backend

We will never have a server, so evaluate only what works offline:
replay codes, ghosts, course invite codes, the weekly gauntlet, save
transfer, themed days, seasonal or event days derived from the seed
alone, the parked ghost gallery idea. Judge whether the daily hole
currently produces any social pull at all, and what would create a
sharing culture without a leaderboard. Propose only what genuinely
serves the loop.

## Part 16 — Performance and technical polish

FPS and the 60/120 battery mode, load and generation time (practice
generation is async), memory, draw calls, particle counts, UI
performance, input latency, device compatibility, battery drain, and
the cost of anything you propose. Report as Impact / Severity /
Suggested solution. Where you cannot measure, say so and say what to
measure.

## Part 17 — Store and portfolio presentation

Two shop windows. First Google Play: icon, screenshots, feature
graphic, description, first impression, value proposition, USP,
trailer. Compare against the competitors you researched and answer:
seeing this on the store, why would anyone install it? Second the
GitHub README, which is this project's other storefront: does it sell
the determinism story to a technical reviewer in the first screen, and
what is missing (hero GIF and a WebGL demo are known gaps)?

## Part 18 — Monetization: the quarantined argument

IAP is a permanent non-goal and the game is free, MIT-licensed and
portfolio-first; that decision stands unless you can beat it. So do not
scatter monetization through the report. In this section only, answer
coldly: what does the no-monetization stance cost, if anything · would
any non-IAP model (paid app, one-time unlock, rewarded ads, donation)
fit this game without damaging it · what would each cost in trust,
UX and development · and what is your actual recommendation, including
"keep it free" if that is the honest answer. Same for the other
non-goals if you think one has become the wrong call. Nothing from this
section enters the main priority list unless I move it there.

## Part 19 — What NOT to build

Say what to remove or refuse, not only what to add. Cover feature
bloat, UI clutter, overdesign, unnecessary systems, and any mechanic
that would damage the core loop or the project's identity. Include at
least three things currently in the game that should be cut or
simplified — subtraction has already improved this project twice.

## Part 20 — Scoring and prioritization

Score every proposal: Player Impact /10 · Retention Impact /10 · UX
Impact /10 · Visual and Feel Impact /10 · Portfolio Impact /10 ·
Development Cost /10 (10 = cheap) · Development Risk /10 (10 = safe;
determinism, frozen v1, and save/code-format changes are the risky
ones). Combine into a Priority Score and explain the weighting you
chose. Then sort into P0 MUST HAVE · P1 HIGH · P2 MEDIUM · P3 NICE TO
HAVE.

## Part 21 — Quick wins and big bets

Two explicit lists. **Quick wins:** low effort, high impact — an
evening or less each. **Big bets:** expensive, potentially
transformative, the shape of a future major update. For big bets, state
what has to be true for the bet to pay off.

## Part 22 — Development roadmap

Six updates, each with features, rationale, priority, complexity and
expected player effect:
1. Polish — what makes today's build feel better with no new systems.
2. Gameplay — core loop and mechanics.
3. Progression — retention and long-term play.
4. Content — new courses, elements, modes, cosmetics.
5. Reach — store presentation, README, hero GIF, WebGL demo, trailer.
6. Backend-free live ops — seasonal and event structure that survives
   with no server.
Then a three-month plan (Month 1 / 2 / 3) for one solo developer
working evenings — realistic, not aspirational.

## Part 23 — Executive summary (put this FIRST in the document)

  Current level of the game: X/10
  Core Gameplay X/10 · UI/UX X/10 · Visual Quality X/10 · Animation
  X/10 · VFX X/10 · Sound X/10 · Game Feel X/10 · Onboarding X/10 ·
  Progression X/10 · Retention Potential X/10 · Shareability X/10 ·
  Accessibility X/10 · Technical Quality X/10 · Portfolio Impression
  X/10 · Market Potential X/10 · Overall X/10

Give low scores where they are deserved; a flattering audit is a
useless one. Then: the 5 greatest strengths · the 5 biggest problems ·
the first 10 things to do · the 10 features most worth adding · the 10
things to learn from competitors · the 5 things to definitely not add ·
and one sentence naming the single biggest weakness. If the honest
verdict is "it works, but nothing about it is memorable", write exactly
that.

## Proposal card format — mandatory, every item, no exceptions

### [ID] Short title
- **Problem today:** evidence — file:line, a screenshot, a doc line, a
  CourseViewer render, or a competitor comparison. Not a hunch.
- **Proposal:** concrete enough to hand to an implementer with no
  follow-up question.
- **Reference:** the game or talk, and the precise mechanism taken.
- **Felt when:** first launch / mid-run / hole-out / failure / next
  morning / share moment / reviewer reading the repo.
- **Scores:** the seven from Part 20, plus the Priority Score.
- **Cost:** S (<2h) · M (an evening) · L (multi-day).
- **Risk:** determinism / generator version / save or code format /
  scene rebuild / none.
- **Touches:** the files it would actually change.
- **Done when:** one checkable criterion — a test name, or the exact
  thing I should see in the editor.
- **After deeper pars:** better / worse / unnecessary.

Each proposal gets one ID and appears once. Elsewhere, reference the
ID — never restate the item. A reader must never meet the same idea
twice in different words.

## Analysis rules

1. No generic advice. "Make the UI nicer", "add more content",
   "improve the animations" are rejected on sight. If a sentence would
   be equally true of any other mobile game, delete it.
2. Every claim about the current game carries evidence. If you did not
   open it, do not assert it.
3. Every proposal states why, its player-experience effect, and its
   cost.
4. Never propose a feature merely because a competitor has it.
5. Guard the core loop against feature creep, and do not rewrite the
   game's identity to chase a genre norm.
6. Diagnose before prescribing — problem first, solution second.
7. Mark assumptions **[Assumption]**.
8. Never write "competitors do X" without having researched it in this
   session; cite the source.
9. Use real player reviews wherever you can find them.
10. Balance impact against effort in every recommendation.
11. Do not force systems that do not fit this genre or this project.
12. Keep your visual, technical and design proposals from contradicting
    each other; if two proposals conflict, say so and pick one.
13. Ask me at most 5 questions before writing, and only ones whose
    answer changes a recommendation. Otherwise state the assumption and
    continue.
14. Talk to me in Turkish. Write the documents in English, the repo's
    language.

## Deliverable

Write docs/AUDIT.md (and, if the research runs long,
docs/AUDIT-COMPETITORS.md). Those are the only files you create; no
code, scene, asset or config changes this session.

When the document is done, stop. I will pick the items; we then
implement them one at a time, each with its own tests, its own editor
check and its own commit.
```

---

## Follow-up prompt (after I have picked items)

```
Read docs/AUDIT.md. Implement only these items: <IDs>. One at a time:
tests first where core is involved, then the Unity layer, then tell me
what to check in the editor and wait. No commit until I confirm the
look and Unity is closed for the batch tests.
```
