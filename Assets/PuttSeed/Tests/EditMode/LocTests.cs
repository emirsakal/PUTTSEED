using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    public class LocTests
    {
        [TearDown]
        public void TearDown()
        {
            Loc.Apply("en"); // never leak a language into other tests
        }

        [Test]
        public void English_IsIdentity()
        {
            Loc.Apply("en");
            Assert.That(Loc.Tr("Practice"), Is.EqualTo("Practice"));
        }

        [Test]
        public void Turkish_TranslatesKnown_PassesThroughUnknown()
        {
            Loc.Apply("tr");
            Assert.That(Loc.Tr("Practice"), Is.EqualTo("Antrenman"));
            Assert.That(Loc.Tr("Some brand new string"), Is.EqualTo("Some brand new string"));
        }

        [Test]
        public void ExplicitCodes_WinOverAuto()
        {
            Loc.Apply("tr");
            Assert.That(Loc.Current, Is.EqualTo(Loc.Language.Turkish));
            Loc.Apply("en");
            Assert.That(Loc.Current, Is.EqualTo(Loc.Language.English));
        }

        [Test]
        public void EveryTranslation_KeepsItsFormatPlaceholders()
        {
            foreach (var pair in Loc.Turkish)
            {
                Assert.That(pair.Value, Is.Not.Empty, $"empty translation for '{pair.Key}'");
                for (int i = 0; i < 5; i++)
                {
                    string slot = "{" + i + "}";
                    Assert.That(pair.Value.Contains(slot), Is.EqualTo(pair.Key.Contains(slot)),
                        $"placeholder {slot} mismatch in '{pair.Key}' -> '{pair.Value}'");
                }
            }
        }

        [Test]
        public void EveryAchievementAndSkin_HasATurkishEntry()
        {
            foreach (var def in Achievements.All)
            {
                if (def.Title != "Ace") // golf vocabulary stays universal
                {
                    Assert.That(Loc.Turkish.ContainsKey(def.Title), Is.True, def.Title);
                }

                Assert.That(Loc.Turkish.ContainsKey(def.Detail), Is.True, def.Detail);
            }

            foreach (var skin in BallSkins.All)
            {
                Assert.That(Loc.Turkish.ContainsKey(skin.Name), Is.True, skin.Name);
            }

            foreach (var trail in BallTrails.All)
            {
                Assert.That(Loc.Turkish.ContainsKey(trail.Name), Is.True, trail.Name);
            }
        }

        [Test]
        public void ShortDate_FollowsTheUiLanguage_NotTheDevice()
        {
            var august17 = new System.DateTime(2026, 8, 17);

            Loc.Apply("en");
            Assert.That(Loc.ShortDate(august17), Is.EqualTo("Aug 17"));

            // Not just the month WORD: Turkish writes the day first, so a
            // fixed "MMM d" pattern would still read wrong.
            Loc.Apply("tr");
            Assert.That(Loc.ShortDate(august17), Is.EqualTo("17 Ağu"));
        }

        [Test]
        public void ShortDate_IsStableUnderAForeignAmbientCulture()
        {
            // The bug this guards: dates used to render with the ambient
            // culture, so an English UI on a Turkish device showed "Ağu 17".
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture =
                    new System.Globalization.CultureInfo("tr-TR");
                Loc.Apply("en");
                Assert.That(Loc.ShortDate(new System.DateTime(2026, 8, 17)), Is.EqualTo("Aug 17"));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void MonthLabel_FollowsTheUiLanguage()
        {
            var august = new System.DateTime(2026, 8, 1);

            Loc.Apply("en");
            Assert.That(Loc.MonthLabel(august), Is.EqualTo("August 2026"));

            Loc.Apply("tr");
            Assert.That(Loc.MonthLabel(august), Is.EqualTo("Ağustos 2026"));
        }

        [Test]
        public void WeekdayInitials_StartOnTheCulturesFirstDay()
        {
            // The calendar grid lays days out in this order, so getting it
            // wrong shifts every square by one column.
            Loc.Apply("en");
            Assert.That(Loc.FirstDayOfWeek, Is.EqualTo(System.DayOfWeek.Sunday));
            var en = Loc.WeekdayInitials();
            Assert.That(en.Length, Is.EqualTo(7));
            Assert.That(en[0], Is.EqualTo("Sun"));
            Assert.That(en[6], Is.EqualTo("Sat"));

            Loc.Apply("tr");
            Assert.That(Loc.FirstDayOfWeek, Is.EqualTo(System.DayOfWeek.Monday),
                "Turkish weeks start on Monday");
            Assert.That(Loc.WeekdayInitials().Length, Is.EqualTo(7));
        }

        [Test]
        public void WeekdayInitials_AreAFullWeek_NoRepeats()
        {
            // The bug this caught: Unity's Turkish "shortest" day names are
            // single letters with four distinct values, so three columns of
            // the calendar shared a heading. Exact spellings differ between
            // runtimes, so the assertion is structural.
            foreach (var code in new[] { "en", "tr" })
            {
                Loc.Apply(code);
                var days = Loc.WeekdayInitials();
                Assert.That(days.Length, Is.EqualTo(7), code);
                foreach (var day in days)
                {
                    Assert.That(day, Is.Not.Empty, code);
                }

                Assert.That(new System.Collections.Generic.HashSet<string>(days).Count,
                    Is.EqualTo(7), $"{code} repeats a weekday heading");
            }
        }

        [Test]
        public void EveryTutorialHint_HasATurkishEntry()
        {
            foreach (var stage in TutorialConfig.Stages)
            {
                Assert.That(Loc.Turkish.ContainsKey(stage.Hint), Is.True, stage.Hint);
            }
        }
    }
}
