namespace PuttSeed.Core.FixedMath
{
    /// <summary>
    /// Fixed-point trigonometry via a committed 1024-entry Q32.32 sine table.
    /// Aim angles enter the simulation as integer table indices (a full turn is
    /// <see cref="AngleSteps"/> steps), so no runtime trigonometry is needed and
    /// results are bit-identical everywhere. Cosine is the same table read a
    /// quarter turn ahead.
    /// </summary>
    public static class FixTrig
    {
        /// <summary>Number of angle steps in a full turn (table size).</summary>
        public const int AngleSteps = 1024;

        private const int IndexMask = AngleSteps - 1;

        /// <summary>Sine of the angle <c>2*pi*index/1024</c>; index wraps modulo 1024.</summary>
        public static Fix64 Sin(int angleIndex)
            => Fix64.FromRaw(FixTrigTable.Sin[angleIndex & IndexMask]);

        /// <summary>Cosine of the angle <c>2*pi*index/1024</c>; index wraps modulo 1024.</summary>
        public static Fix64 Cos(int angleIndex)
            => Sin(angleIndex + AngleSteps / 4);

        /// <summary>Unit direction vector for an angle index (Y-up convention).</summary>
        public static Vec2Fix UnitVector(int angleIndex)
            => new Vec2Fix(Cos(angleIndex), Sin(angleIndex));
    }
}
