using PuttSeed.Core.FixedMath;

namespace PuttSeed.Core.Sim
{
    /// <summary>
    /// The deterministic mini-golf simulation. Advances in fixed 1/120 s ticks;
    /// all math is Q32.32 fixed point, so identical inputs produce bit-identical
    /// states on every device. The Unity layer only calls <see cref="Shoot"/>
    /// with quantized input and <see cref="Tick"/> at the fixed rate, and reads
    /// snapshots for rendering.
    /// </summary>
    public sealed class GolfSim
    {
        private readonly CourseData _course;
        private readonly SimConfig _config;

        private Vec2Fix _position;
        private Vec2Fix _velocity;

        /// <summary>Current ball state snapshot.</summary>
        public BallState Ball => new BallState(_position, _velocity);

        /// <summary>Number of ticks advanced since construction.</summary>
        public int TickCount { get; private set; }

        /// <summary>Strokes played so far.</summary>
        public int Strokes { get; private set; }

        /// <summary>Creates a simulation for one course.</summary>
        public GolfSim(CourseData course, SimConfig config)
        {
            _course = course;
            _config = config;
            _position = course.StartPosition;
            _velocity = Vec2Fix.Zero;
        }

        /// <summary>
        /// Plays a stroke: sets the ball velocity from the quantized angle and
        /// power. Speed scales linearly: <c>max * (power+1)/256</c>.
        /// </summary>
        public void Shoot(ShotInput shot)
        {
            var speed = _config.MaxShotSpeed * Fix64.FromFraction(shot.PowerIndex + 1, 256);
            _velocity = FixTrig.UnitVector(shot.AngleIndex) * speed;
            Strokes++;
        }

        /// <summary>
        /// Advances the simulation by one fixed 1/120 s step: exponential
        /// rolling-friction damping, then position integration (semi-implicit).
        /// </summary>
        public void Tick()
        {
            _velocity *= _config.RollDamping;
            _position += _velocity * _config.Dt;
            TickCount++;
        }
    }
}
