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

        // The day's OFFICIAL result: the first finish, whatever came after it.
        // Retries stay unlimited — the loop is built on them — but a score
        // taken on the thirty-fourth attempt is not a score anyone can compare
        // against, so it is a personal best rather than the day's answer.
        // Zero means the day has been played and not yet finished.
        public int firstStrokes;
        public int firstStars;
        public string firstReplay = "";

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
        public int bestStreak;

        // The streak that can actually break. The played streak only asks
        // whether you showed up, and with unlimited retries showing up is
        // enough; this one asks whether the day's FIRST finish reached par.
        public int parStreak;
        public int bestParStreak;
        public int lastCompletedDay;
        public int practicePlayed;
        public bool tutorialSeen;
        public List<string> achievements = new List<string>();

        // Settings ride in the same file. Field initializers double as the
        // defaults for saves written before the fields existed (JsonUtility
        // leaves absent fields at their initializer values).
        public bool soundEnabled = true;
        public bool hapticsEnabled = true;
        public bool aimDirect;
        public string ballSkin = "cream";
        public string ballTrail = "plain";
        public bool colorblindMode;
        public bool batterySaver;
        public bool reducedMotion;
        public string language = ""; // "" = follow the device, else "en"/"tr"

        // Practice personal bests per difficulty bucket (0 = none yet).
        public int bestPracticeEasy;
        public int bestPracticeNormal;
        public int bestPracticeHard;

        // Journey progress: index = level, value = best stars (1-3). A level
        // is unlocked when its index <= journeyStars.Count.
        public List<int> journeyStars = new List<int>();

        public List<DayRecord> days = new List<DayRecord>();

        // Weekly gauntlet: the week of the best run and its stroke total.
        // One record is enough — a new week replaces the old one, the same
        // way the gauntlet itself moves on.
        public int gauntletWeek = -1;
        public int gauntletBestStrokes;
        public int gauntletsFinished;
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
            if (firstCompletionToday)
            {
                // Stamped once and never touched again: this is the number the
                // streak, the share and the calendar answer with.
                record.firstStrokes = strokes;
                record.firstStars = stars;
                record.firstReplay = replayCode;
            }

            if (firstCompletionToday || strokes < record.bestStrokes)
            {
                record.bestStrokes = strokes;
                record.bestStars = stars;
                record.bestReplay = replayCode;
            }

            record.completed = true;

            if (countsForStreak && firstCompletionToday && day > Data.lastCompletedDay)
            {
                bool consecutive = day == Data.lastCompletedDay + 1 && Data.lastCompletedDay != 0;
                Data.streak = consecutive ? Data.streak + 1 : 1;

                // Par or better on the FIRST finish keeps the par streak
                // alive; a bogey ends it, and no amount of retrying that day
                // brings it back. That is the whole point of it — the played
                // streak already rewards patience, this one rewards the putt.
                bool parred = stars >= 3;
                Data.parStreak = parred ? (consecutive ? Data.parStreak + 1 : 1) : 0;
                if (Data.parStreak > Data.bestParStreak)
                {
                    Data.bestParStreak = Data.parStreak;
                }

                Data.lastCompletedDay = day;
                if (Data.streak > Data.bestStreak)
                {
                    Data.bestStreak = Data.streak;
                }
            }

            Save();
        }

        /// <summary>
        /// Records a finished gauntlet. A better total on the SAME week
        /// replaces the record; a new week always does, because last week's
        /// score is not a target any more.
        /// </summary>
        public bool RecordGauntlet(int weekIndex, int strokes)
        {
            bool improved = Data.gauntletWeek != weekIndex || strokes < Data.gauntletBestStrokes;
            if (improved)
            {
                Data.gauntletWeek = weekIndex;
                Data.gauntletBestStrokes = strokes;
            }

            Data.gauntletsFinished++;
            Save();
            return improved;
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

        /// <summary>Persists the aim direction preference.</summary>
        public void SetAimDirect(bool direct)
        {
            Data.aimDirect = direct;
            Save();
        }

        /// <summary>Persists the chosen ball skin id.</summary>
        public void SetBallSkin(string id)
        {
            Data.ballSkin = id;
            Save();
        }

        /// <summary>Persists the equipped ball trail.</summary>
        public void SetBallTrail(string id)
        {
            Data.ballTrail = id;
            Save();
        }

        /// <summary>Persists the colorblind palette toggle.</summary>
        public void SetColorblindMode(bool enabled)
        {
            Data.colorblindMode = enabled;
            Save();
        }

        /// <summary>Persists the 60 fps battery saver toggle.</summary>
        public void SetBatterySaver(bool enabled)
        {
            Data.batterySaver = enabled;
            Save();
        }

        /// <summary>
        /// Persists the motion preference. Reduced motion drops shake,
        /// slow-motion, letterbox and confetti and keeps everything that
        /// carries information (see <see cref="MotionSettings"/>).
        /// </summary>
        public void SetReducedMotion(bool reduced)
        {
            Data.reducedMotion = reduced;
            Save();
        }

        /// <summary>Persists the language choice ("en"/"tr").</summary>
        public void SetLanguage(string code)
        {
            Data.language = code;
            Save();
        }

        /// <summary>
        /// Keeps the lowest stroke count per practice bucket; true when this
        /// run set a new best.
        /// </summary>
        public bool RecordPracticeBest(int difficulty, int strokes)
        {
            int current = difficulty == 0 ? Data.bestPracticeEasy
                : difficulty == 1 ? Data.bestPracticeNormal
                : Data.bestPracticeHard;
            if (current != 0 && strokes >= current)
            {
                return false;
            }

            if (difficulty == 0) { Data.bestPracticeEasy = strokes; }
            else if (difficulty == 1) { Data.bestPracticeNormal = strokes; }
            else { Data.bestPracticeHard = strokes; }
            Save();
            return true;
        }

        /// <summary>
        /// Records a journey level result, keeping the best stars; completing
        /// the newest level unlocks the next. True when anything improved.
        /// </summary>
        public bool RecordJourneyResult(int level, int stars)
        {
            bool changed = false;
            while (Data.journeyStars.Count <= level)
            {
                Data.journeyStars.Add(0);
                changed = true;
            }

            if (stars > Data.journeyStars[level])
            {
                Data.journeyStars[level] = stars;
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        /// <summary>How many journey levels are playable (completed + 1).</summary>
        public int UnlockedJourneyLevels(int totalLevels)
            => Math.Min(Data.journeyStars.Count + 1, totalLevels);

        /// <summary>Total journey stars earned (skin gating currency).</summary>
        public int TotalJourneyStars()
        {
            int total = 0;
            for (int i = 0; i < Data.journeyStars.Count; i++)
            {
                total += Data.journeyStars[i];
            }

            return total;
        }

        /// <summary>Replaces the whole save (import); persists immediately.</summary>
        public void ReplaceData(SaveData data)
        {
            Data = data;
            Save();
        }

        /// <summary>Adds an achievement id once; true when newly unlocked.</summary>
        public bool Unlock(string id)
        {
            if (Data.achievements.Contains(id))
            {
                return false;
            }

            Data.achievements.Add(id);
            Save();
            return true;
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
