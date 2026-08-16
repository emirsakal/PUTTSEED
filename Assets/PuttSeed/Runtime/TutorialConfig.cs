namespace PuttSeed.Unity
{
    /// <summary>
    /// FTUE per the GDD: three hand-picked fixed seeds, one hint line each, no
    /// text walls. Seeds were chosen by scanning generator output for courses
    /// that isolate the element being taught (56: no hazards; 10: bumpers
    /// only; 8: sand only).
    /// </summary>
    public static class TutorialConfig
    {
        /// <summary>One tutorial course: a fixed seed plus its single hint line.</summary>
        public readonly struct Stage
        {
            /// <summary>Fixed generator seed.</summary>
            public ulong Seed { get; }

            /// <summary>The one-line hint shown while playing this stage.</summary>
            public string Hint { get; }

            /// <summary>Creates a stage.</summary>
            public Stage(ulong seed, string hint)
            {
                Seed = seed;
                Hint = hint;
            }
        }

        /// <summary>The three tutorial stages, in teaching order.</summary>
        public static readonly Stage[] Stages =
        {
            new Stage(56UL, "Drag anywhere and release to shoot — reach the hole within the stroke limit."),
            new Stage(10UL, "Pink bumpers boost your ball. Bounce off them — or steer clear."),
            new Stage(8UL, "Sand kills your speed. Power through it or roll around."),
        };
    }
}
