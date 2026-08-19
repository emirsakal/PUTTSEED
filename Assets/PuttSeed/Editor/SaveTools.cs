#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Save-file tools for testing the first launch. The local save is a plain
    /// JSON file, NOT PlayerPrefs — "Clear All PlayerPrefs" leaves it
    /// untouched, which is a confusing half hour the first time — and the FTUE
    /// only fires when that file is absent, so testing a new player means
    /// moving it out of the way.
    ///
    /// Every reset writes a timestamped backup first, because the file it
    /// deletes holds a real streak, a real campaign and real unlocks. Restore
    /// puts the newest one back.
    /// </summary>
    public static class SaveTools
    {
        private const string BackupPrefix = "puttseed-stats.backup-";

        /// <summary>Backs up and deletes the local save, so the next Play is a first launch.</summary>
        [MenuItem("PuttSeed/Reset Save (first launch)", priority = 200)]
        public static void ResetSave()
        {
            if (RefuseWhilePlaying())
            {
                return;
            }

            string path = MenuBootstrap.StatsPath();
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("PuttSeed",
                    $"There is no save file at\n\n{path}\n\nThe next Play already starts as a new player.",
                    "OK");
                return;
            }

            string dir = Path.GetDirectoryName(path)!;
            if (!EditorUtility.DisplayDialog("Reset the PuttSeed save?",
                    "This deletes local progress: streak, journey, achievements, skins, practice bests.\n\n"
                    + $"A timestamped backup is written first, next to the save:\n{dir}\n\n"
                    + "The next Play then starts as a brand-new player and walks straight into Tutorial 1.",
                    "Back up and reset", "Cancel"))
            {
                return;
            }

            string backup = Path.Combine(dir,
                BackupPrefix + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            File.Copy(path, backup, overwrite: true);
            File.Delete(path);
            Debug.Log($"PuttSeed: save reset — next Play is a first launch. Backup: {backup}");
        }

        /// <summary>Puts the most recent reset backup back in place.</summary>
        [MenuItem("PuttSeed/Restore Last Save Backup", priority = 201)]
        public static void RestoreLastBackup()
        {
            if (RefuseWhilePlaying())
            {
                return;
            }

            string path = MenuBootstrap.StatsPath();
            string dir = Path.GetDirectoryName(path)!;

            // Timestamps are written in a sortable format, so the last name is
            // the newest backup.
            string? newest = Directory.Exists(dir)
                ? Directory.GetFiles(dir, BackupPrefix + "*.json").OrderBy(f => f, StringComparer.Ordinal).LastOrDefault()
                : null;

            if (newest == null)
            {
                EditorUtility.DisplayDialog("PuttSeed",
                    $"No reset backup found in\n\n{dir}", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Restore the PuttSeed save?",
                    $"This overwrites the current save with\n\n{Path.GetFileName(newest)}",
                    "Restore", "Cancel"))
            {
                return;
            }

            File.Copy(newest, path, overwrite: true);
            Debug.Log($"PuttSeed: save restored from {newest}");
        }

        /// <summary>
        /// A running game rewrites the save when it stops, so anything done to
        /// the file during Play mode is undone the moment the player quits.
        /// </summary>
        private static bool RefuseWhilePlaying()
        {
            if (!EditorApplication.isPlaying)
            {
                return false;
            }

            EditorUtility.DisplayDialog("PuttSeed",
                "Exit Play mode first — a running game writes the save back when it stops, "
                + "so a reset made now would simply reappear.",
                "OK");
            return true;
        }
    }
}
