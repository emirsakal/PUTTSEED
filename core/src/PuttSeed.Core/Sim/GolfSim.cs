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
        /// rolling-friction damping, then sub-stepped position integration with
        /// wall collision resolution (semi-implicit Euler).
        /// </summary>
        public void Tick()
        {
            _velocity *= _config.RollDamping;

            // Split the tick so no sub-step moves farther than the anti-tunneling
            // limit (a fraction of the ball radius). Integer sub-step count keeps
            // this fully deterministic.
            var travel = _velocity.Length() * _config.Dt;
            int subSteps = 1;
            if (travel > _config.MaxTravelPerSubStep)
            {
                subSteps = (travel / _config.MaxTravelPerSubStep).ToInt() + 1;
            }

            var dtSub = _config.Dt / Fix64.FromInt(subSteps);
            for (int i = 0; i < subSteps; i++)
            {
                _position += _velocity * dtSub;
                ResolveWallCollisions();
            }

            TickCount++;
        }

        /// <summary>
        /// Pushes the ball out of any wall it penetrates and reflects the normal
        /// velocity component with restitution; tangential component is kept.
        /// </summary>
        private void ResolveWallCollisions()
        {
            var walls = _course.Walls;
            for (int i = 0; i < walls.Length; i++)
            {
                var a = walls[i].A;
                var ab = walls[i].B - a;

                // Closest point on the segment to the ball center.
                var abLenSq = ab.LengthSq();
                var t = abLenSq == Fix64.Zero
                    ? Fix64.Zero
                    : Fix64.Clamp(Vec2Fix.Dot(_position - a, ab) / abLenSq, Fix64.Zero, Fix64.One);
                var closest = a + ab * t;

                var delta = _position - closest;
                var distSq = delta.LengthSq();
                var radius = _config.BallRadius;
                if (distSq >= radius * radius)
                {
                    continue;
                }

                // Contact normal: from wall toward ball center. If the center sits
                // exactly on the segment, fall back to the segment perpendicular
                // facing against the velocity (deterministic tie-break).
                Vec2Fix normal;
                var dist = Fix64.Sqrt(distSq);
                if (dist > Fix64.Zero)
                {
                    normal = delta / dist;
                }
                else
                {
                    normal = ab.Perp() / ab.Length();
                    if (Vec2Fix.Dot(_velocity, normal) > Fix64.Zero)
                    {
                        normal = -normal;
                    }
                }

                // Positional correction: place the ball exactly on the surface.
                _position = closest + normal * radius;

                // Velocity response: reflect the approaching normal component.
                var vn = Vec2Fix.Dot(_velocity, normal);
                if (vn < Fix64.Zero)
                {
                    var bounce = Fix64.One + _config.WallRestitution;
                    _velocity -= normal * (bounce * vn);
                }
            }
        }
    }
}
