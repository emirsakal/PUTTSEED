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
    /// The opening lesson was re-picked in the same pass: seed 35 was curated
    /// as "no hazards" and had since drifted into carrying water and ice, so
    /// the first hole a new player ever saw opened with two elements the
    /// tutorial had not reached yet. 304 is bare corridor.
    /// <see cref="PuttSeed.Unity.Tests"/> holds every seed here to its
    /// declaration, which is how that drift was found.
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
            /// The generator the seed was curated against. The first four
            /// lessons are v1 and stay v1 forever; the element wave only
            /// exists from v2, so its lessons declare it.
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
        /// elements its lesson declares and nothing else; the wave's two are
        /// v2, because gates, ramps, portals and windmills do not exist in v1.
        /// </summary>
        public static readonly Stage[] Stages =
        {
            new Stage(304UL, "Drag anywhere and release to shoot — reach the hole within the stroke limit.",
                Lesson.Shot),
            new Stage(40UL, "Bumpers boost the ball, sand drags it down — both sit on your way to the cup.",
                Lesson.Bumper | Lesson.Sand),
            new Stage(17UL, "Ice barely slows the ball; water costs a stroke and puts it back where it was.",
                Lesson.Ice | Lesson.Water),
            new Stage(216UL, "Arrows show the way: gates pass from one side only, ramps push you downhill.",
                Lesson.Gate | Lesson.Ramp, configVersion: 2),
            new Stage(522UL, "Portals throw the ball to their twin — and the blades never stop turning.",
                Lesson.Portal | Lesson.Windmill, configVersion: 2),
        };
    }
}
