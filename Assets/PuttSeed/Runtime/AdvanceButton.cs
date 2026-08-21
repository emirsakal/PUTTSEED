#nullable enable

namespace PuttSeed.Unity
{
    /// <summary>
    /// What the bar's one advance button says, and whether it is there at all.
    ///
    /// Five modes want different things from the same button — a lesson, a
    /// level, a gauntlet hole, a fresh practice course, or the end of the
    /// tutorial — and the rules had grown into a pair of nested conditionals
    /// inside a per-frame refresh, where nothing could be checked. They live
    /// here as one function of the run's state so a test can hold each of
    /// them, including the one that is easy to get wrong: practice offers a
    /// NEW course when the ball settles, holed or not, because a practice
    /// course you failed is finished with too.
    /// </summary>
    public static class AdvanceButton
    {
        /// <summary>The button's state for one moment of one run.</summary>
        public readonly struct State
        {
            /// <summary>Whether the button belongs on the bar at all.</summary>
            public readonly bool Visible;

            /// <summary>The English label, before localization.</summary>
            public readonly string Label;

            /// <summary>Creates a state.</summary>
            public State(bool visible, string label)
            {
                Visible = visible;
                Label = label;
            }
        }

        /// <summary>Hidden — nothing to advance to.</summary>
        public static readonly State Hidden = new State(false, "");

        /// <summary>Decides the button from the run.</summary>
        public static State For(GameMode mode, bool holed, bool failed,
            bool hasNextTutorialStage, bool hasNextJourneyLevel, bool hasNextGauntletHole)
        {
            bool settled = holed || failed;
            switch (mode)
            {
                case GameMode.Tutorial:
                    // Always offered: a lesson is not a challenge to pass, and
                    // a player who wants the next one should not have to hole
                    // this one first.
                    return new State(true, hasNextTutorialStage ? "Next lesson" : "Finish tutorial");

                case GameMode.Journey:
                    return holed && hasNextJourneyLevel
                        ? new State(true, "Next level")
                        : Hidden;

                case GameMode.Gauntlet:
                    // A failed hole still spends its strokes and the week
                    // carries on, so the gauntlet advances on settled, not on
                    // holed.
                    return settled && hasNextGauntletHole
                        ? new State(true, "Next hole")
                        : Hidden;

                case GameMode.Practice:
                    // The mode's whole promise is another one, now. It used to
                    // take a trip through the menu — finish, Menu, Practice —
                    // to get a course the game had already grown in the
                    // background.
                    return settled ? new State(true, "New course") : Hidden;

                default:
                    return Hidden;
            }
        }
    }
}
