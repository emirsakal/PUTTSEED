namespace PuttSeed.Unity
{
    /// <summary>
    /// FTUE per the GDD: hand-picked fixed seeds, one hint line each, no text
    /// walls. Seeds are chosen by scanning generator output for courses that
    /// isolate the element being taught (101: bumpers only; 43: sand only;
    /// 24: ice only).
    ///
    /// Extended 2026-08-19 with the element wave — gates, ramps, portals and
    /// windmills had shipped with nothing teaching them at all — and with
    /// water, which had gone untaught since the MVP despite being the only
    /// element that costs a stroke. The opening lesson was re-picked at the
    /// same time: seed 35 was curated as "no hazards" and had since drifted to
    /// a course carrying water and ice, so the first hole a new player ever
    /// saw opened with two elements the tutorial had not reached yet. 304 is
    /// bare corridor. <see cref="PuttSeed.Unity.Tests"/> holds every seed here
    /// to its hint, which is how that drift was found.
    /// </summary>
    public static class TutorialConfig
    {
        /// <summary>
        /// What a stage exists to teach. Declared rather than implied, so a
        /// test can hold the curated seed to its promise: a lesson whose
        /// course does not actually contain its element teaches nothing.
        /// </summary>
        public enum Lesson
        {
            /// <summary>The shot itself — drag, release, reach the cup.</summary>
            Shot,

            /// <summary>Bumpers.</summary>
            Bumper,

            /// <summary>Sand zones.</summary>
            Sand,

            /// <summary>Ice zones.</summary>
            Ice,

            /// <summary>Water zones — the only element that costs a stroke.</summary>
            Water,

            /// <summary>One-way gates.</summary>
            Gate,

            /// <summary>Ramp slopes.</summary>
            Ramp,

            /// <summary>Portal pairs.</summary>
            Portal,

            /// <summary>Windmills.</summary>
            Windmill,
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
        /// The tutorial stages, in teaching order: the shot, then the five
        /// elements a v1 course can hold, then the four the wave added. The
        /// wave's lessons are v2 seeds, because their elements do not exist in
        /// v1 at all; each was picked from a 3000-seed scan for isolating its
        /// element — no other wave element on the course, and as little else
        /// as generation would give.
        /// </summary>
        public static readonly Stage[] Stages =
        {
            new Stage(304UL, "Drag anywhere and release to shoot — reach the hole within the stroke limit.",
                Lesson.Shot),
            new Stage(101UL, "Pink bumpers boost your ball. Bounce off them — or steer clear.",
                Lesson.Bumper),
            new Stage(43UL, "Sand kills your speed. Power through it or roll around.",
                Lesson.Sand),
            new Stage(24UL, "Ice barely slows the ball — ease off and plan for the long slide.",
                Lesson.Ice),
            new Stage(148UL, "Water costs a stroke and puts the ball back where it was — go around it.",
                Lesson.Water),
            new Stage(1046UL, "Amber gates pass one way only — the chevrons point the way through.",
                Lesson.Gate, configVersion: 2),
            new Stage(1599UL, "Ramps tilt the green: the arrows point downhill, and the ball runs with them.",
                Lesson.Ramp, configVersion: 2),
            new Stage(2019UL, "Portals come in pairs — in one mouth, out of its twin, still moving.",
                Lesson.Portal, configVersion: 2),
            new Stage(535UL, "The blades never stop turning. Watch for your gap, then take the shot.",
                Lesson.Windmill, configVersion: 2),
        };
    }
}
