#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Procedurally synthesizes the six feedback sounds as 16-bit mono WAV
    /// assets under Resources/Sfx — the same philosophy as the generated app
    /// icon: no purchased packs, no licensing, byte-identical on re-run
    /// (seeded noise, pure math). Assign custom clips on the Feedback object
    /// to override any of them.
    /// </summary>
    public static class SfxSynth
    {
        private const int SampleRate = 44100;
        private const string OutDir = "Assets/PuttSeed/Resources/Sfx";

        /// <summary>Batch/menu entry: writes all six clips and imports them.</summary>
        [MenuItem("PuttSeed/Generate SFX")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(OutDir);
            Write("shot", Shot());
            Write("wall", Wall());
            Write("bumper", Bumper());
            Write("capture", Capture());
            Write("water", Water());
            Write("fail", Fail());
            AssetDatabase.Refresh();
            Debug.Log($"PuttSeed: synthesized 6 SFX clips into {OutDir}.");
        }

        // --- sound recipes -------------------------------------------------

        /// <summary>Soft putter contact: seeded noise tick + low thump.</summary>
        private static float[] Shot()
        {
            var s = NewBuffer(0.09f);
            var rng = new System.Random(101);
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 55f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 220f);
                float thump = Mathf.Sin(2f * Mathf.PI * 105f * t) * env;
                s[i] = 0.55f * thump + 0.35f * noise;
            }

            return s;
        }

        /// <summary>Firm knock off a wall: damped low sine with a click.</summary>
        private static float[] Wall()
        {
            var s = NewBuffer(0.07f);
            var rng = new System.Random(202);
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / SampleRate;
                float click = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 400f);
                float body = Mathf.Sin(2f * Mathf.PI * 175f * t) * Mathf.Exp(-t * 70f);
                s[i] = 0.6f * body + 0.25f * click;
            }

            return s;
        }

        /// <summary>Springy boing: upward pitch sweep with vibrato.</summary>
        private static float[] Bumper()
        {
            var s = NewBuffer(0.18f);
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / 0.18f;
                float freq = Mathf.Lerp(280f, 560f, k) * (1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 28f * t));
                phase += 2f * Mathf.PI * freq / SampleRate;
                float env = Mathf.Exp(-t * 22f);
                s[i] = (0.6f * Mathf.Sin(phase) + 0.15f * Mathf.Sin(2f * phase)) * env;
            }

            return s;
        }

        /// <summary>Hole capture: a thud, then two bright celebratory dings.</summary>
        private static float[] Capture()
        {
            var s = NewBuffer(0.45f);
            var rng = new System.Random(303);
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / SampleRate;
                float thud = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 180f) * 0.3f
                             + Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 60f) * 0.4f;
                float ding1 = t > 0.10f
                    ? Mathf.Sin(2f * Mathf.PI * 659f * (t - 0.10f)) * Mathf.Exp(-(t - 0.10f) * 14f) * 0.35f
                    : 0f;
                float ding2 = t > 0.22f
                    ? Mathf.Sin(2f * Mathf.PI * 880f * (t - 0.22f)) * Mathf.Exp(-(t - 0.22f) * 10f) * 0.35f
                    : 0f;
                s[i] = thud + ding1 + ding2;
            }

            return s;
        }

        /// <summary>Water plop: falling sine plus two tiny bubble blips.</summary>
        private static float[] Water()
        {
            var s = NewBuffer(0.25f);
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / 0.25f;
                float freq = Mathf.Lerp(420f, 140f, Mathf.Sqrt(k));
                phase += 2f * Mathf.PI * freq / SampleRate;
                float plop = Mathf.Sin(phase) * Mathf.Exp(-t * 16f) * 0.6f;
                float blip1 = Blip(t, 0.12f, 900f);
                float blip2 = Blip(t, 0.18f, 1200f);
                s[i] = plop + blip1 + blip2;
            }

            return s;
        }

        /// <summary>Out of strokes: two muted descending tones.</summary>
        private static float[] Fail()
        {
            var s = NewBuffer(0.35f);
            for (int i = 0; i < s.Length; i++)
            {
                float t = (float)i / SampleRate;
                float tone1 = t < 0.16f
                    ? Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 18f) * 0.5f
                    : 0f;
                float tone2 = t > 0.16f
                    ? Mathf.Sin(2f * Mathf.PI * 165f * (t - 0.16f)) * Mathf.Exp(-(t - 0.16f) * 14f) * 0.5f
                    : 0f;
                s[i] = tone1 + tone2;
            }

            return s;
        }

        /// <summary>A short high sine burst starting at <paramref name="start"/>.</summary>
        private static float Blip(float t, float start, float freq)
        {
            if (t <= start)
            {
                return 0f;
            }

            float dt = t - start;
            return Mathf.Sin(2f * Mathf.PI * freq * dt) * Mathf.Exp(-dt * 90f) * 0.12f;
        }

        // --- WAV plumbing --------------------------------------------------

        private static float[] NewBuffer(float seconds)
        {
            return new float[(int)(SampleRate * seconds)];
        }

        /// <summary>Writes samples as a 16-bit mono PCM WAV with a 3 ms fade-out.</summary>
        private static void Write(string name, float[] samples)
        {
            int fade = Math.Min(samples.Length, SampleRate * 3 / 1000);
            for (int i = 0; i < fade; i++)
            {
                samples[samples.Length - 1 - i] *= (float)i / fade;
            }

            string path = $"{OutDir}/{name}.wav";
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int dataBytes = samples.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);            // PCM
                writer.Write((short)1);            // mono
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);      // byte rate
                writer.Write((short)2);            // block align
                writer.Write((short)16);           // bits per sample
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                foreach (float sample in samples)
                {
                    writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }
            }
        }
    }
}
