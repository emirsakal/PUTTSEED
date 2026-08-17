#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Flat-color rendering support: one shared vertex-color material
    /// (Sprites/Default) plus the game palette. Meshes carry their color in
    /// vertex data, so every renderer shares this single material.
    /// The build script adds the shader to Always Included Shaders.
    /// </summary>
    public static class PaletteMaterials
    {
        private static Material? _shared;

        /// <summary>The single shared vertex-color material.</summary>
        public static Material Shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new Material(Shader.Find("Sprites/Default"));
                }

                return _shared;
            }
        }

        /// <summary>Felt background.</summary>
        public static readonly Color Felt = new Color(0.22f, 0.52f, 0.31f);

        /// <summary>Lighter felt band — the mowed-stripe alternate tone.</summary>
        public static readonly Color FeltLight = new Color(0.245f, 0.555f, 0.335f);

        /// <summary>Flag cloth (matches the menu emblem).</summary>
        public static readonly Color Flag = new Color(0.86f, 0.24f, 0.19f);

        /// <summary>Soft drop shadow under raised elements.</summary>
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.22f);

        /// <summary>Wall color.</summary>
        public static readonly Color Wall = new Color(0.16f, 0.15f, 0.18f);

        /// <summary>Sand zone fill.</summary>
        public static readonly Color Sand = new Color(0.85f, 0.76f, 0.54f);

        /// <summary>Water zone fill.</summary>
        public static readonly Color Water = new Color(0.27f, 0.55f, 0.83f);

        /// <summary>Ice zone fill (pale glacial blue).</summary>
        public static readonly Color Ice = new Color(0.78f, 0.91f, 0.96f);

        /// <summary>Bumper fill.</summary>
        public static readonly Color Bumper = new Color(0.91f, 0.36f, 0.46f);

        /// <summary>Hole cup fill.</summary>
        public static readonly Color Hole = new Color(0.07f, 0.07f, 0.09f);

        /// <summary>Player ball.</summary>
        public static readonly Color Ball = new Color(0.97f, 0.97f, 0.95f);

        /// <summary>Ghost ball (translucent).</summary>
        public static readonly Color Ghost = new Color(0.97f, 0.97f, 0.95f, 0.35f);

        /// <summary>
        /// Colorblind palette toggle (set from the saved setting before a
        /// course renders). The alternates push sand toward strong yellow,
        /// water toward deep blue, ice toward white, and the red-on-green
        /// bumper/flag pair toward orange — the classic deutan trouble spots.
        /// The zone textures (speckles, sheen, waves) carry shape cues too.
        /// </summary>
        public static bool ColorblindMode;

        /// <summary>Active sand fill.</summary>
        public static Color SandColor => ColorblindMode ? new Color(0.95f, 0.82f, 0.30f) : Sand;

        /// <summary>Active water fill.</summary>
        public static Color WaterColor => ColorblindMode ? new Color(0.13f, 0.38f, 0.86f) : Water;

        /// <summary>Active ice fill.</summary>
        public static Color IceColor => ColorblindMode ? new Color(0.93f, 0.97f, 1f) : Ice;

        /// <summary>Active bumper fill.</summary>
        public static Color BumperColor => ColorblindMode ? new Color(0.98f, 0.60f, 0.12f) : Bumper;

        /// <summary>Active flag cloth.</summary>
        public static Color FlagColor => ColorblindMode ? new Color(0.98f, 0.60f, 0.12f) : Flag;

        /// <summary>One-way gate bar (amber reads as a signal in both palettes).</summary>
        public static readonly Color Gate = new Color(0.99f, 0.80f, 0.38f);

        /// <summary>
        /// Portal mouths. Violet sits apart from every zone fill in both
        /// palettes; the ring shape is the redundant cue.
        /// </summary>
        public static readonly Color Portal = new Color(0.62f, 0.40f, 0.92f);
    }
}
