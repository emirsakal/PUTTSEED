#nullable enable
using System;

namespace PuttSeed.Unity
{
    /// <summary>The presentation effects a celebration is made of.</summary>
    [Flags]
    public enum MotionEffect
    {
        /// <summary>Nothing.</summary>
        None = 0,

        /// <summary>Camera shake on impact.</summary>
        Shake = 1,

        /// <summary>The slow-motion replay of the winning putt.</summary>
        SlowMo = 2,

        /// <summary>The cinematic bars that ride with it.</summary>
        Letterbox = 4,

        /// <summary>Three-star confetti.</summary>
        Confetti = 8,

        /// <summary>Water splash — tells the player they lost a stroke.</summary>
        Splash = 16,

        /// <summary>Sand puff — tells the player the ground changed.</summary>
        Puff = 32,

        /// <summary>The star reveal — tells the player what they scored.</summary>
        StarReveal = 64,

        /// <summary>The near-miss camera tighten.</summary>
        CameraPush = 128,
    }

    /// <summary>
    /// What the player's motion setting allows. The split is not "big effects
    /// versus small" but INFORMATIONAL versus decorative: a splash says a
    /// stroke was lost and a puff says the ground changed, so both survive
    /// reduced motion, while shake, slow-motion, letterbox and confetti only
    /// ever said "well done" and can say it without moving the screen. The
    /// near miss says it with a sound and a ring under reduced motion, and
    /// keeps its camera tighten to itself.
    ///
    /// It lives here rather than as four ifs in the feedback controller so the
    /// promise can be tested: exactly these four, and nothing else.
    /// </summary>
    public static class MotionSettings
    {
        /// <summary>The effects a reduced-motion player does not get.</summary>
        public const MotionEffect Calming =
            MotionEffect.Shake | MotionEffect.SlowMo | MotionEffect.Letterbox
            | MotionEffect.Confetti | MotionEffect.CameraPush;

        /// <summary>Whether an effect may play under the current setting.</summary>
        public static bool Allows(MotionEffect effect, bool reducedMotion)
            => !reducedMotion || (Calming & effect) == MotionEffect.None;
    }
}
