#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>One cosmetic ball color, gated by a local achievement.</summary>
    public sealed class BallSkinDef
    {
        /// <summary>Stable id stored in the save file.</summary>
        public readonly string Id;

        /// <summary>Chip label.</summary>
        public readonly string Name;

        /// <summary>The ball disc color.</summary>
        public readonly Color Color;

        /// <summary>Achievement id that unlocks it (null = no achievement gate).</summary>
        public readonly string? RequiredAchievement;

        /// <summary>Journey level (1-based) whose completion unlocks it (0 = none).</summary>
        public readonly int RequiredJourneyLevel;

        /// <summary>Total journey stars required (0 = none).</summary>
        public readonly int RequiredJourneyStars;

        public BallSkinDef(string id, string name, Color color, string? requiredAchievement,
            int requiredJourneyLevel = 0, int requiredJourneyStars = 0)
        {
            Id = id;
            Name = name;
            Color = color;
            RequiredAchievement = requiredAchievement;
            RequiredJourneyLevel = requiredJourneyLevel;
            RequiredJourneyStars = requiredJourneyStars;
        }
    }

    /// <summary>
    /// The cosmetic catalog: achievement-gated ball colors (GDD rules — no
    /// IAP, no accounts; unlocks live in the local save). Pure logic, so the
    /// cycle/unlock rules are EditMode-testable.
    /// </summary>
    public static class BallSkins
    {
        /// <summary>All skins in display order; the first is always unlocked.</summary>
        public static readonly BallSkinDef[] All =
        {
            new BallSkinDef("cream", "Cream", new Color(0.97f, 0.97f, 0.95f), null),
            new BallSkinDef("amber", "Amber", new Color(0.99f, 0.80f, 0.38f), "three_star"),
            new BallSkinDef("rose", "Rose", new Color(0.98f, 0.62f, 0.66f), "ace"),
            new BallSkinDef("mint", "Mint", new Color(0.62f, 0.92f, 0.70f), "dailies10"),
            new BallSkinDef("sky", "Sky", new Color(0.60f, 0.82f, 0.99f), "streak7"),
            new BallSkinDef("lime", "Lime", new Color(0.74f, 0.95f, 0.42f), null, requiredJourneyLevel: 5),
            new BallSkinDef("coral", "Coral", new Color(1f, 0.55f, 0.40f), null, requiredJourneyLevel: 10),
            new BallSkinDef("violet", "Violet", new Color(0.74f, 0.62f, 0.98f), null, requiredJourneyLevel: 25),
            new BallSkinDef("ember", "Ember", new Color(0.94f, 0.42f, 0.22f), null, requiredJourneyStars: 75),
            new BallSkinDef("gold", "Gold", new Color(1f, 0.85f, 0.22f), null, requiredJourneyLevel: 50),
        };

        /// <summary>The skin for an id (unknown ids fall back to the default).</summary>
        public static BallSkinDef Resolve(string id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return All[0];
        }

        /// <summary>True when the save has earned the skin (all gates pass).</summary>
        public static bool IsUnlocked(BallSkinDef skin, SaveData data)
            => (skin.RequiredAchievement == null
                || data.achievements.Contains(skin.RequiredAchievement))
               && data.journeyStars.Count >= skin.RequiredJourneyLevel
               && TotalJourneyStars(data) >= skin.RequiredJourneyStars;

        /// <summary>Localized one-line unlock hint for a locked skin.</summary>
        public static string UnlockHint(BallSkinDef skin)
        {
            if (skin.RequiredAchievement != null)
            {
                return Loc.Tr(Achievements.Find(skin.RequiredAchievement)?.Detail ?? "");
            }

            if (skin.RequiredJourneyLevel > 0)
            {
                return string.Format(Loc.Tr("complete journey level {0}"), skin.RequiredJourneyLevel);
            }

            if (skin.RequiredJourneyStars > 0)
            {
                return string.Format(Loc.Tr("earn {0} journey stars"), skin.RequiredJourneyStars);
            }

            return "";
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

        /// <summary>The next unlocked skin after the current one (wraps).</summary>
        public static BallSkinDef NextUnlocked(string currentId, SaveData data)
        {
            int start = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == currentId)
                {
                    start = i;
                    break;
                }
            }

            for (int step = 1; step <= All.Length; step++)
            {
                var candidate = All[(start + step) % All.Length];
                if (IsUnlocked(candidate, data))
                {
                    return candidate;
                }
            }

            return All[0];
        }

        /// <summary>How many skins the save has unlocked.</summary>
        public static int UnlockedCount(SaveData data)
        {
            int count = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (IsUnlocked(All[i], data))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
