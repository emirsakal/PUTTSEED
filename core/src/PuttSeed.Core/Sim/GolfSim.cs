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

        /// <summary>
        /// Clearance the division-free rejection insists on before it skips
        /// the exact test: 2^-10 world units, some twenty binary orders above
        /// the rounding of the division it is avoiding.
        /// </summary>
        private static readonly Fix64 RejectMargin = Fix64.FromFraction(1, 1024);

        // Wall bounding boxes, grown by the ball radius (see the constructor).
        private readonly Fix64[] _wallMinX;
        private readonly Fix64[] _wallMaxX;
        private readonly Fix64[] _wallMinY;
        private readonly Fix64[] _wallMaxY;

        private Vec2Fix _position;
        private Vec2Fix _velocity;
        private Vec2Fix _lastRestPosition;
        private int _restTicks;

        // The windmill phase clock: a free-running counter that advances on
        // EVERY tick, at rest included, so blades keep turning while a player
        // lines up. That makes WHEN you shoot part of the physics, which is
        // why a replay records each shot's clock value (see ReplayCodec v3).
        // It wraps at the 1024-step angle space, so the phase of every mill is
        // a pure function of it. RestoreRest re-arms it to 0, which is what
        // keeps the solver's rest-state BFS reproducible. Deliberately NOT in
        // StateHash: two runs compared tick-for-tick share it by construction.
        private int _millClock;

        /// <summary>Current ball state snapshot.</summary>
        public BallState Ball => new BallState(_position, _velocity);

        /// <summary>Number of ticks advanced since construction.</summary>
        public int TickCount { get; private set; }

        /// <summary>
        /// The free-running windmill phase clock, wrapped to the 1024-step
        /// angle space. Exposed so the render layer can mirror blade angles
        /// exactly, and so a replay can record the moment a shot was taken.
        /// </summary>
        public int MillClock => _millClock;

        /// <summary>The clock wrap: blade phases repeat every this many ticks.</summary>
        public const int MillClockPeriod = 1024;

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

        /// <summary>GDD stroke limit: par + 3. Shots beyond it are refused.</summary>
        public int StrokeLimit => _course.Par + 3;

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

        /// <summary>
        /// Wall bounces during the CURRENT shot (re-armed by <see cref="Shoot"/>
        /// and <see cref="RestoreRest"/>) — what a bank shot is counted with.
        /// </summary>
        public int WallHitsThisShot { get; private set; }

        /// <summary>
        /// True once the ball has met any hazard this run: a bumper, water, or
        /// the inside of a sand or ice zone. Walls are not hazards — they are
        /// the course. Never resets short of a new sim.
        /// </summary>
        public bool TouchedHazard { get; private set; }

        /// <summary>
        /// The speed at which the wind lets go of the ball, squared, on each of
        /// the three surfaces. Above it the wind pushes; below it the ground
        /// has the ball. This is what keeps a windy day from being a day the
        /// ball never stops.
        /// </summary>
        private readonly Fix64 _windReleaseRollSq;
        private readonly Fix64 _windReleaseSandSq;
        private readonly Fix64 _windReleaseIceSq;

        /// <summary>Creates a simulation for one course.</summary>
        public GolfSim(CourseData course, SimConfig config)
        {
            _course = course;
            _config = config;
            _windReleaseRollSq = ReleaseSpeedSq(config, config.RollDamping);
            _windReleaseSandSq = ReleaseSpeedSq(config, config.SandDamping);
            _windReleaseIceSq = ReleaseSpeedSq(config, config.IceDamping);
            _position = course.StartPosition;
            _velocity = Vec2Fix.Zero;
            _lastRestPosition = course.StartPosition;
            IsAtRest = true;

            // Broad-phase boxes for the walls: each segment's own bounding box
            // grown by the ball radius. The exact test below costs a Fix64
            // DIVISION per wall per sub-step, and division here is a
            // shift-subtract loop — at a dozen walls it is most of what the
            // simulation does, and the solver's budget is denominated in ticks.
            var walls = course.Walls;
            _wallMinX = new Fix64[walls.Length];
            _wallMaxX = new Fix64[walls.Length];
            _wallMinY = new Fix64[walls.Length];
            _wallMaxY = new Fix64[walls.Length];
            for (int i = 0; i < walls.Length; i++)
            {
                var a = walls[i].A;
                var b = walls[i].B;
                _wallMinX[i] = Fix64.Min(a.X, b.X) - config.BallRadius;
                _wallMaxX[i] = Fix64.Max(a.X, b.X) + config.BallRadius;
                _wallMinY[i] = Fix64.Min(a.Y, b.Y) - config.BallRadius;
                _wallMaxY[i] = Fix64.Max(a.Y, b.Y) + config.BallRadius;
            }
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
            _millClock = 0; // solver nodes must expand identically every time
            WallHitsThisShot = 0;
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
            WallHitsThisShot = 0;
            // The mill clock deliberately keeps running: the blade angle a
            // shot launches into is whatever the player waited for.
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
                // Blades turn while the player lines up, so the clock advances
                // here too — this branch is the whole point of a free clock.
                AdvanceMillClock();
                TickCount++;
                return;
            }

            // Surface friction priority: sand beats ice beats bare ground
            // (deterministic tie-break when generated zones overlap). Resolved
            // before the wind, because how fast the wind CAN hold a ball
            // depends on the ground it is holding it on.
            bool inSand = IsInSand();
            bool inIce = !inSand && IsInIce();
            TouchedHazard |= inSand || inIce;

            // Wind and ramps push first, then friction damps — damping applies
            // to the boosted velocity, so a slope has a stable terminal speed.
            //
            // The wind bends a roll; it never drives one. Rolling friction here
            // is VISCOUS — proportional to speed, and therefore zero at zero
            // speed — so a steady acceleration always wins in the end: the ball
            // settled at the wind's own terminal speed, drifted forever, and
            // pinned itself against the nearest wall, firing a bounce sound
            // every few frames. Gating the push at exactly that terminal speed
            // is the whole fix, and it needs no number anyone has to tune: over
            // it the wind still shapes the shot, under it the grass has the ball.
            //
            // Wind is zero on an ordinary day, where every gate is zero and
            // adding zero was exact anyway, so no existing hash moves.
            var releaseSq = inSand ? _windReleaseSandSq
                : inIce ? _windReleaseIceSq
                : _windReleaseRollSq;
            if (_velocity.LengthSq() > releaseSq)
            {
                _velocity += _config.Wind * _config.Dt;
            }

            ApplyRampAcceleration();
            _velocity *= inSand ? _config.SandDamping
                : inIce ? _config.IceDamping
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

            AdvanceMillClock();
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
                TouchedHazard = true;
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
        /// <summary>
        /// The speed the wind lets go at, squared, on a surface with the given
        /// damping: twice v = |wind| * dt * k / (1 - k), the fixed point of
        /// "push, then damp".
        ///
        /// The doubling is not taste. That fixed point is an ATTRACTOR reached
        /// from above, so a gate sitting exactly on it is never crossed: the
        /// ball hovers there and rolls on for ever, which is measurably what
        /// happened — 0.44597 units per second, tick after tick, until the test
        /// gave up at 2400. Twice the speed the wind could hold clears the
        /// asymptote while leaving the wind in charge of the whole part of the
        /// roll a player is aiming with. Zero when there is no wind, and zero
        /// on a surface that never slows anything, where no such speed exists.
        /// </summary>
        private static Fix64 ReleaseSpeedSq(SimConfig config, Fix64 damping)
        {
            if (damping >= Fix64.One)
            {
                return Fix64.Zero;
            }

            var sustained = config.Wind.Length() * config.Dt * damping / (Fix64.One - damping);
            var release = sustained + sustained;
            return release * release;
        }

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
                int baseAngle = mills[i].Phase0 + mills[i].OmegaSteps * _millClock;
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

        /// <summary>Advances the wrapped mill clock by one tick.</summary>
        private void AdvanceMillClock()
        {
            _millClock++;
            if (_millClock >= MillClockPeriod)
            {
                _millClock = 0;
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
                    TouchedHazard = true;
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
                // Every point of a segment lies inside the segment's bounding
                // box, so a ball centre outside that box grown by its radius
                // cannot be touching the wall. Comparisons only: nothing is
                // rounded, nothing is approximated, and the exact test still
                // decides every case it could have decided. The 10k-tick golden
                // hash is the proof that this changed only the speed.
                if (_position.X < _wallMinX[i] || _position.X > _wallMaxX[i]
                    || _position.Y < _wallMinY[i] || _position.Y > _wallMaxY[i])
                {
                    continue;
                }

                if (ResolveSegmentCollision(walls[i].A, walls[i].B))
                {
                    WallHitCount++;
                    WallHitsThisShot++;
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

            // Division-free rejection. Whether a circle touches a segment can be
            // decided with multiplies alone — perpendicular distance squared
            // compared as cross² vs r²·|ab|² — but the exact resolution below
            // rounds through a Fix64 division, so agreeing with it bit for bit
            // means only rejecting when the ball is clear by a margin far wider
            // than that rounding could ever move the answer (2^-10 against an
            // error of order 2^-30). Everything nearer falls through to the
            // original arithmetic untouched.
            var abLenSqFast = ab.LengthSq();
            if (abLenSqFast > Fix64.Zero)
            {
                var ap = _position - a;
                var reach = _config.BallRadius + RejectMargin;
                var reachSq = reach * reach;
                var dot = Vec2Fix.Dot(ap, ab);
                if (dot <= Fix64.Zero)
                {
                    if (ap.LengthSq() > reachSq)
                    {
                        return false;
                    }
                }
                else if (dot >= abLenSqFast)
                {
                    if ((_position - b).LengthSq() > reachSq)
                    {
                        return false;
                    }
                }
                else
                {
                    var cross = ab.X * ap.Y - ab.Y * ap.X;
                    if (cross * cross > reachSq * abLenSqFast)
                    {
                        return false;
                    }
                }
            }

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
