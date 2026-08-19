#nullable enable
using System.Collections.Generic;
using System.Text;

namespace PuttSeed.Unity
{
    /// <summary>
    /// What each stroke of a run met, and the little emoji line that makes a
    /// finished run legible to someone who was not there. A replay code proves
    /// the run but reads as noise; a row of glyphs tells the story, which is
    /// the whole reason Wordle's grid travelled and its answer did not.
    ///
    /// Presentation only. The marks come from the sim's deterministic event
    /// counters as <see cref="FeedbackController"/> already observes them, so
    /// two players who play the same shots produce the same line — but nothing
    /// here can reach back into the simulation.
    /// </summary>
    public sealed class ShotLog
    {
        /// <summary>What a single stroke ran into.</summary>
        [System.Flags]
        public enum Mark
        {
            /// <summary>A clean roll across the felt.</summary>
            None = 0,

            /// <summary>Banked off at least one wall.</summary>
            Wall = 1 << 0,

            /// <summary>Took a bumper.</summary>
            Bumper = 1 << 1,

            /// <summary>Crossed sand.</summary>
            Sand = 1 << 2,

            /// <summary>Crossed ice.</summary>
            Ice = 1 << 3,

            /// <summary>Found water — the stroke that costs a stroke.</summary>
            Water = 1 << 4,

            /// <summary>Was turned back by a one-way gate.</summary>
            Gate = 1 << 5,

            /// <summary>Ran down a ramp.</summary>
            Ramp = 1 << 9,

            /// <summary>Went through a portal.</summary>
            Portal = 1 << 6,

            /// <summary>Was slapped by a windmill blade.</summary>
            Windmill = 1 << 7,

            /// <summary>Dropped. Always the last stroke of a finished run.</summary>
            Holed = 1 << 8,
        }

        private readonly List<Mark> _shots = new List<Mark>();

        /// <summary>The marks, one per stroke taken, in order.</summary>
        public IReadOnlyList<Mark> Shots => _shots;

        /// <summary>Clears the log — a new course or a retry starts empty.</summary>
        public void Reset() => _shots.Clear();

        /// <summary>Opens a stroke's entry; every later mark lands on it.</summary>
        public void BeginShot() => _shots.Add(Mark.None);

        /// <summary>Adds what just happened to the stroke in progress.</summary>
        public void Record(Mark mark)
        {
            if (_shots.Count > 0)
            {
                _shots[_shots.Count - 1] |= mark;
            }
        }

        /// <summary>The scorecard line for this run (empty before the first shot).</summary>
        public string Glyphs() => Render(_shots);

        /// <summary>
        /// One glyph per stroke: the most telling thing that happened to it.
        /// Ordered by what a reader would ask about first — the drop, then the
        /// penalty, then the strange, then the ordinary.
        /// </summary>
        public static string Render(IReadOnlyList<Mark> shots)
        {
            var line = new StringBuilder(shots.Count * 2);
            for (int i = 0; i < shots.Count; i++)
            {
                line.Append(GlyphFor(shots[i]));
            }

            return line.ToString();
        }

        private static string GlyphFor(Mark mark)
        {
            if ((mark & Mark.Holed) != 0) { return "⛳"; }
            if ((mark & Mark.Water) != 0) { return "💧"; }
            if ((mark & Mark.Portal) != 0) { return "🌀"; }
            if ((mark & Mark.Windmill) != 0) { return "🌬"; }
            if ((mark & Mark.Bumper) != 0) { return "🔴"; }
            if ((mark & Mark.Gate) != 0) { return "🚪"; }
            if ((mark & Mark.Ramp) != 0) { return "🔻"; }
            if ((mark & Mark.Sand) != 0) { return "🟫"; }
            if ((mark & Mark.Ice) != 0) { return "🧊"; }
            if ((mark & Mark.Wall) != 0) { return "⬜"; }
            return "🟩";
        }
    }
}
