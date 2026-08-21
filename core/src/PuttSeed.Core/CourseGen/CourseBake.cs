using System;
using System.Collections.Generic;
using System.IO;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.CourseGen
{
    /// <summary>
    /// Reads and writes generated courses as bytes, so a device can LOAD what
    /// it would otherwise have to SOLVE.
    ///
    /// Generation costs about 1.6 seconds on a desktop, nearly all of it spent
    /// in the solver proving that no shorter solution exists. On a 2018
    /// mid-range phone that is fifteen to thirty seconds, and practice — which
    /// searches up to eight candidates for a difficulty bucket — was taking
    /// two minutes to open. That is not a bug to optimise away: it is the
    /// price of the proof, and cutting the solver's budget converts par-3
    /// courses into par-2 ones (measured: 36% to 1%).
    ///
    /// The simulation is bit-deterministic, which makes the answer portable: a
    /// course computed on a desktop is the SAME course the phone would have
    /// computed. So it is computed once, shipped, and read back in
    /// microseconds. <c>CourseBakeTests</c> holds the format to a byte-exact
    /// round trip, and the baker verifies a sample against live generation, so
    /// "the same course" is a checked claim rather than a hopeful one.
    /// </summary>
    public static class CourseBake
    {
        /// <summary>Magic bytes: "PSBC" — PuttSeed baked courses.</summary>
        private static readonly byte[] Magic = { 0x50, 0x53, 0x42, 0x43 };

        /// <summary>
        /// Format revision. A reader refuses anything else rather than
        /// mis-parsing it: a course read wrong is a course that plays wrong.
        /// </summary>
        public const byte Format = 1;

        /// <summary>One baked course: the seed that grew it and everything the generator found.</summary>
        public readonly struct Entry
        {
            /// <summary>The seed this course was generated from.</summary>
            public readonly ulong Seed;

            /// <summary>The generation result, exactly as the generator returned it.</summary>
            public readonly GenerationResult Result;

            /// <summary>Creates an entry.</summary>
            public Entry(ulong seed, GenerationResult result)
            {
                Seed = seed;
                Result = result;
            }
        }

        /// <summary>
        /// Packs courses into one blob. The generator version travels with
        /// them, because a seed grows a different hole under a different
        /// version and a pack that forgot which one it came from is a pack
        /// nobody can trust.
        /// </summary>
        public static byte[] Write(IReadOnlyList<Entry> entries, int generatorVersion)
        {
            using var buffer = new MemoryStream();
            using var writer = new BinaryWriter(buffer);
            writer.Write(Magic);
            writer.Write(Format);
            writer.Write((byte)generatorVersion);
            writer.Write(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                WriteEntry(writer, entries[i]);
            }

            writer.Flush();
            return buffer.ToArray();
        }

        /// <summary>
        /// Unpacks a blob written by <see cref="Write"/>.
        /// </summary>
        /// <exception cref="InvalidDataException">
        /// Not a pack, or a format this build does not read.
        /// </exception>
        public static List<Entry> Read(byte[] bytes, out int generatorVersion)
        {
            using var buffer = new MemoryStream(bytes, writable: false);
            using var reader = new BinaryReader(buffer);
            var magic = reader.ReadBytes(Magic.Length);
            for (int i = 0; i < Magic.Length; i++)
            {
                if (magic.Length <= i || magic[i] != Magic[i])
                {
                    throw new InvalidDataException("Not a PuttSeed course pack.");
                }
            }

            byte format = reader.ReadByte();
            if (format != Format)
            {
                throw new InvalidDataException($"Course pack format {format}, expected {Format}.");
            }

            generatorVersion = reader.ReadByte();
            int count = reader.ReadInt32();
            var entries = new List<Entry>(count);
            for (int i = 0; i < count; i++)
            {
                entries.Add(ReadEntry(reader));
            }

            return entries;
        }

        private static void WriteEntry(BinaryWriter writer, Entry entry)
        {
            var result = entry.Result;
            writer.Write(entry.Seed);
            writer.Write(result.AuthorStrokes);
            writer.Write((byte)result.Difficulty);
            writer.Write(result.DifficultyScore);
            writer.Write(result.Attempts);
            writer.Write(result.RelaxationLevel);

            var shots = result.AuthorSolution;
            writer.Write((byte)shots.Length);
            for (int i = 0; i < shots.Length; i++)
            {
                writer.Write((ushort)shots[i].AngleIndex);
                writer.Write((byte)shots[i].PowerIndex);
            }

            WriteCourse(writer, result.Course);
        }

        private static Entry ReadEntry(BinaryReader reader)
        {
            ulong seed = reader.ReadUInt64();
            int authorStrokes = reader.ReadInt32();
            var difficulty = (Difficulty)reader.ReadByte();
            int score = reader.ReadInt32();
            int attempts = reader.ReadInt32();
            int relaxation = reader.ReadInt32();

            int shotCount = reader.ReadByte();
            var shots = new ShotInput[shotCount];
            for (int i = 0; i < shotCount; i++)
            {
                int angle = reader.ReadUInt16();
                int power = reader.ReadByte();
                shots[i] = new ShotInput(angle, power);
            }

            var course = ReadCourse(reader);
            return new Entry(seed,
                new GenerationResult(course, shots, authorStrokes, difficulty, attempts, relaxation, score));
        }

        private static void WriteCourse(BinaryWriter writer, CourseData course)
        {
            WriteVector(writer, course.StartPosition);
            WriteVector(writer, course.HolePosition);
            writer.Write(course.Par);

            writer.Write(course.Walls.Length);
            foreach (var wall in course.Walls)
            {
                WriteVector(writer, wall.A);
                WriteVector(writer, wall.B);
            }

            writer.Write(course.Bumpers.Length);
            foreach (var bumper in course.Bumpers)
            {
                WriteVector(writer, bumper.Center);
                writer.Write(bumper.Radius.Raw);
            }

            WriteZones(writer, course.SandZones);
            WriteZones(writer, course.WaterZones);
            WriteZones(writer, course.IceZones);

            writer.Write(course.Gates.Length);
            foreach (var gate in course.Gates)
            {
                WriteVector(writer, gate.A);
                WriteVector(writer, gate.B);
                WriteVector(writer, gate.PassNormal);
            }

            writer.Write(course.Ramps.Length);
            foreach (var ramp in course.Ramps)
            {
                WriteZone(writer, ramp.Area);
                WriteVector(writer, ramp.Accel);
            }

            writer.Write(course.Portals.Length);
            foreach (var portal in course.Portals)
            {
                WriteVector(writer, portal.Entry);
                WriteVector(writer, portal.Exit);
                writer.Write(portal.Radius.Raw);
            }

            writer.Write(course.Windmills.Length);
            foreach (var mill in course.Windmills)
            {
                WriteVector(writer, mill.Pivot);
                writer.Write(mill.BladeLength.Raw);
                writer.Write(mill.BladeCount);
                writer.Write(mill.OmegaSteps);
                writer.Write(mill.Phase0);
            }
        }

        private static CourseData ReadCourse(BinaryReader reader)
        {
            var start = ReadVector(reader);
            var hole = ReadVector(reader);
            int par = reader.ReadInt32();

            var walls = new WallSegment[reader.ReadInt32()];
            for (int i = 0; i < walls.Length; i++)
            {
                walls[i] = new WallSegment(ReadVector(reader), ReadVector(reader));
            }

            var bumpers = new Bumper[reader.ReadInt32()];
            for (int i = 0; i < bumpers.Length; i++)
            {
                bumpers[i] = new Bumper(ReadVector(reader), Fix64.FromRaw(reader.ReadInt64()));
            }

            var sand = ReadZones(reader);
            var water = ReadZones(reader);
            var ice = ReadZones(reader);

            var gates = new OneWayGate[reader.ReadInt32()];
            for (int i = 0; i < gates.Length; i++)
            {
                gates[i] = new OneWayGate(ReadVector(reader), ReadVector(reader), ReadVector(reader));
            }

            var ramps = new RampZone[reader.ReadInt32()];
            for (int i = 0; i < ramps.Length; i++)
            {
                ramps[i] = new RampZone(ReadZone(reader), ReadVector(reader));
            }

            var portals = new Portal[reader.ReadInt32()];
            for (int i = 0; i < portals.Length; i++)
            {
                portals[i] = new Portal(ReadVector(reader), ReadVector(reader),
                    Fix64.FromRaw(reader.ReadInt64()));
            }

            var mills = new Windmill[reader.ReadInt32()];
            for (int i = 0; i < mills.Length; i++)
            {
                mills[i] = new Windmill(ReadVector(reader), Fix64.FromRaw(reader.ReadInt64()),
                    reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            }

            return new CourseData(start, hole, par, walls, bumpers, sand, water, ice,
                gates, ramps, portals, mills);
        }

        private static void WriteZones(BinaryWriter writer, ZonePolygon[] zones)
        {
            writer.Write(zones.Length);
            foreach (var zone in zones)
            {
                WriteZone(writer, zone);
            }
        }

        private static ZonePolygon[] ReadZones(BinaryReader reader)
        {
            var zones = new ZonePolygon[reader.ReadInt32()];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = ReadZone(reader);
            }

            return zones;
        }

        private static void WriteZone(BinaryWriter writer, ZonePolygon zone)
        {
            writer.Write(zone.Vertices.Length);
            foreach (var vertex in zone.Vertices)
            {
                WriteVector(writer, vertex);
            }
        }

        private static ZonePolygon ReadZone(BinaryReader reader)
        {
            var vertices = new Vec2Fix[reader.ReadInt32()];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = ReadVector(reader);
            }

            return new ZonePolygon(vertices);
        }

        // Fix64 travels as its RAW long. Anything else — a float, a rounded
        // decimal — would be a different course on arrival, and the whole
        // point of shipping courses is that they arrive identical.
        private static void WriteVector(BinaryWriter writer, Vec2Fix value)
        {
            writer.Write(value.X.Raw);
            writer.Write(value.Y.Raw);
        }

        private static Vec2Fix ReadVector(BinaryReader reader)
            => new Vec2Fix(Fix64.FromRaw(reader.ReadInt64()), Fix64.FromRaw(reader.ReadInt64()));
    }
}
