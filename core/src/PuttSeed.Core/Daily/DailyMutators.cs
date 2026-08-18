using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Daily
{
    /// <summary>A themed day's twist on the ordinary rules.</summary>
    public enum DailyMutator
    {
        /// <summary>The ordinary game.</summary>
        None,

        /// <summary>Low friction everywhere: the whole green plays like ice.</summary>
        Icy,

        /// <summary>Bumpers kick noticeably harder.</summary>
        Bouncy,

        /// <summary>A steady crosswind pushes the ball off line.</summary>
        Windy,
    }

    /// <summary>
    /// Rare themed days, derived from the seed alone. This is the cheapest
    /// content in the game: no new geometry, no new art — the existing physics
    /// knobs, turned. Because the twist is a function of the seed, a replay
    /// code reproduces it for free, and the generator proves the course
    /// solvable UNDER the twist, so a windy day is never an unfair day.
    ///
    /// Gated on generator version 2: version 1 regenerates Journey levels and
    /// every archived daily, and a mutator there would rewrite finished
    /// history.
    /// </summary>
    public static class DailyMutators
    {
        /// <summary>
        /// One day in eighteen is themed, split evenly between the three
        /// kinds. Rare enough that the game a player learns is the plain one.
        /// </summary>
        private const int Buckets = 18;

        /// <summary>The twist a seed carries, or <see cref="DailyMutator.None"/>.</summary>
        public static DailyMutator ForSeed(ulong seed, int configVersion)
        {
            if (configVersion < 2)
            {
                return DailyMutator.None;
            }

            // A salted SplitMix64 pass, so the twist does not correlate with
            // the layout the same seed grows.
            ulong state = seed ^ 0x4D55544154455221UL;
            ulong roll = FixRng.SplitMix64(ref state);
            return (roll % Buckets) switch
            {
                0 => DailyMutator.Icy,
                1 => DailyMutator.Bouncy,
                2 => DailyMutator.Windy,
                _ => DailyMutator.None,
            };
        }

        /// <summary>
        /// The physics a seed actually plays under. Returns the base config
        /// unchanged on a plain day, so nothing is rebuilt and nothing shifts.
        /// </summary>
        public static SimConfig Apply(SimConfig baseConfig, ulong seed, int configVersion)
        {
            switch (ForSeed(seed, configVersion))
            {
                case DailyMutator.Icy:
                    // Toward the ice constant without reaching it: the green is
                    // slick, but a putt still dies rather than running forever.
                    return baseConfig.WithRollDamping(Fix64.FromFraction(995, 1000));

                case DailyMutator.Bouncy:
                    return baseConfig.WithBumperRestitution(Fix64.FromFraction(16, 10));

                case DailyMutator.Windy:
                    return baseConfig.WithWind(WindForSeed(seed));

                default:
                    return baseConfig;
            }
        }

        /// <summary>
        /// The day's wind: a fixed strength on one of the sixteen compass
        /// points the angle table already gives exactly.
        /// </summary>
        private static Vec2Fix WindForSeed(ulong seed)
        {
            ulong state = seed ^ 0x57494E4421212121UL;
            ulong roll = FixRng.SplitMix64(ref state);
            int angleIndex = (int)(roll % 16) * (FixTrig.AngleSteps / 16);

            // Gentle: about a fifteenth of gravity, enough to bend a long roll
            // without stealing the shot from the player.
            return FixTrig.UnitVector(angleIndex) * Fix64.FromFraction(65, 100);
        }
    }
}
