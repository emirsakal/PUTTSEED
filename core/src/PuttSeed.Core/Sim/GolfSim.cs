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
        private Vec2Fix _lastRestPosition;
        private int _restTicks;

        // Ticks since the current shot began — the windmill phase clock. Reset
        // by Shoot and RestoreRest, so dynamics never depend on absolute
        // TickCount (the solver's rest-state BFS and the timing-free replay
        // codec both rely on that). Deliberately NOT part of StateHash: it is
        // fully derived from the input stream the hash comparisons already fix.
        private int _ticksSinceShot;

        /// <summary>Current ball state snapshot.</summary>
        public BallState Ball => new BallState(_position, _velocity);

        /// <summary>Number of ticks advanced since construction.</summary>
        public int TickCount { get; private set; }

        /// <summary>
        /// Ticks since the current shot began — the windmill phase clock
        /// (frozen while at rest, reset by <see cref="Shoot"/> and
        /// <see cref="RestoreRest"/>). Exposed so the render layer can mirror
        /// blade angles exactly.
        /// </summary>
        public int TicksSinceShot => _ticksSinceShot;

        /// <summary>Strokes played so far.</summary>
        public int Strokes { get; private set; }

        /// <summary>
        /// True when the ball has been slower than the rest threshold for the
        /// required number of consecutive ticks (or has not been shot yet).
        /// Only an at-rest ball accepts the next shot.
        /// </summary>
        public bool IsAtRest { get; private set; }

        /// <summary>True once the ball has dropped into the hole; the sim is finished.</summary>
        public bool IsHoled { get; private set; }

        /// <summary>
        /// Strokes allowed over par before the run fails. The GDD default is
        /// 3; daily hard mode plays the same seed at 1. A rule, not a force —
        /// it never touches the physics, only whether a shot is accepted.
        /// </summary>
        public int StrokeAllowance { get; }

        /// <summary>Stroke limit: par + allowance. Shots beyond it are refused.</summary>
        public int StrokeLimit => _course.Par + StrokeAllowance;

        /// <summary>
        /// True when the run is over without a capture: the stroke limit is
        /// spent and the ball is at rest (GDD "course failed"; retry allowed).
        /// </summary>
        public bool IsFailed => !IsHoled && IsAtRest && Strokes >= StrokeLimit;

        /// <summary>
        /// Wall bounces so far. Presentation-facing observation (audio,
        /// haptics): deterministic, but deliberately NOT part of
        /// <see cref="StateHash"/> — it adds no dynamics information.
        /// </summary>
        public int WallHitCount { get; private set; }

        /// <summary>Bumper bounces so far (see <see cref="WallHitCount"/> caveat).</summary>
        public int BumperHitCount { get; private set; }

        /// <summary>Water resets so far (see <see cref="WallHitCount"/> caveat).</summary>
        public int WaterEntryCount { get; private set; }

        /// <summary>One-way gate blocks so far (see <see cref="WallHitCount"/> caveat).</summary>
        public int GateHitCount { get; private set; }

        /// <summary>Portal transits so far (see <see cref="WallHitCount"/> caveat).</summary>
        public int PortalTransitCount { get; private set; }

        /// <summary>Windmill blade bounces so far (see <see cref="WallHitCount"/> caveat).</summary>
        public int WindmillHitCount { get; private set; }

        /// <summary>Creates a simulation for one course.</summary>
        public GolfSim(CourseData course, SimConfig config, int strokeAllowance = 3)
        {
            _course = course;
            _config = config;
            StrokeAllowance = strokeAllowance;
            _position = course.StartPosition;
            _velocity = Vec2Fix.Zero;
            _lastRestPosition = course.StartPosition;
            IsAtRest = true;
        }

        /// <summary>
        /// Resets the simulation to an arbitrary rest state: ball at rest at
        /// <paramref name="position"/> with <paramref name="strokes"/> played,
        /// not holed. Solver support: lets the SolvabilityChecker expand a BFS
        /// node without replaying the whole shot sequence. Tick count is not
        /// reset (it does not affect future dynamics).
        /// </summary>
        public void RestoreRest(Vec2Fix position, int strokes)
        {
            _position = position;
            _velocity = Vec2Fix.Zero;
            _lastRestPosition = position;
            _restTicks = 0;
            _ticksSinceShot = 0;
            Strokes = strokes;
            IsAtRest = true;
            IsHoled = false;
        }

        /// <summary>
        /// Plays a stroke: sets the ball velocity from the quantized angle and
        /// power. Speed scales linearly: <c>max * (power+1)/256</c>.
        /// </summary>
        public void Shoot(ShotInput shot)
        {
            if (!IsAtRest || IsHoled || Strokes >= StrokeLimit)
            {
                return; // needs a resting ball, an open hole and strokes left
            }

            var speed = _config.MaxShotSpeed * Fix64.FromFraction(shot.PowerIndex + 1, 256);
            _velocity = FixTrig.UnitVector(shot.AngleIndex) * speed;
            Strokes++;
            IsAtRest = false;
            _restTicks = 0;
            _ticksSinceShot = 0; // windmill blades re-arm to their base phase
        }

        /// <summary>
        /// Advances the simulation by one fixed 1/120 s step: exponential
        /// rolling-friction damping, then sub-stepped position integration with
        /// wall collision resolution (semi-implicit Euler).
        /// </summary>
        public void Tick()
        {
            if (IsAtRest)
            {
                TickCount++;
                return;
            }

            // Ramp acceleration first, then friction damping — damping applies
            // to the boosted velocity, so a slope has a stable terminal speed.
            ApplyRampAcceleration();

            // Surface friction priority: sand beats ice beats bare ground
            // (deterministic tie-break when generated zones overlap).
            _velocity *= IsInSand() ? _config.SandDamping
                : IsInIce() ? _config.IceDamping
                : _config.RollDamping;

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
                ResolveBumperCollisions();
                ResolveWallCollisions();
                ResolveGateCollisions();
                ResolveWindmillCollisions();
                ResolvePortalTransits();
                if (CheckWaterHazard() || CheckHoleCapture())
                {
                    break; // ball was reset or captured; the rest of the tick is void
                }
            }

            if (!IsAtRest)
            {
                UpdateRestDetection();
            }

            _ticksSinceShot++;
            TickCount++;
        }

        /// <summary>
        /// Water: if the ball center is inside a water polygon it sinks — one
        /// penalty stroke, ball returns to the last rest position, at rest.
        /// Checked every sub-step so fast balls cannot skip across.
        /// </summary>
        private bool CheckWaterHazard()
        {
            var zones = _course.WaterZones;
            for (int i = 0; i < zones.Length; i++)
            {
                if (!zones[i].Contains(_position))
                {
                    continue;
                }

                Strokes++;
                WaterEntryCount++;
                _position = _lastRestPosition;
                _velocity = Vec2Fix.Zero;
                _restTicks = 0;
                IsAtRest = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Counts consecutive below-threshold ticks; once enough accumulate the
        /// ball is at rest and its velocity is zeroed exactly (canonical state
        /// for hashing and for accepting the next shot).
        /// </summary>
        private void UpdateRestDetection()
        {
            if (_velocity.LengthSq() < _config.RestSpeedEpsSq)
            {
                _restTicks++;
                if (_restTicks >= _config.RestTicksRequired)
                {
                    _velocity = Vec2Fix.Zero;
                    _lastRestPosition = _position;
                    IsAtRest = true;
                }
            }
            else
            {
                _restTicks = 0;
            }
        }

        /// <summary>
        /// FNV-1a (64-bit) over the raw fields of the simulation state — the
        /// backbone of all determinism tests: two runs match iff their hashes
        /// match after every tick.
        /// </summary>
        public ulong StateHash()
        {
            ulong h = 14695981039346656037UL;
            h = HashLong(h, _position.X.Raw);
            h = HashLong(h, _position.Y.Raw);
            h = HashLong(h, _velocity.X.Raw);
            h = HashLong(h, _velocity.Y.Raw);
            h = HashLong(h, _lastRestPosition.X.Raw);
            h = HashLong(h, _lastRestPosition.Y.Raw);
            h = HashLong(h, TickCount);
            h = HashLong(h, Strokes);
            h = HashLong(h, _restTicks);
            h = HashLong(h, IsAtRest ? 1L : 0L);
            h = HashLong(h, IsHoled ? 1L : 0L);
            return h;
        }

        private static ulong HashLong(ulong hash, long value)
        {
            unchecked
            {
                ulong v = (ulong)value;
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (v >> (i * 8)) & 0xFF;
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }

        /// <summary>
        /// Hole: when the ball center is inside the cup, it is captured if slow
        /// enough (sim finished); a fast overlap rims out — the ball is pushed
        /// back to the cup edge and its inward velocity reflects with reduced
        /// restitution. Checked every sub-step.
        /// </summary>
        private bool CheckHoleCapture()
        {
            var delta = _position - _course.HolePosition;
            var distSq = delta.LengthSq();
            var holeRadius = _config.HoleRadius;
            if (distSq >= holeRadius * holeRadius)
            {
                return false;
            }

            if (_velocity.LengthSq() <= _config.HoleCaptureSpeedSq)
            {
                _position = _course.HolePosition;
                _velocity = Vec2Fix.Zero;
                _restTicks = 0;
                IsAtRest = true;
                IsHoled = true;
                return true;
            }

            // Rim out: place the ball on the cup edge and reflect the inward
            // normal component with reduced restitution.
            var dist = Fix64.Sqrt(distSq);
            var normal = dist > Fix64.Zero
                ? delta / dist
                : new Vec2Fix(Fix64.One, Fix64.Zero);

            _position = _course.HolePosition + normal * holeRadius;
            var vn = Vec2Fix.Dot(_velocity, normal);
            if (vn < Fix64.Zero)
            {
                var bounce = Fix64.One + _config.RimRestitution;
                _velocity -= normal * (bounce * vn);
            }

            return false;
        }

        /// <summary>
        /// Collides the ball against every blade at its current phase. Blades
        /// are segments from the pivot outward; phase advances with ticks since
        /// the current shot. A blade sweeping into a slow ball shoves it via
        /// the shared positional push-out (no surface-velocity transfer — a
        /// deliberate simplification the courses are designed around).
        /// </summary>
        private void ResolveWindmillCollisions()
        {
            var mills = _course.Windmills;
            for (int i = 0; i < mills.Length; i++)
            {
                int baseAngle = mills[i].Phase0 + mills[i].OmegaSteps * _ticksSinceShot;
                int spacing = FixTrig.AngleSteps / mills[i].BladeCount;
                for (int b = 0; b < mills[i].BladeCount; b++)
                {
                    int angle = (baseAngle + b * spacing) % FixTrig.AngleSteps;
                    if (angle < 0)
                    {
                        angle += FixTrig.AngleSteps;
                    }

                    var tip = mills[i].Pivot + FixTrig.UnitVector(angle) * mills[i].BladeLength;
                    if (ResolveSegmentCollision(mills[i].Pivot, tip))
                    {
                        WindmillHitCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Teleports the ball through the first portal whose trigger disc holds
        /// its center: it reappears just outside the exit along its velocity
        /// direction (radius + ball radius), velocity untouched. The offset
        /// clears the twin portal's disc, so one pass triggers exactly once.
        /// </summary>
        private void ResolvePortalTransits()
        {
            var portals = _course.Portals;
            for (int i = 0; i < portals.Length; i++)
            {
                var delta = _position - portals[i].Entry;
                var radius = portals[i].Radius;
                if (delta.LengthSq() >= radius * radius)
                {
                    continue;
                }

                var speedSq = _velocity.LengthSq();
                var dir = speedSq > Fix64.Zero
                    ? _velocity / Fix64.Sqrt(speedSq)
                    : new Vec2Fix(Fix64.One, Fix64.Zero);
                _position = portals[i].Exit + dir * (radius + _config.BallRadius);
                PortalTransitCount++;
                return; // one transit per sub-step: the ball is elsewhere now
            }
        }

        /// <summary>Adds every containing ramp's acceleration for one tick.</summary>
        private void ApplyRampAcceleration()
        {
            var ramps = _course.Ramps;
            for (int i = 0; i < ramps.Length; i++)
            {
                if (ramps[i].Area.Contains(_position))
                {
                    _velocity += ramps[i].Accel * _config.Dt;
                }
            }
        }

        /// <summary>True when the ball center is inside any sand polygon.</summary>
        private bool IsInSand()
        {
            var zones = _course.SandZones;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Contains(_position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the ball center is inside any ice polygon.</summary>
        private bool IsInIce()
        {
            var zones = _course.IceZones;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i].Contains(_position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Circle-vs-circle bumper bounce: push out, reflect the normal component
        /// with restitution &gt; 1 (speed boost), then cap the exit speed.
        /// </summary>
        private void ResolveBumperCollisions()
        {
            var bumpers = _course.Bumpers;
            for (int i = 0; i < bumpers.Length; i++)
            {
                var delta = _position - bumpers[i].Center;
                var minDist = _config.BallRadius + bumpers[i].Radius;
                var distSq = delta.LengthSq();
                if (distSq >= minDist * minDist)
                {
                    continue;
                }

                var dist = Fix64.Sqrt(distSq);
                // Center exactly on the bumper center: deterministic fallback normal.
                var normal = dist > Fix64.Zero
                    ? delta / dist
                    : new Vec2Fix(Fix64.One, Fix64.Zero);

                _position = bumpers[i].Center + normal * minDist;

                var vn = Vec2Fix.Dot(_velocity, normal);
                if (vn < Fix64.Zero)
                {
                    BumperHitCount++;
                    var bounce = Fix64.One + _config.BumperRestitution;
                    _velocity -= normal * (bounce * vn);

                    // Cap the boosted exit speed.
                    var speedSq = _velocity.LengthSq();
                    var cap = _config.BumperMaxExitSpeed;
                    if (speedSq > cap * cap)
                    {
                        var speed = Fix64.Sqrt(speedSq);
                        _velocity = _velocity * (cap / speed);
                    }
                }
            }
        }

        /// <summary>
        /// One-way gates: a ball moving with the gate's pass normal ignores the
        /// segment entirely; any other ball collides with it exactly like a
        /// wall. The velocity test uses the pre-collision velocity of this
        /// sub-step, so a ball reflected by the gate cannot re-trigger passage
        /// within the same resolution pass.
        /// </summary>
        private void ResolveGateCollisions()
        {
            var gates = _course.Gates;
            for (int i = 0; i < gates.Length; i++)
            {
                if (Vec2Fix.Dot(_velocity, gates[i].PassNormal) > Fix64.Zero)
                {
                    continue; // moving in the allowed direction: the gate is open
                }

                if (ResolveSegmentCollision(gates[i].A, gates[i].B))
                {
                    GateHitCount++;
                }
            }
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
                if (ResolveSegmentCollision(walls[i].A, walls[i].B))
                {
                    WallHitCount++;
                }
            }
        }

        /// <summary>
        /// Shared circle-vs-segment resolution (walls and blocking gates):
        /// pushes the ball onto the surface and reflects the approaching normal
        /// component with wall restitution. Returns true when a bounce happened.
        /// </summary>
        private bool ResolveSegmentCollision(Vec2Fix a, Vec2Fix b)
        {
            var ab = b - a;

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
                return false;
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
                return true;
            }

            return false;
        }
    }
}
