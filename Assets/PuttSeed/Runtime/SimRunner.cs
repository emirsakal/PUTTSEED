#nullable enable
using System;
using System.Collections.Generic;
using PuttSeed.Core.CourseGen;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Owns the deterministic simulation on the Unity side: accumulates frame
    /// time, steps <see cref="PuttSeed.Core.Sim.GolfSim.Tick"/> at a fixed
    /// 120 Hz, keeps the last two states and exposes interpolated positions for
    /// rendering. Ghost sims are stepped in lockstep with the player sim.
    /// No game rules live here — the sim decides everything.
    /// </summary>
    public sealed class SimRunner : MonoBehaviour
    {
        /// <summary>A read-only replay sim rendered as a translucent ball.</summary>
        public sealed class Ghost
        {
            internal GolfSim Sim = null!;
            internal ShotInput[] Shots = Array.Empty<ShotInput>();

            /// <summary>
            /// Mill clock each shot was taken at. Empty for untimed (pre-v3)
            /// codes, which shoot as soon as the ball rests.
            /// </summary>
            internal int[] ShotClocks = Array.Empty<int>();

            internal int NextShot;
            internal BallState Prev;
            internal BallState Curr;

            /// <summary>Display label ("author", "import").</summary>
            public string Label = "";

            /// <summary>True once the ghost's replay has holed out.</summary>
            public bool IsHoled => Sim.IsHoled;
        }

        [Tooltip("Feel asset; falls back to core defaults when absent.")]
        public FeelConfig? feel;

        private readonly FixedStepper _stepper = new FixedStepper();
        private readonly List<Ghost> _ghosts = new List<Ghost>();
        private readonly List<ShotInput> _playedShots = new List<ShotInput>();

        // The mill clock each accepted shot launched at — the timing half of a
        // v3 replay. Blades keep turning while the player lines up, so the
        // angle a shot met is only reproducible with this.
        private readonly List<int> _playedShotClocks = new List<int>();
        // Per accepted shot: launch rest position + strokes BEFORE the shot
        // (water penalties can push strokes past the shot index).
        private readonly List<(Vec2Fix Origin, int Strokes)> _shotOrigins =
            new List<(Vec2Fix, int)>();

        private SimConfig _simConfig = SimConfig.Default;

        private GenerationResult? _generation;
        private GolfSim? _sim;
        private BallState _prev;
        private BallState _curr;

        /// <summary>Raised whenever visible state may have changed (ticks, shots, reset).</summary>
        public event Action? StateChanged;

        /// <summary>Raised after a run reset (load or retry) — clear trails etc.</summary>
        public event Action? RunReset;

        /// <summary>Raised when the sim accepts a stroke (audio/haptics hook).</summary>
        public event Action? ShotFired;

        /// <summary>Seed of the loaded course.</summary>
        public ulong Seed { get; private set; }

        /// <summary>The loaded generation result (course + author solution), if any.</summary>
        public GenerationResult? Generation => _generation;

        /// <summary>The live sim, if a course is loaded.</summary>
        public GolfSim? Sim => _sim;

        /// <summary>
        /// The exact config the player sim runs under (post difficulty
        /// relaxation) — preview sims must use this, or their physics lies.
        /// </summary>
        public SimConfig PlayConfig => _simConfig;

        /// <summary>Ghost list (read-only view).</summary>
        public IReadOnlyList<Ghost> Ghosts => _ghosts;

        /// <summary>Shots accepted this run, in order (for sharing the replay).</summary>
        public IReadOnlyList<ShotInput> PlayedShots => _playedShots;

        /// <summary>Mill clock of each accepted shot, aligned with <see cref="PlayedShots"/>.</summary>
        public IReadOnlyList<int> PlayedShotClocks => _playedShotClocks;

        /// <summary>The rest position the latest accepted shot launched from.</summary>
        public Vec2Fix LastShotOrigin { get; private set; }

        /// <summary>The latest accepted shot (slow-mo replay support).</summary>
        public ShotInput LastShot { get; private set; }

        /// <summary>Interpolated ball position at the current render frame.</summary>
        public Vector2 BallRenderPosition => Vector2.Lerp(
            FixView.ToVector2(_prev.Position), FixView.ToVector2(_curr.Position), _stepper.Alpha);

        /// <summary>Interpolated position of a ghost's ball.</summary>
        public Vector2 GhostRenderPosition(Ghost ghost) => Vector2.Lerp(
            FixView.ToVector2(ghost.Prev.Position), FixView.ToVector2(ghost.Curr.Position), _stepper.Alpha);

        /// <summary>
        /// Generates and loads the course for a seed using the current feel
        /// config (generation runs under the same config the player will play,
        /// so the solvability proof holds for this exact build).
        /// </summary>
        public void LoadSeed(ulong seed)
        {
            var config = feel != null ? feel.BuildSimConfig() : SimConfig.Default;
            AdoptGeneration(seed, CourseGenerator.Generate(
                seed, GeneratorConfig.Default, config, SolverConfig.Default), config);
        }

        /// <summary>
        /// Adopts an externally generated course (e.g. the practice mode's
        /// difficulty-filtered candidates). The generation MUST have been
        /// produced under <paramref name="config"/> for the solvability proof
        /// to hold.
        /// </summary>
        public void AdoptGeneration(ulong seed, GenerationResult generation, SimConfig config)
        {
            Seed = seed;
            // Play may relax hole capture on Easy/Normal courses (rated
            // difficulty is seed-deterministic, so replays stay identical).
            _simConfig = feel != null ? feel.BuildPlayConfig(config, generation.Difficulty) : config;
            _generation = generation;
            _ghosts.Clear();
            ResetRun();
        }

        /// <summary>Restarts the current course (attempt counter is UI-side).</summary>
        public void Retry() => ResetRun();

        /// <summary>
        /// Undoes the last accepted shot (the practice mulligan): restores the
        /// pre-shot rest state via core's RestoreRest — bit-exact, so replays
        /// of the remaining shots stay valid — and reverts any water penalty
        /// that shot incurred. Works mid-roll; refuses once the ball is holed.
        /// </summary>
        public bool TryUndoShot()
        {
            if (_sim == null || _playedShots.Count == 0 || _sim.IsHoled)
            {
                return false;
            }

            int last = _playedShots.Count - 1;
            _sim.RestoreRest(_shotOrigins[last].Origin, _shotOrigins[last].Strokes);
            _playedShots.RemoveAt(last);
            if (_playedShotClocks.Count > last)
            {
                _playedShotClocks.RemoveAt(last);
            }

            _shotOrigins.RemoveAt(last);
            _prev = _curr = _sim.Ball;
            for (int i = 0; i < _ghosts.Count; i++)
            {
                ResetGhost(_ghosts[i]);
            }

            RunReset?.Invoke(); // clears trails and presentation state
            StateChanged?.Invoke();
            return true;
        }

        private void ResetRun()
        {
            if (_generation == null)
            {
                return;
            }

            _sim = new GolfSim(_generation.Course, _simConfig);
            _prev = _curr = _sim.Ball;
            _playedShots.Clear();
            _playedShotClocks.Clear();
            _shotOrigins.Clear();
            for (int i = 0; i < _ghosts.Count; i++)
            {
                ResetGhost(_ghosts[i]);
            }

            RunReset?.Invoke();
            StateChanged?.Invoke();
        }

        /// <summary>Adds a replay ghost stepped in lockstep with the player sim.</summary>
        public Ghost AddGhost(ShotInput[] shots, string label)
            => AddGhost(shots, label, Array.Empty<int>());

        /// <summary>
        /// Adds a replay ghost whose shots are held to the mill clocks they
        /// were taken at (pass an empty array for untimed, pre-v3 replays).
        /// </summary>
        public Ghost AddGhost(ShotInput[] shots, string label, int[] shotClocks)
        {
            if (_generation == null)
            {
                throw new InvalidOperationException("Load a course before adding ghosts.");
            }

            var ghost = new Ghost { Shots = shots, Label = label, ShotClocks = shotClocks };
            ResetGhost(ghost);
            _ghosts.Add(ghost);
            StateChanged?.Invoke();
            return ghost;
        }

        /// <summary>Adds the generator's author solution as a ghost.</summary>
        public Ghost AddAuthorGhost()
            => AddGhost(_generation!.AuthorSolution, "author", _generation.AuthorShotClocks);

        /// <summary>Removes all ghosts.</summary>
        public void ClearGhosts()
        {
            _ghosts.Clear();
            StateChanged?.Invoke();
        }

        /// <summary>Removes the ghosts carrying a label ("author", "best", "import").</summary>
        public void RemoveGhosts(string label)
        {
            _ghosts.RemoveAll(g => g.Label == label);
            StateChanged?.Invoke();
        }

        /// <summary>True when any ghost carries the label.</summary>
        public bool HasGhost(string label)
        {
            for (int i = 0; i < _ghosts.Count; i++)
            {
                if (_ghosts[i].Label == label)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetGhost(Ghost ghost)
        {
            ghost.Sim = new GolfSim(_generation!.Course, _simConfig);
            ghost.NextShot = 0;
            ghost.Prev = ghost.Curr = ghost.Sim.Ball;
        }

        /// <summary>
        /// Whether a resting ghost may take its next shot yet. A timed replay
        /// waits for the blade phase its shot was taken at — the clock wraps,
        /// so the ghost never idles longer than one full turn, and on a course
        /// without mills it never waits at all.
        /// </summary>
        private bool GhostMayShoot(Ghost ghost)
        {
            if (ghost.ShotClocks.Length <= ghost.NextShot
                || _generation?.Course.Windmills.Length == 0)
            {
                return true;
            }

            return ghost.Sim.MillClock == ghost.ShotClocks[ghost.NextShot];
        }

        /// <summary>
        /// Forwards a quantized shot to the sim. Returns true when the sim
        /// accepted it (at rest, hole open, strokes left).
        /// </summary>
        public bool TryShoot(ShotInput shot)
        {
            if (_sim == null)
            {
                return false;
            }

            int before = _sim.Strokes;
            var origin = _sim.Ball.Position; // at rest — Shoot only sets velocity
            _sim.Shoot(shot);
            if (_sim.Strokes == before)
            {
                return false;
            }

            LastShotOrigin = origin;
            LastShot = shot;
            _playedShots.Add(shot);
            _playedShotClocks.Add(_sim.MillClock);
            _shotOrigins.Add((origin, before));
            ShotFired?.Invoke();
            StateChanged?.Invoke();
            return true;
        }

        private void Update()
        {
            if (_sim == null)
            {
                return;
            }

            int ticks = _stepper.Advance(Time.deltaTime);
            for (int t = 0; t < ticks; t++)
            {
                _prev = _curr;
                _sim.Tick();
                _curr = _sim.Ball;

                for (int g = 0; g < _ghosts.Count; g++)
                {
                    var ghost = _ghosts[g];
                    ghost.Prev = ghost.Curr;
                    if (ghost.Sim.IsAtRest && !ghost.Sim.IsHoled && ghost.NextShot < ghost.Shots.Length
                        && GhostMayShoot(ghost))
                    {
                        ghost.Sim.Shoot(ghost.Shots[ghost.NextShot]);
                        ghost.NextShot++;
                    }

                    ghost.Sim.Tick();
                    ghost.Curr = ghost.Sim.Ball;
                }
            }

            if (ticks > 0)
            {
                StateChanged?.Invoke();
            }
        }
    }
}
