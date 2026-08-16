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
- **Weekly gauntlet** — seven consecutive daily seeds scored as one round,
  shareable as a single combined code (codec has a version byte spare).
- **Deeper pars** — raise the solver's tick budget/depth so generation can
  certify par 4–5 courses; costs generation time, revisit after profiling on
  device (see SolverConfig notes in STATUS.md).
- **Squash orientation** — impact squash is axis-aligned; orienting it along
  the contact normal reads better on side hits.
- **Colorblind palettes** — the palette is a single static class
  (PaletteMaterials); swapping it per accessibility profile is cheap.
- **Replay scrubbing** — since replays are re-simulation, a timeline scrubber
  only needs periodic state snapshots (RestoreRest already exists).

## Graduated (built after parking)

- Async practice generation (background `Task.Run`, 2026-08-16)
- Course of the day archive (menu Archive panel, 2026-08-17)
