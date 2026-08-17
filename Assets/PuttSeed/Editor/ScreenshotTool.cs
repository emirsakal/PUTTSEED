#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Captures the Game view into docs/media/ at its current resolution —
    /// used to produce the README screenshots at a consistent 1080x1920.
    /// Play mode only: every frame worth showing is a runtime frame.
    /// </summary>
    public static class ScreenshotTool
    {
        [MenuItem("PuttSeed/Capture Screenshot")]
        public static void Capture()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "PuttSeed: enter Play mode first — screenshots capture the running game.");
                return;
            }

            string dir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "docs", "media"));
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"shot-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            ScreenCapture.CaptureScreenshot(file);
            Debug.Log($"PuttSeed: screenshot -> {file}");
        }
    }
}
