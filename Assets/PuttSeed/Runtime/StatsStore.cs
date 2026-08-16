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
        /// Records holing out the daily: keeps the best stroke count and its
        /// replay; first completion of a day advances the streak (consecutive
        /// day numbers) or restarts it at 1 after a gap.
        /// </summary>
        public void RecordDailyCompletion(int day, int strokes, string replayCode)
        {
            var record = GetOrCreateDay(day);
            bool firstCompletionToday = !record.completed;
            if (firstCompletionToday || strokes < record.bestStrokes)
            {
                record.bestStrokes = strokes;
                record.bestReplay = replayCode;
            }

            record.completed = true;

            if (firstCompletionToday && day > Data.lastCompletedDay)
            {
                Data.streak = day == Data.lastCompletedDay + 1 && Data.lastCompletedDay != 0
                    ? Data.streak + 1
                    : 1;
                Data.lastCompletedDay = day;
            }

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
