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
        /// <summary>All achievements, in display order.</summary>
        public static readonly AchievementDef[] All =
        {
            new AchievementDef("first_hole", "First Putt", "hole out for the first time"),
            new AchievementDef("ace", "Ace", "hole in one"),
            new AchievementDef("no_walls", "Clean Strike", "hole out without touching a wall"),
            new AchievementDef("three_star", "Under Par", "finish a daily under par"),
            new AchievementDef("streak7", "Seven Days", "reach a 7-day streak"),
            new AchievementDef("dailies10", "Regular", "complete 10 different dailies"),
            new AchievementDef("archive1", "Time Traveler", "complete an archive day"),
            new AchievementDef("practice25", "Range Rat", "play 25 practice courses"),
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
        public static List<string> EvaluateRun(SaveData data, GameMode mode, bool isArchiveDay,
            int strokes, int par, int wallHits)
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
            if (strokes == 1)
            {
                Earn("ace");
            }

            if (wallHits == 0)
            {
                Earn("no_walls");
            }

            if (mode == GameMode.Daily)
            {
                if (strokes < par)
                {
                    Earn("three_star");
                }

                if (isArchiveDay)
                {
                    Earn("archive1");
                }
            }

            if (data.streak >= 7)
            {
                Earn("streak7");
            }

            if (CompletedDailyCount(data) >= 10)
            {
                Earn("dailies10");
            }

            if (data.practicePlayed >= 25)
            {
                Earn("practice25");
            }

            return earned;
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
