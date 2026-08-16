namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// A fully quantized shot: the ONLY external input the simulation accepts.
    /// Angle is a table index (0..1023, see <see cref="FixedMath.FixTrig"/>),
    /// power an index 0..255. Quantization happens at the boundary (in the
    /// Unity layer); core never sees raw analog input.
    /// </summary>
    public readonly struct ShotInput
    {
        /// <summary>Aim angle as a FixTrig table index (0..1023, wraps).</summary>
        public int AngleIndex { get; }

        /// <summary>Shot power index (0..255); speed scales linearly with index + 1.</summary>
        public int PowerIndex { get; }

        /// <summary>Creates a quantized shot. Angle wraps modulo 1024; power clamps to 0..255.</summary>
        public ShotInput(int angleIndex, int powerIndex)
        {
            AngleIndex = angleIndex & (FixedMath.FixTrig.AngleSteps - 1);
            PowerIndex = powerIndex < 0 ? 0 : powerIndex > 255 ? 255 : powerIndex;
        }
    }
}
