namespace PuttSeed.Unity
{
    /// <summary>
    /// FTUE per the GDD: hand-picked fixed seeds, one hint line each, no text
    /// walls. Seeds were chosen by scanning generator output for courses that
    /// isolate the element being taught (35: no hazards; 101: bumpers only;
    /// 43: sand only; 24: ice only). Re-scanned when ice entered generation.
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

        /// <summary>The tutorial stages, in teaching order.</summary>
        public static readonly Stage[] Stages =
        {
            new Stage(35UL, "Drag anywhere and release to shoot — reach the hole within the stroke limit."),
            new Stage(101UL, "Pink bumpers boost your ball. Bounce off them — or steer clear."),
            new Stage(43UL, "Sand kills your speed. Power through it or roll around."),
            new Stage(24UL, "Ice barely slows the ball — ease off and plan for the long slide."),
        };
    }
}
