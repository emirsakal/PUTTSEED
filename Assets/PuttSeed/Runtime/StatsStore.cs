#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>One daily course's local record.</summary>
    [Serializable]
    public sealed class DayRecord
    {
        public int day;
        public int bestStrokes;
        public int bestStars;
        public int attempts;
        public bool completed;
        public string bestReplay = "";
    }

    /// <summary>Everything persisted locally (no backend, no accounts).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int streak;
        public int lastCompletedDay;
        public int practicePlayed;
        public bool tutorialSeen;

        // Settings ride in the same file. Field initializers double as the
        // defaults for saves written before the fields existed (JsonUtility
        // leaves absent fields at their initializer values).
        public bool soundEnabled = true;
        public bool hapticsEnabled = true;

        public List<DayRecord> days = new List<DayRecord>();
    }

    /// <summary>
    /// Local stats and streak, stored as JSON at a caller-supplied path
    /// (persistentDataPath in the app; a temp file in tests). Plain class so
    /// the streak arithmetic is EditMode-testable. Corrupt or missing files
    /// start fresh rather than crashing the game.
    /// </summary>
    public sealed class StatsStore
    {
        private readonly string _path;

        /// <summary>The loaded save data (mutate via Record* methods).</summary>
        public SaveData Data { get; private set; } = new SaveData();

        /// <summary>Opens (or freshly creates) the store at a JSON file path.</summary>
        public StatsStore(string path)
        {
            _path = path;
            try
            {
                if (File.Exists(_path))
                {
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path)) ?? new SaveData();
                }
            }
            catch (Exception)
            {
                Data = new SaveData();
            }
        }

        /// <summary>The record for a day number, or null — never creates (archive browsing).</summary>
        public DayRecord? FindDay(int day)
        {
            for (int i = 0; i < Data.days.Count; i++)
            {
                if (Data.days[i].day == day)
                {
                    return Data.days[i];
                }
            }

            return null;
        }

        /// <summary>The record for a day number, created on first touch.</summary>
        public DayRecord GetOrCreateDay(int day)
        {
            for (int i = 0; i < Data.days.Count; i++)
            {
                if (Data.days[i].day == day)
                {
                    return Data.days[i];
                }
            }

            var record = new DayRecord { day = day };
            Data.days.Add(record);
            return record;
        }

        /// <summary>Counts one attempt (run start or retry) on a daily course.</summary>
        public void RecordDailyAttempt(int day)
        {
            GetOrCreateDay(day).attempts++;
            Save();
        }

        /// <summary>
        /// Records holing out a daily: keeps the best stroke count, its star
        /// rating and its replay; first completion of a day advances the
        /// streak (consecutive day numbers) or restarts it at 1 after a gap.
        /// Archive plays pass <paramref name="countsForStreak"/> false — the
        /// record fills in, but history cannot be farmed for streak credit.
        /// </summary>
        public void RecordDailyCompletion(int day, int strokes, int stars, string replayCode,
            bool countsForStreak = true)
        {
            var record = GetOrCreateDay(day);
            bool firstCompletionToday = !record.completed;
            if (firstCompletionToday || strokes < record.bestStrokes)
            {
                record.bestStrokes = strokes;
                record.bestStars = stars;
                record.bestReplay = replayCode;
            }

            record.completed = true;

            if (countsForStreak && firstCompletionToday && day > Data.lastCompletedDay)
            {
                Data.streak = day == Data.lastCompletedDay + 1 && Data.lastCompletedDay != 0
                    ? Data.streak + 1
                    : 1;
                Data.lastCompletedDay = day;
            }

            Save();
        }

        /// <summary>Marks the FTUE tutorial as launched (persisted immediately).</summary>
        public void MarkTutorialSeen()
        {
            Data.tutorialSeen = true;
            Save();
        }

        /// <summary>Persists the sound toggle.</summary>
        public void SetSoundEnabled(bool enabled)
        {
            Data.soundEnabled = enabled;
            Save();
        }

        /// <summary>Persists the haptics toggle.</summary>
        public void SetHapticsEnabled(bool enabled)
        {
            Data.hapticsEnabled = enabled;
            Save();
        }

        /// <summary>Counts one practice course played.</summary>
        public void RecordPracticePlayed()
        {
            Data.practicePlayed++;
            Save();
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_path, JsonUtility.ToJson(Data, true));
            }
            catch (Exception)
            {
                // Persistence must never take the game down; stats are best-effort.
            }
        }
    }
}
