# PUTTSEED — Claude Code prompts

Paste these one at a time. Run the acceptance checks yourself between
phases. Don't feed the next phase until the current one is verifiably
green.

---

## Kickoff prompt (paste first, in a fresh session at repo root)

```
Read CLAUDE.md, docs/ARCHITECTURE.md, docs/GDD.md and docs/ROADMAP.md
fully before doing anything.

Then execute ROADMAP Week 1 only:
1. Scaffold the repo exactly as CLAUDE.md's layout describes: a
   netstandard2.1 class library PuttSeed.Core, an NUnit test project
   PuttSeed.Core.Tests, a solution file, and scripts/test.bat that runs
   `dotnet test core`. Verify `dotnet test core` runs (0 tests, green).
2. Work strictly TDD in small steps, committing after each green cycle
   with conventional commit messages, in this order: Fix64 -> Vec2Fix ->
   FixRng -> sin/cos table -> GolfSim integration & friction -> wall
   collision with sub-stepping -> rest detection & StateHash -> bumper ->
   sand -> water -> hole capture -> 10k-tick determinism test with a
   committed golden hash.
3. Enforce CLAUDE.md hard rules mechanically: add a script
   scripts/check-purity.bat that greps core/src for float, double,
   System.Random, DateTime and UnityEngine and fails if found; wire it
   into test.bat.
4. Finish by writing a short STATUS.md summarizing what exists, test
   counts, and any deviations from ARCHITECTURE.md with reasons.

Do not touch Unity, generation, or replay this session. Ask me before
deviating from the architecture doc.
```

---

## Week 2 prompt

```
Read CLAUDE.md, docs/ARCHITECTURE.md, docs/GDD.md, STATUS.md. Execute
ROADMAP Week 2: CourseGen (corridor growth, decoration, clearance),
SolvabilityChecker (BFS over the quantized shot space with tick caps,
store the author solution), DifficultyRater, ReplayCodec with round-trip
and golden-fixture tests, DailySeed, and the 1000-seed property suite.
Also build tools/CourseViewer: a console app that takes a seed and prints
an ASCII render of the course plus the author solution shot list. TDD,
small conventional commits, update STATUS.md at the end. Do not touch
Unity yet.
```

---

## Week 3 prompt

```
Read CLAUDE.md, docs/ARCHITECTURE.md, docs/GDD.md, STATUS.md. Execute
ROADMAP Week 3. Create the Unity 6 project under unity/ via the Unity CLI
(-batchmode -createProject), link core/src into Assets via a directory
junction plus an asmdef with noEngineReferences=true, and add
scripts/unity-tests.bat and scripts/build-android.bat (batch mode).
Implement SimRunner (fixed 120 Hz stepping + render interpolation), drag
input quantized to ShotInput, flat-color course rendering, ball trail,
camera framing, ghost playback, and the minimal UI from the GDD (aim
line, power, stroke counter, retry, share/import of PUTT- codes). Expose
feel parameters (friction, power curve, bumper restitution, capture
threshold) in one FeelConfig ScriptableObject so I can tune on device.
Update STATUS.md.
```

---

## Week 4 prompt

```
Read CLAUDE.md, docs/GDD.md, docs/ROADMAP.md, STATUS.md. Execute ROADMAP
Week 4: daily mode from the UTC date seed, practice mode with difficulty
buckets, local stats and streak (JSON in persistentDataPath), 3
fixed-seed tutorial courses with one-line hints, audio hookup points for
my purchased pack, haptics, hole-in celebration, app icon/splash wiring,
Android signing config reading from a local keystore.properties (never
commit secrets), and a release .aab build script. Then write the public
README.md: project pitch, architecture diagram, determinism proof section
referencing the test suite, how to run tests, an ASCII course render, and
a LATER.md for out-of-scope ideas. Update STATUS.md.
```

---

## Utility prompts

- Feel tuning session: `Here are my on-device notes: <notes>. Propose
  concrete FeelConfig value changes one variable at a time, explain the
  expected effect of each, and wait for my test result before the next.`
- When stuck in a fix loop: `Stop patching. Re-read the failing test,
  explain the root cause from first principles, list 2-3 candidate fixes
  with trade-offs, and ask me before implementing.`
- Before each commit batch: `Run scripts/test.bat and
  scripts/check-purity.bat; paste the summary output.`
