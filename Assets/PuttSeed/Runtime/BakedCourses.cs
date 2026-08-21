#nullable enable
using System.Collections.Generic;
using PuttSeed.Core.CourseGen;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Courses the game reads instead of solving.
    ///
    /// A course costs about 1.6 seconds to generate on a desktop and fifteen
    /// to thirty on a 2018 phone, nearly all of it the solver proving that no
    /// shorter solution exists. That proof does not have to happen twice: the
    /// simulation is bit-deterministic, so the desktop's answer IS the phone's
    /// answer, and <see cref="Editor.CourseBaker"/> ships it.
    ///
    /// Nothing here is required for the game to work. Every caller falls back
    /// to generating when a seed is not in a pack, which is what happens to a
    /// daily beyond the baked window or a practice course somebody sent you —
    /// slow, but correct, and identical either way.
    /// </summary>
    public static class BakedCourses
    {
        /// <summary>Which shipped set a course belongs to.</summary>
        public enum Pack
        {
            /// <summary>The five tutorial lessons.</summary>
            Tutorial,

            /// <summary>The hundred journey levels.</summary>
            Journey,

            /// <summary>The daily calendar, for as far ahead as it was baked.</summary>
            Daily,

            /// <summary>The practice pool, indexed by rated difficulty.</summary>
            Practice,
        }

        private static readonly Dictionary<Pack, Dictionary<ulong, GenerationResult>> Loaded =
            new Dictionary<Pack, Dictionary<ulong, GenerationResult>>();

        private static readonly Dictionary<Pack, int> Versions = new Dictionary<Pack, int>();

        private static Dictionary<Difficulty, List<ulong>>? _practiceByDifficulty;

        /// <summary>
        /// The baked course for a seed, if this build shipped one for that
        /// generator version. A version mismatch is a miss rather than an
        /// error: the same seed grows a different hole under a different
        /// generator, and handing back the wrong one would be worse than
        /// waiting.
        /// </summary>
        public static bool TryGet(Pack pack, ulong seed, int version, out GenerationResult result)
        {
            var entries = Load(pack);
            if (entries != null && Versions[pack] == version && entries.TryGetValue(seed, out var found))
            {
                result = found;
                return true;
            }

            result = null!;
            return false;
        }

        /// <summary>
        /// A practice course in the requested bucket, drawn from the pool.
        /// Returns false when nothing was baked for that difficulty, and the
        /// caller searches for one the long way.
        /// </summary>
        public static bool TryDrawPractice(Difficulty want, int version,
            System.Random entropy, out ulong seed, out GenerationResult result)
        {
            seed = 0;
            result = null!;
            var entries = Load(Pack.Practice);
            if (entries == null || Versions[Pack.Practice] != version)
            {
                return false;
            }

            if (_practiceByDifficulty == null)
            {
                _practiceByDifficulty = new Dictionary<Difficulty, List<ulong>>();
                foreach (var pair in entries)
                {
                    if (!_practiceByDifficulty.TryGetValue(pair.Value.Difficulty, out var list))
                    {
                        list = new List<ulong>();
                        _practiceByDifficulty[pair.Value.Difficulty] = list;
                    }

                    list.Add(pair.Key);
                }

                // Dictionary order is not a promise anyone should rely on, and
                // this list is drawn from at random anyway — but sorting makes
                // a run reproducible from its seed, which is worth having.
                foreach (var list in _practiceByDifficulty.Values)
                {
                    list.Sort();
                }
            }

            if (!_practiceByDifficulty.TryGetValue(want, out var bucket) || bucket.Count == 0)
            {
                return false;
            }

            seed = bucket[entropy.Next(bucket.Count)];
            result = entries[seed];
            return true;
        }

        private static Dictionary<ulong, GenerationResult>? Load(Pack pack)
        {
            if (Loaded.TryGetValue(pack, out var cached))
            {
                return cached;
            }

            var asset = Resources.Load<TextAsset>("Courses/" + pack.ToString().ToLowerInvariant());
            if (asset == null)
            {
                Loaded[pack] = null!;
                Versions[pack] = -1;
                return null;
            }

            try
            {
                var entries = CourseBake.Read(asset.bytes, out int version);
                var map = new Dictionary<ulong, GenerationResult>(entries.Count);
                foreach (var entry in entries)
                {
                    map[entry.Seed] = entry.Result;
                }

                Loaded[pack] = map;
                Versions[pack] = version;
                return map;
            }
            catch (System.IO.InvalidDataException e)
            {
                // A pack this build cannot read is a pack it does without.
                Debug.LogWarning($"PuttSeed: course pack {pack} unreadable ({e.Message}); generating instead.");
                Loaded[pack] = null!;
                Versions[pack] = -1;
                return null;
            }
            finally
            {
                Resources.UnloadAsset(asset);
            }
        }
    }
}
