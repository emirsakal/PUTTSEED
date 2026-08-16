#nullable enable
using System;
using System.Collections.Generic;
using PuttSeed.Core.CourseGen;
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

        private void ResetRun()
        {
            if (_generation == null)
            {
                return;
            }

            _sim = new GolfSim(_generation.Course, _simConfig);
            _prev = _curr = _sim.Ball;
            _playedShots.Clear();
            for (int i = 0; i < _ghosts.Count; i++)
            {
                ResetGhost(_ghosts[i]);
            }

            RunReset?.Invoke();
            StateChanged?.Invoke();
        }

        /// <summary>Adds a replay ghost stepped in lockstep with the player sim.</summary>
        public Ghost AddGhost(ShotInput[] shots, string label)
        {
            if (_generation == null)
            {
                throw new InvalidOperationException("Load a course before adding ghosts.");
            }

            var ghost = new Ghost { Shots = shots, Label = label };
            ResetGhost(ghost);
            _ghosts.Add(ghost);
            StateChanged?.Invoke();
            return ghost;
        }

        /// <summary>Adds the generator's author solution as a ghost.</summary>
        public Ghost AddAuthorGhost() => AddGhost(_generation!.AuthorSolution, "author");

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
            _sim.Shoot(shot);
            if (_sim.Strokes == before)
            {
                return false;
            }

            _playedShots.Add(shot);
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
                    if (ghost.Sim.IsAtRest && !ghost.Sim.IsHoled && ghost.NextShot < ghost.Shots.Length)
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
