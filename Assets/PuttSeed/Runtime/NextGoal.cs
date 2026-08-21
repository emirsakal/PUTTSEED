#nullable enable
using System.Collections.Generic;

namespace PuttSeed.Unity
{
    /// <summary>Where a goal is finished, so tapping the line goes somewhere useful.</summary>
    public enum GoalPanel
    {
        /// <summary>The stats and achievements panel.</summary>
        Stats,

        /// <summary>The journey level grid.</summary>
        Journey,

        /// <summary>The collection of skins and trails.</summary>
        Collection,
    }

    /// <summary>The nearest thing the player has left to finish.</summary>
    public readonly struct Goal
    {
        /// <summary>How many of whatever it counts are still to go.</summary>
        public readonly int Remaining;

        /// <summary>The line as the menu shows it, localized.</summary>
        public readonly string Text;

        /// <summary>The panel that shows progress toward it.</summary>
        public readonly GoalPanel Panel;

        /// <summary>Creates a goal.</summary>
        public Goal(int remaining, string text, GoalPanel panel)
        {
            Remaining = remaining;
            Text = text;
            Panel = panel;
        }
    }

    /// <summary>
    /// The one goal the player is closest to, for the menu's footer line.
    ///
    /// The chip used to read "Streak 2 · Today: 34 attempt(s) · Practice: 39"
    /// — three unrelated numbers, none of which is a goal, while thirteen
    /// achievements and ten skins sat behind panels the menu never pointed at.
    /// One line, the nearest thing, and a tap that opens where it lives.
    ///
    /// Only COUNTABLE goals qualify. "Hole in one" is a fine achievement and a
    /// terrible next goal: there is no number of anything that brings it
    /// closer, and a line promising one would be noise.
    /// </summary>
    public static class NextGoal
    {
        /// <summary>
        /// The nearest unfinished goal, or null when every countable one is
        /// done — the menu falls back to its streak line there, which is the
        /// right thing to say to somebody who has finished everything.
        /// </summary>
        public static Goal? For(SaveData data)
        {
            var goals = new List<Goal>();

            // Skins gated on the journey are read from the catalog rather than
            // listed here, so a new skin brings its own goal with it.
            foreach (var skin in BallSkins.All)
            {
                if (BallSkins.IsUnlocked(skin, data))
                {
                    continue;
                }

                string reward = string.Format(Loc.Tr("{0} ball"), Loc.Tr(skin.Name));
                if (skin.RequiredJourneyLevel > 0)
                {
                    Add(goals, skin.RequiredJourneyLevel - data.journeyStars.Count,
                        "level", "levels", reward, GoalPanel.Journey);
                }
                else if (skin.RequiredJourneyStars > 0)
                {
                    Add(goals, skin.RequiredJourneyStars - TotalJourneyStars(data),
                        "star", "stars", reward, GoalPanel.Collection);
                }
            }

            // Achievements that count something. The thresholds are the same
            // constants the achievement rules test against, so this line cannot
            // promise a number the rule does not honour.
            AddAchievement(goals, data, "streak7",
                Achievements.StreakDays - data.streak, "day", "days");
            AddAchievement(goals, data, "dailies10",
                Achievements.DailyCount - Achievements.CompletedDailyCount(data), "daily", "dailies");
            AddAchievement(goals, data, "three_star_10",
                Achievements.ThreeStarDays - Achievements.ThreeStarDayCount(data),
                "three-star daily", "three-star dailies");
            AddAchievement(goals, data, "practice25",
                Achievements.PracticeCount - data.practicePlayed, "practice course", "practice courses");

            if (goals.Count == 0)
            {
                return null;
            }

            var nearest = goals[0];
            for (int i = 1; i < goals.Count; i++)
            {
                if (goals[i].Remaining < nearest.Remaining)
                {
                    nearest = goals[i];
                }
            }

            return nearest;
        }

        private static void AddAchievement(List<Goal> goals, SaveData data, string id,
            int remaining, string one, string many)
        {
            if (data.achievements.Contains(id))
            {
                return;
            }

            var def = Achievements.Find(id);
            Add(goals, remaining, one, many, Loc.Tr(def?.Title ?? id), GoalPanel.Stats);
        }

        private static void Add(List<Goal> goals, int remaining, string one, string many,
            string reward, GoalPanel panel)
        {
            if (remaining <= 0)
            {
                return; // earned, or earned in all but the bookkeeping
            }

            // One template, so a language that puts the reward first can say
            // so: Turkish reads "Mercan top icin 2 seviye daha".
            string text = string.Format(Loc.Tr("{0} more {1} → {2}"),
                remaining, Loc.Tr(remaining == 1 ? one : many), reward);
            goals.Add(new Goal(remaining, text, panel));
        }

        private static int TotalJourneyStars(SaveData data)
        {
            int total = 0;
            for (int i = 0; i < data.journeyStars.Count; i++)
            {
                total += data.journeyStars[i];
            }

            return total;
        }
    }
}
