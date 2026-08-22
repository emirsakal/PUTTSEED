#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>How a trail is drawn — colour was the only axis until 2026-08.</summary>
    public enum TrailStyle
    {
        /// <summary>The classic ribbon (a TrailRenderer).</summary>
        Ribbon,

        /// <summary>Short, wide and bright at the head: a shooting star.</summary>
        Comet,

        /// <summary>No ribbon at all — small circles shed along the path.</summary>
        Bubbles,
    }

    /// <summary>One cosmetic ball trail, gated like the skins (no IAP — GDD).</summary>
    public sealed class BallTrailDef
    {
        /// <summary>Stable id stored in the save file.</summary>
        public readonly string Id;

        /// <summary>Chip label.</summary>
        public readonly string Name;

        /// <summary>The trail's base tint (surface cues still override it).</summary>
        public readonly Color Color;

        /// <summary>Achievement id that unlocks it (null = no achievement gate).</summary>
        public readonly string? RequiredAchievement;

        /// <summary>Total journey stars required (0 = none).</summary>
        public readonly int RequiredJourneyStars;

        /// <summary>How the trail is drawn (colour is the other axis).</summary>
        public readonly TrailStyle Style;

        public BallTrailDef(string id, string name, Color color, string? requiredAchievement,
            int requiredJourneyStars = 0, TrailStyle style = TrailStyle.Ribbon)
        {
            Id = id;
            Name = name;
            Color = color;
            RequiredAchievement = requiredAchievement;
            RequiredJourneyStars = requiredJourneyStars;
            Style = style;
        }
    }

    /// <summary>
    /// The trail catalog. Trails carry the campaign's long tail: the star
    /// gates step 100 / 200 / 300 across the journey's 300, so the last one is
    /// a genuine completionist reward rather than another mid-run trinket.
    /// Pure logic, so every gate is EditMode-testable.
    /// </summary>
    public static class BallTrails
    {
        /// <summary>All trails in display order; the first is always unlocked.</summary>
        public static readonly BallTrailDef[] All =
        {
            new BallTrailDef("plain", "Classic", new Color(1f, 1f, 1f, 0.5f), null),
            new BallTrailDef("spark", "Spark", new Color(1f, 0.86f, 0.45f, 0.6f), "bank_shot"),
            new BallTrailDef("frost", "Frost", new Color(0.66f, 0.9f, 1f, 0.6f), "untouched"),
            new BallTrailDef("blaze", "Blaze", new Color(1f, 0.5f, 0.24f, 0.6f), null,
                requiredJourneyStars: 100),
            new BallTrailDef("aurora", "Aurora", new Color(0.55f, 1f, 0.78f, 0.6f), null,
                requiredJourneyStars: 200),
            new BallTrailDef("prism", "Prism", new Color(0.82f, 0.68f, 1f, 0.7f), null,
                requiredJourneyStars: 300),

            // The 2026-08 pair opened a second axis: SHAPE. Colour variants
            // ask "which tint"; these ask "which kind of thing follows the
            // ball" — and their gates reuse achievements that until now
            // rewarded nothing but a jingle.
            new BallTrailDef("comet", "Comet", new Color(1f, 0.93f, 0.75f, 0.75f), "last_stroke",
                style: TrailStyle.Comet),
            new BallTrailDef("bubbles", "Bubbles", new Color(0.85f, 0.95f, 1f, 0.6f), "archive1",
                style: TrailStyle.Bubbles),
        };

        /// <summary>The trail for an id (unknown ids fall back to the default).</summary>
        public static BallTrailDef Resolve(string id)
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

        /// <summary>True when the save has earned the trail (all gates pass).</summary>
        public static bool IsUnlocked(BallTrailDef trail, SaveData data)
            => (trail.RequiredAchievement == null
                || data.achievements.Contains(trail.RequiredAchievement))
               && TotalJourneyStars(data) >= trail.RequiredJourneyStars;

        /// <summary>Localized one-line unlock hint for a locked trail.</summary>
        public static string UnlockHint(BallTrailDef trail)
        {
            if (trail.RequiredAchievement != null)
            {
                return Loc.Tr(Achievements.Find(trail.RequiredAchievement)?.Detail ?? "");
            }

            if (trail.RequiredJourneyStars > 0)
            {
                return string.Format(Loc.Tr("earn {0} journey stars"), trail.RequiredJourneyStars);
            }

            return "";
        }

        /// <summary>How many trails the save has unlocked.</summary>
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
