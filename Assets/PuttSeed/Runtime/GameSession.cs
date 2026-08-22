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

        /// <summary>Archive pick: the past day number to play, -1 for today.</summary>
        public static int ArchiveDayNumber = -1;

        /// <summary>
        /// A replay code the menu found on the clipboard and the player chose
        /// to open — the game scene imports it directly instead of loading a
        /// daily first and swapping it out.
        /// </summary>
        public static string? PendingReplayCode;

        /// <summary>
        /// The clipboard code already offered (and taken) on the menu, so the
        /// game scene does not offer the same one a second time.
        /// </summary>
        public static string? ConsumedClipboardCode;

        /// <summary>Journey level to start at (0-based).</summary>
        public static int JourneyLevel;

        /// <summary>Gauntlet week to run (-1 = none).</summary>
        public static int GauntletWeekIndex = -1;

        /// <summary>Editor testing: load this exact seed instead of the mode.</summary>
        public static bool UseFixedSeed;

        /// <summary>The fixed seed when <see cref="UseFixedSeed"/> is set.</summary>
        public static ulong FixedSeed = 1;

        /// <summary>Generator config version for the fixed seed (default v1).</summary>
        public static int FixedSeedConfigVersion = 1;

        /// <summary>Seed of the course the menu prepared, if any.</summary>
        public static ulong PreparedSeed;

        /// <summary>Generator version the prepared course was grown under.</summary>
        public static int PreparedVersion = -1;

        /// <summary>
        /// A course the MENU already generated — for the thumbnail on the daily
        /// card — handed forward so the game scene does not grow the very same
        /// hole a second time. It used to: two identical generations per launch,
        /// which cost nothing worth noticing at 70 ms a course and costs
        /// seconds once a hole can be worth three strokes.
        ///
        /// Seed and generator version determine a course completely: the day's
        /// mutator is a pure function of both, and the feel asset ships inside
        /// the build. A matching key is therefore the same course, not a
        /// probably-similar one.
        /// </summary>
        public static PuttSeed.Core.CourseGen.GenerationResult? PreparedCourse;

        /// <summary>The bucket the prepared practice course was grown for.</summary>
        public static PuttSeed.Core.CourseGen.Difficulty PreparedPracticeBucket;

        /// <summary>
        /// A practice course the menu grew while the player was still deciding.
        /// A search is up to eight generations; without this the first course
        /// of a session sits behind the whole search, and only the first —
        /// after that the game scene grows the next one during play.
        /// </summary>
        public static PracticeCourses.Candidate? PreparedPractice;

        /// <summary>Takes the prepared practice course if its bucket matches.</summary>
        public static PracticeCourses.Candidate? TakePreparedPractice(
            PuttSeed.Core.CourseGen.Difficulty want)
        {
            if (PreparedPractice == null || PreparedPracticeBucket != want)
            {
                return null;
            }

            var prepared = PreparedPractice;
            PreparedPractice = null;
            return prepared;
        }

        /// <summary>
        /// Takes the prepared course if it is the one being asked for. Single
        /// use: the menu prepares one on every visit, and a stale one must
        /// never outlive the day it belongs to.
        /// </summary>
        public static PuttSeed.Core.CourseGen.GenerationResult? TakePrepared(ulong seed, int version)
        {
            if (PreparedCourse == null || PreparedSeed != seed || PreparedVersion != version)
            {
                return null;
            }

            var prepared = PreparedCourse;
            PreparedCourse = null;
            return prepared;
        }
    }
}
