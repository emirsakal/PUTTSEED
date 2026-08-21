#nullable enable
using System.Collections.Generic;

namespace PuttSeed.Unity
{
    /// <summary>One local achievement (no backend, no accounts — GDD rules).</summary>
    public sealed class AchievementDef
    {
        /// <summary>Stable id stored in the save file.</summary>
        public readonly string Id;

        /// <summary>Short display name.</summary>
        public readonly string Title;

        /// <summary>One-line unlock condition.</summary>
        public readonly string Detail;

        public AchievementDef(string id, string title, string detail)
        {
            Id = id;
            Title = title;
            Detail = detail;
        }
    }

    /// <summary>
    /// The achievement catalog and its evaluator. Pure logic over SaveData and
    /// run facts so every rule is EditMode-testable; unlock persistence lives
    /// in <see cref="StatsStore.Unlock"/>.
    /// </summary>
    public static class Achievements
    {
        /// <summary>
        /// What a finished run looks like to the achievement rules. Grouping
        /// these beats growing the evaluator's parameter list: the sim already
        /// measures every one exactly, so detection is a read, never a guess.
        /// </summary>
        public readonly struct RunFacts
        {
            /// <summary>The mode the run was played in.</summary>
            public readonly GameMode Mode;

            /// <summary>True when the daily was a past day from the archive.</summary>
            public readonly bool IsArchiveDay;

            /// <summary>Strokes taken to hole out.</summary>
            public readonly int Strokes;

            /// <summary>The course par.</summary>
            public readonly int Par;

            /// <summary>Strokes the run was allowed (par + allowance).</summary>
            public readonly int StrokeLimit;

            /// <summary>Wall bounces over the whole run.</summary>
            public readonly int WallHits;

            /// <summary>Wall bounces on the shot that holed out.</summary>
            public readonly int WallHitsFinalShot;

            /// <summary>True when the ball met a bumper, water, sand or ice.</summary>
            public readonly bool TouchedHazard;

            /// <summary>True when the course carried at least one windmill.</summary>
            public readonly bool HasWindmill;

            /// <summary>Windmill blade bounces over the whole run.</summary>
            public readonly int WindmillHits;

            public RunFacts(GameMode mode, bool isArchiveDay, int strokes, int par,
                int strokeLimit, int wallHits, int wallHitsFinalShot, bool touchedHazard,
                bool hasWindmill, int windmillHits)
            {
                Mode = mode;
                IsArchiveDay = isArchiveDay;
                Strokes = strokes;
                Par = par;
                StrokeLimit = strokeLimit;
                WallHits = wallHits;
                WallHitsFinalShot = wallHitsFinalShot;
                TouchedHazard = touchedHazard;
                HasWindmill = hasWindmill;
                WindmillHits = windmillHits;
            }
        }

        /// <summary>All achievements, in display order.</summary>
        /// <summary>Days in a row for "Seven Days".</summary>
        public const int StreakDays = 7;

        /// <summary>Different dailies completed for "Regular".</summary>
        public const int DailyCount = 10;

        /// <summary>Three-star dailies for "Perfectionist".</summary>
        public const int ThreeStarDays = 10;

        /// <summary>Practice courses played for "Range Rat".</summary>
        public const int PracticeCount = 25;

        public static readonly AchievementDef[] All =
        {
            new AchievementDef("first_hole", "First Putt", "hole out for the first time"),
            new AchievementDef("ace", "Ace", "hole in one"),
            new AchievementDef("no_walls", "Clean Strike", "hole out without touching a wall"),
            new AchievementDef("three_star", "Three Stars", "earn three stars on a daily"),
            new AchievementDef("streak7", "Seven Days", "reach a 7-day streak"),
            new AchievementDef("dailies10", "Regular", "complete 10 different dailies"),
            new AchievementDef("archive1", "Time Traveler", "complete an archive day"),
            new AchievementDef("practice25", "Range Rat", "play 25 practice courses"),

            // The 2026-08-18 wave. Every rule reads a measurement the sim
            // already makes, so none of them can fire by accident.
            new AchievementDef("bank_shot", "Bank Shot", "hole out on a shot off three walls"),
            new AchievementDef("untouched", "Untouched", "hole out without touching a hazard"),
            new AchievementDef("millwright", "Millwright", "hole out on a windmill course, blades untouched"),
            new AchievementDef("last_stroke", "Down to the Wire", "hole out on your final allowed stroke"),
            new AchievementDef("three_star_10", "Perfectionist", "earn three stars on 10 dailies"),
        };

        /// <summary>Catalog lookup by id (null for unknown ids in old saves).</summary>
        public static AchievementDef? Find(string id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Which achievements a just-holed run newly earns. Call AFTER the run
        /// was recorded so streak/day counts include it; already-unlocked ids
        /// are never returned.
        /// </summary>
        public static List<string> EvaluateRun(SaveData data, in RunFacts run)
        {
            var earned = new List<string>();
            void Earn(string id)
            {
                if (!data.achievements.Contains(id))
                {
                    earned.Add(id);
                }
            }

            Earn("first_hole");
            if (run.Strokes == 1)
            {
                Earn("ace");
            }

            if (run.WallHits == 0)
            {
                Earn("no_walls");
            }

            if (run.WallHitsFinalShot >= 3)
            {
                Earn("bank_shot");
            }

            if (!run.TouchedHazard)
            {
                Earn("untouched");
            }

            if (run.HasWindmill && run.WindmillHits == 0)
            {
                Earn("millwright");
            }

            if (run.Strokes == run.StrokeLimit)
            {
                Earn("last_stroke");
            }

            if (run.Mode == GameMode.Daily)
            {
                // Was "strokes < par", which on the par-2 courses generation
                // actually makes was the same condition as Ace — two of eight
                // achievements firing together, forever. It now means what its
                // id always said, and defers to the one scoring rule.
                if (PuttSeed.Core.Sim.Scoring.Stars(run.Strokes, run.Par) == 3)
                {
                    Earn("three_star");
                }

                if (run.IsArchiveDay)
                {
                    Earn("archive1");
                }
            }

            if (ThreeStarDayCount(data) >= ThreeStarDays)
            {
                Earn("three_star_10");
            }

            if (data.streak >= StreakDays)
            {
                Earn("streak7");
            }

            if (CompletedDailyCount(data) >= DailyCount)
            {
                Earn("dailies10");
            }

            if (data.practicePlayed >= PracticeCount)
            {
                Earn("practice25");
            }

            return earned;
        }

        /// <summary>Distinct days whose FIRST finish earned all three stars.</summary>
        public static int ThreeStarDayCount(SaveData data)
        {
            int count = 0;
            for (int i = 0; i < data.days.Count; i++)
            {
                if (data.days[i].completed && data.days[i].firstStars >= 3)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Distinct completed daily days in the save.</summary>
        public static int CompletedDailyCount(SaveData data)
        {
            int count = 0;
            for (int i = 0; i < data.days.Count; i++)
            {
                if (data.days[i].completed)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
