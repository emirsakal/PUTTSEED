namespace PuttSeed.Unity
{
    /// <summary>
    /// FTUE per the GDD: hand-picked fixed seeds, one hint line each, no text
    /// walls. Seeds come from generator scans, chosen for carrying exactly the
    /// elements their lesson claims and nothing else.
    ///
    /// Rebuilt 2026-08-19. Gates, ramps, portals and windmills had shipped
    /// with nothing teaching them anywhere, and water had gone untaught since
    /// the MVP despite being the only element that costs a stroke — nine
    /// elements, four lessons. Teaching them one apiece would have walked a
    /// new player through nine courses before the game started, so related
    /// elements were paired and the tutorial came out SHORTER than it began:
    /// five lessons for nine elements.
    ///
    /// Re-picked again on 2026-08-19 when the game moved to the v4 generator:
    /// a seed grows a different hole under a different version, so every
    /// lesson had to be found again. The pass before it caught the opening
    /// lesson curated as "no hazards" and long since drifted into carrying
    /// water and ice — the first hole a new player ever saw, opening with two
    /// elements the tutorial had not reached yet.
    /// <see cref="PuttSeed.Unity.Tests"/> holds every seed here to its
    /// declaration, which is how that drift was found and how these five stay
    /// honest.
    /// </summary>
    public static class TutorialConfig
    {
        /// <summary>
        /// What a stage exists to teach. Declared rather than implied, so a
        /// test can hold the curated seed to its promise — and a flag set
        /// rather than a single value, because a lesson may introduce a PAIR
        /// that belongs together. The test then demands the course contain
        /// exactly what is declared here and nothing else.
        /// </summary>
        [System.Flags]
        public enum Lesson
        {
            /// <summary>The shot itself — drag, release, reach the cup.</summary>
            Shot = 0,

            /// <summary>Bumpers.</summary>
            Bumper = 1 << 0,

            /// <summary>Sand zones.</summary>
            Sand = 1 << 1,

            /// <summary>Ice zones.</summary>
            Ice = 1 << 2,

            /// <summary>Water zones — the only element that costs a stroke.</summary>
            Water = 1 << 3,

            /// <summary>One-way gates.</summary>
            Gate = 1 << 4,

            /// <summary>Ramp slopes.</summary>
            Ramp = 1 << 5,

            /// <summary>Portal pairs.</summary>
            Portal = 1 << 6,

            /// <summary>Windmills.</summary>
            Windmill = 1 << 7,
        }

        /// <summary>One tutorial course: a fixed seed plus its single hint line.</summary>
        public readonly struct Stage
        {
            /// <summary>Fixed generator seed.</summary>
            public ulong Seed { get; }

            /// <summary>The one-line hint shown while playing this stage.</summary>
            public string Hint { get; }

            /// <summary>
            /// The generator the seed was curated against — a lesson is a seed
            /// AND the version that grows it, and a seed carried to another
            /// version grows a different hole entirely.
            /// </summary>
            public int ConfigVersion { get; }

            /// <summary>What the stage is here to teach.</summary>
            public Lesson Teaches { get; }

            /// <summary>Creates a stage.</summary>
            public Stage(ulong seed, string hint, Lesson teaches, int configVersion = 1)
            {
                Seed = seed;
                Hint = hint;
                Teaches = teaches;
                ConfigVersion = configVersion;
            }
        }

        /// <summary>
        /// Five lessons for nine elements. One element per course meant nine
        /// courses before a new player reached the game, so the elements that
        /// share an idea now share a course: the two that change your speed,
        /// the slide and the penalty, the two the arrows point through, and
        /// the two that act on their own. Each pair is a single sentence, not
        /// two facts — which is the argument for pairing them, the shorter
        /// FTUE being the reward rather than the reason.
        ///
        /// Every seed comes from a generator scan and carries EXACTLY the
        /// elements its lesson declares and nothing else. All five are v4 and
        /// all five are par 2: a lesson teaches an element, and a hole worth
        /// three strokes teaches endurance on top of it.
        ///
        /// Scanned under the GAME's physics, not core's defaults. Acceptance
        /// depends on solvability and solvability depends on friction, so the
        /// same seed grows a different hole under the two — which is how the
        /// "ice and water" lesson was first picked as a course that turned out
        /// to carry three bumpers. The test caught it; the scan was redone.
        /// </summary>
        public static readonly Stage[] Stages =
        {
            new Stage(1181UL, "Drag anywhere and release to shoot — reach the hole within the stroke limit.",
                Lesson.Shot, configVersion: 4),
            new Stage(225UL, "Bumpers boost the ball, sand drags it down — both sit on your way to the cup.",
                Lesson.Bumper | Lesson.Sand, configVersion: 4),
            new Stage(2967UL, "Ice barely slows the ball; water costs a stroke and puts it back where it was.",
                Lesson.Ice | Lesson.Water, configVersion: 4),
            new Stage(2190UL, "Arrows show the way: gates pass from one side only, ramps push you downhill.",
                Lesson.Gate | Lesson.Ramp, configVersion: 4),
            new Stage(1620UL, "Portals throw the ball to their twin — and the blades never stop turning.",
                Lesson.Portal | Lesson.Windmill, configVersion: 4),
        };
    }
}
