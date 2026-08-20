# LATER — idea parking lot

Per the GDD's "Out of scope" rule: new ideas land here, never in the sprint.
Nothing below is planned; it is written down so it stops occupying headspace.

## From the GDD's explicit non-goals

- Online leaderboards (would need a backend or store-services integration)
- Accounts / cloud save
- iOS release
- Additional course element types beyond the MVP five
- Level editor
- Multiplayer of any kind
- Push notifications ("today's course is up!")
- IAP / cosmetic skins

## Parked during development

- **Ghost gallery** — keep the last N imported replays and race several
  ghosts at once (the lockstep ghost architecture already supports N).
- **Par 4 holes** — v4 brought par 3 (see Graduated). Par 4 was measured and
  left here: a 32-unit corridor produced 3% par 4 at nine times the cost,
  with one seed in five failing to generate at all. Par is not a function of
  length — a shot carries ~5.5 units and the solver banks off walls, so an
  open corridor stays cheap however long it gets. Par 4 needs a different
  KIND of hole: tight throats, blind pockets, hazards placed to close the
  direct line. A generator project, not a knob turn.
- **Replay scrubbing** — since replays are re-simulation, a timeline scrubber
  only needs periodic state snapshots (RestoreRest already exists).

## Graduated (built after parking)

- Deeper pars (generator v4, 2026-08-19): corridors to 22 units and a solver
  budget that can prove a two-shot solution does not exist. 64% par 2, 36%
  par 3, nothing failing to generate. The cost was 23x generation time, paid
  for by a 3x faster simulation and by the menu handing the game the hole it
  had already grown.

- Async practice generation (background `Task.Run`, 2026-08-16)
- Course of the day archive (menu Archive panel, 2026-08-17; became a month
  calendar 2026-08-18)
- Weekly gauntlet (`GauntletWeek` + `GauntletCodec`, `PUTTWK-` codes whose
  seven seeds derive from the week index, 2026-08-18)
- Colorblind palettes (2026-08-17)
- Oriented impact squash (2026-08-17)
