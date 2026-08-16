#nullable enable
using PuttSeed.Core.CourseGen;

namespace PuttSeed.Unity
{
    /// <summary>
    /// What the menu hands to the game scene: the chosen mode and its
    /// parameters. Static because it must survive the scene load; written only
    /// by the menu (and the editor test hook), read once by GameBootstrap.
    /// </summary>
    public static class GameSession
    {
        /// <summary>Mode chosen in the menu (defaults to Daily so the Game scene runs standalone).</summary>
        public static GameMode Mode = GameMode.Daily;

        /// <summary>Practice difficulty bucket.</summary>
        public static Difficulty PracticeDifficulty = Difficulty.Normal;

        /// <summary>Tutorial stage to start at.</summary>
        public static int TutorialIndex;

        /// <summary>Editor testing: load this exact seed instead of the mode.</summary>
        public static bool UseFixedSeed;

        /// <summary>The fixed seed when <see cref="UseFixedSeed"/> is set.</summary>
        public static ulong FixedSeed = 1;
    }
}
