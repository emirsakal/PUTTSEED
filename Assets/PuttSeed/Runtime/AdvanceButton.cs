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

        /// <summary>Where the button goes when it is pressed.</summary>
        public enum Destination
        {
            /// <summary>Nowhere — the button is not offered.</summary>
            None,

            /// <summary>The next journey level.</summary>
            NextJourneyLevel,

            /// <summary>The next hole of the gauntlet week.</summary>
            NextGauntletHole,

            /// <summary>Another practice course.</summary>
            NewPracticeCourse,

            /// <summary>The next tutorial lesson.</summary>
            NextTutorialLesson,

            /// <summary>Out of the tutorial altogether — the only one that changes scene.</summary>
            FinishTutorial,
        }

        /// <summary>
        /// Where the run can go from here. This is the decision; the label is
        /// derived from it and so is the action, which is the point: the two
        /// used to be written out separately — the rules here, the calls in a
        /// click handler — and nothing stopped a mode from being offered a
        /// button that did nothing when pressed.
        /// </summary>
        public static Destination DestinationFor(GameMode mode, bool holed, bool failed,
            bool hasNextTutorialStage, bool hasNextJourneyLevel, bool hasNextGauntletHole)
        {
            bool settled = holed || failed;
            switch (mode)
            {
                case GameMode.Tutorial:
                    // Always offered: a lesson is not a challenge to pass, and
                    // a player who wants the next one should not have to hole
                    // this one first.
                    return hasNextTutorialStage
                        ? Destination.NextTutorialLesson
                        : Destination.FinishTutorial;

                case GameMode.Journey:
                    return holed && hasNextJourneyLevel
                        ? Destination.NextJourneyLevel
                        : Destination.None;

                case GameMode.Gauntlet:
                    // A failed hole still spends its strokes and the week
                    // carries on, so the gauntlet advances on settled, not on
                    // holed.
                    return settled && hasNextGauntletHole
                        ? Destination.NextGauntletHole
                        : Destination.None;

                case GameMode.Practice:
                    // The mode's whole promise is another one, now. It used to
                    // take a trip through the menu — finish, Menu, Practice —
                    // to get a course the game had already grown in the
                    // background.
                    return settled ? Destination.NewPracticeCourse : Destination.None;

                default:
                    return Destination.None;
            }
        }

        /// <summary>Decides the button from the run.</summary>
        public static State For(GameMode mode, bool holed, bool failed,
            bool hasNextTutorialStage, bool hasNextJourneyLevel, bool hasNextGauntletHole)
        {
            switch (DestinationFor(mode, holed, failed,
                hasNextTutorialStage, hasNextJourneyLevel, hasNextGauntletHole))
            {
                case Destination.NextTutorialLesson:
                    return new State(true, "Next lesson");

                case Destination.FinishTutorial:
                    return new State(true, "Finish tutorial");

                case Destination.NextJourneyLevel:
                    return new State(true, "Next level");

                case Destination.NextGauntletHole:
                    return new State(true, "Next hole");

                case Destination.NewPracticeCourse:
                    return new State(true, "New course");

                default:
                    return Hidden;
            }
        }
    }
}
