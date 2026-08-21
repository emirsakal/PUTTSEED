#nullable enable

namespace PuttSeed.Unity
{
    /// <summary>
    /// Whether a save has ever been greeted, and what is worth asking when it
    /// has not.
    ///
    /// The rule for what belongs on a first-run screen is narrow: ask only
    /// what the player can ANSWER before playing, and what is worse to
    /// discover late. Three questions pass that test — the language, the
    /// colour palette and how much the screen is allowed to move — and every
    /// other setting fails it for a reason worth writing down.
    ///
    /// Aim style is the clearest failure. "Sling or direct" is meaningless to
    /// somebody who has never taken a shot; the tutorial teaches it, and the
    /// setting waits in Settings for a player with an opinion. Haptics cannot
    /// be judged before feeling one. The frame rate cannot be judged at all,
    /// and its default is right. Sound is the most discoverable setting a
    /// phone has — there is a hardware button for it — so spending a question
    /// on it buys nothing.
    ///
    /// The two accessibility questions earn their place the other way round:
    /// a player who needs a colourblind palette or less motion needs it from
    /// the first frame, not after hunting through a menu they cannot read
    /// comfortably.
    /// </summary>
    public static class FirstRun
    {
        /// <summary>
        /// True for a genuinely new save.
        ///
        /// The extra conditions are for the update case: a player who has been
        /// playing for weeks gets a build with this screen in it, and their
        /// save has never set the flag simply because the flag did not exist.
        /// Interrogating them about settings they already chose would be the
        /// software equivalent of asking a regular their name.
        /// </summary>
        public static bool NeedsSetup(SaveData data)
            => !data.setupSeen
               && !data.tutorialSeen
               && data.days.Count == 0
               && data.practicePlayed == 0
               && data.journeyStars.Count == 0;
    }
}
