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

        /// <summary>Wall color.</summary>
        public static readonly Color Wall = new Color(0.16f, 0.15f, 0.18f);

        /// <summary>Sand zone fill.</summary>
        public static readonly Color Sand = new Color(0.85f, 0.76f, 0.54f);

        /// <summary>Water zone fill.</summary>
        public static readonly Color Water = new Color(0.27f, 0.55f, 0.83f);

        /// <summary>Bumper fill.</summary>
        public static readonly Color Bumper = new Color(0.91f, 0.36f, 0.46f);

        /// <summary>Hole cup fill.</summary>
        public static readonly Color Hole = new Color(0.07f, 0.07f, 0.09f);

        /// <summary>Player ball.</summary>
        public static readonly Color Ball = new Color(0.97f, 0.97f, 0.95f);

        /// <summary>Ghost ball (translucent).</summary>
        public static readonly Color Ghost = new Color(0.97f, 0.97f, 0.95f, 0.35f);
    }
}
