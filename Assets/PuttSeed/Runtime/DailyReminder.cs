#nullable enable
using System;
using System.Collections.Generic;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

namespace PuttSeed.Unity
{
    /// <summary>
    /// The optional nudge that tomorrow's hole exists.
    ///
    /// Entirely local: the device's own alarm clock, no server, no internet —
    /// the app still has no network permission. What it does cost is honesty
    /// elsewhere: Android 13+ asks the player for notification permission, so
    /// the store listing's "the only permission is vibration" became "and, if
    /// you turn the reminder on, notifications". Opt-in, never on by default,
    /// and the game never asks until the player has finished a couple of
    /// dailies — somebody who has played three mornings in a row is being
    /// offered a convenience; somebody who installed four minutes ago is
    /// being nagged.
    ///
    /// Scheduling is the part with teeth, because the hole flips at UTC
    /// midnight and players do not live there. A reminder that fires on a
    /// hole the player already answered is a notification that lies, and in
    /// the timezones far from UTC that is exactly what a naive "every day at
    /// ten" does. <see cref="NextFires"/> is the pure, tested piece: local
    /// mornings, skipping any whose UTC day the player has already answered.
    /// Everything is rescheduled from scratch on every app open, and only a
    /// few days ahead — a player who stops opening the game gets a few quiet
    /// nudges and then silence, not a permanent daily nag.
    /// </summary>
    public static class DailyReminder
    {
        /// <summary>Reminders scheduled ahead — then the game goes quiet.</summary>
        public const int DaysAhead = 3;

        /// <summary>The local hour a reminder aims for.</summary>
        public const int FireHour = 10;

        private const string ChannelId = "daily";

        /// <summary>Turns the reminder on, asks the OS, and schedules.</summary>
        public static void Enable(StatsStore stats)
        {
            stats.SetReminderEnabled(true);
            RequestPermission();
            Sync(stats);
        }

        /// <summary>Turns the reminder off and clears everything scheduled.</summary>
        public static void Disable(StatsStore stats)
        {
            stats.SetReminderEnabled(false);
            CancelAll();
        }

        /// <summary>
        /// Re-plans the next few reminders from the save's current truth.
        /// Called on every app open: cheaper to always rebuild than to reason
        /// about which of yesterday's schedules are still honest.
        /// </summary>
        public static void Sync(StatsStore stats)
        {
            if (!stats.Data.reminderEnabled)
            {
                return;
            }

            CancelAll();
            var nowUtc = DateTime.UtcNow;
            bool todayAnswered = stats.FindDay(ModeController.DayNumber(nowUtc))?.completed == true;
            var fires = NextFires(nowUtc, TimeZoneInfo.Local.GetUtcOffset(nowUtc), todayAnswered, DaysAhead);
            foreach (var fireUtc in fires)
            {
                Schedule(fireUtc);
            }
        }

        /// <summary>
        /// The next UTC instants a reminder may fire: the coming local
        /// mornings at <see cref="FireHour"/>, keeping only those whose UTC
        /// day the player has not already answered. West of Greenwich the
        /// first candidate morning often still belongs to an answered UTC day
        /// — a Monday-evening player in California has already answered
        /// Tuesday's hole — and that skip is the reason this function exists.
        /// </summary>
        public static List<DateTime> NextFires(DateTime nowUtc, TimeSpan utcOffset,
            bool todayAnswered, int count)
        {
            var local = nowUtc + utcOffset;
            var candidate = new DateTime(local.Year, local.Month, local.Day, FireHour, 0, 0);
            if (candidate <= local)
            {
                candidate = candidate.AddDays(1);
            }

            int firstOpenDay = ModeController.DayNumber(nowUtc) + (todayAnswered ? 1 : 0);
            var fires = new List<DateTime>(count);
            while (fires.Count < count)
            {
                var fireUtc = candidate - utcOffset;
                int fireDay = ModeController.DayNumber(fireUtc);
                if (fireDay >= firstOpenDay)
                {
                    fires.Add(fireUtc);
                    firstOpenDay = fireDay + 1; // one nudge per hole
                }

                candidate = candidate.AddDays(1);
            }

            return fires;
        }

        private static void RequestPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
#endif
        }

        private static void CancelAll()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif
        }

        private static void Schedule(DateTime fireUtc)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var channel = new AndroidNotificationChannel(ChannelId,
                Loc.Tr("Daily hole"), Loc.Tr("One nudge when a new hole is ready."),
                Importance.Default);
            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            var notification = new AndroidNotification
            {
                Title = "PUTTSEED",
                Text = Loc.Tr("Today's hole is ready ⛳"),
                FireTime = fireUtc.ToLocalTime(),
            };
            AndroidNotificationCenter.SendNotification(notification, ChannelId);
#endif
        }
    }
}
