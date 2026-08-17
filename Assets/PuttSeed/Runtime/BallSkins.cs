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

        /// <summary>Achievement id that unlocks it (null = always available).</summary>
        public readonly string? RequiredAchievement;

        public BallSkinDef(string id, string name, Color color, string? requiredAchievement)
        {
            Id = id;
            Name = name;
            Color = color;
            RequiredAchievement = requiredAchievement;
        }
    }

    /// <summary>
    /// The cosmetic catalog: achievement-gated ball colors (GDD rules — no
    /// IAP, no accounts; unlocks live in the local save). Pure logic, so the
    /// cycle/unlock rules are EditMode-testable.
    /// </summary>
    public static class BallSkins
    {
        /// <summary>All skins in cycle order; the first is always unlocked.</summary>
        public static readonly BallSkinDef[] All =
        {
            new BallSkinDef("cream", "Cream", new Color(0.97f, 0.97f, 0.95f), null),
            new BallSkinDef("amber", "Amber", new Color(0.99f, 0.80f, 0.38f), "three_star"),
            new BallSkinDef("rose", "Rose", new Color(0.98f, 0.62f, 0.66f), "ace"),
            new BallSkinDef("mint", "Mint", new Color(0.62f, 0.92f, 0.70f), "dailies10"),
            new BallSkinDef("sky", "Sky", new Color(0.60f, 0.82f, 0.99f), "streak7"),
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

        /// <summary>True when the save has earned the skin.</summary>
        public static bool IsUnlocked(BallSkinDef skin, SaveData data)
            => skin.RequiredAchievement == null
               || data.achievements.Contains(skin.RequiredAchievement);

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
