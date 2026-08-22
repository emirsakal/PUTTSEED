using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using PuttSeed.Unity.Editor;
using UnityEngine;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The face the game is set in has to be able to print the game. Six
    /// candidates were auditioned for this project and two could not: one had
    /// no right arrow, and one had no dotless i, no soft g and no cedilla s —
    /// every Turkish string would have come out as boxes. Neither failure
    /// breaks a compile, neither shows up in English, and both are invisible
    /// until somebody plays in the other language.
    /// </summary>
    public class UiFontTests
    {
        /// <summary>
        /// Punctuation the chrome prints itself, outside any translation
        /// table: the separator, both dashes, the ellipsis, the arrow and the
        /// two guillemets the carousel steps with.
        /// </summary>
        private const string Chrome = "·—–…‹›→";

        [TearDown]
        public void TearDown() => Loc.Apply("en"); // never leak a language

        [Test]
        public void ActiveFont_PrintsEveryCharacterTheUiCanShow()
        {
            var font = BuildTools.ActiveUiFont();
            if (font == null)
            {
                Assert.Ignore("No project font configured — the built-in face is in use.");
                return;
            }

            var missing = new List<string>();
            foreach (char c in FontCoverage.Missing(font, Required()))
            {
                missing.Add($"U+{(int)c:X4} {c}");
            }

            Assert.That(missing, Is.Empty, $"{font.name} cannot print {string.Join(", ", missing)}");
        }

        /// <summary>
        /// Every non-ASCII character the UI can put on screen, taken from the
        /// things that actually produce text rather than from a list somebody
        /// has to remember to update: both sides of the translation table, the
        /// chrome punctuation, and the dates — which no table holds, because
        /// the culture supplies them (and supplies letters the table misses,
        /// like the g in Ağustos and the C in Çarşamba).
        ///
        /// Emoji are excluded by rule rather than by accident: the shot log's
        /// glyphs come from the OS fallback, and the daily reminder's ⛳ lives
        /// in the translation table but is printed by Android's notification
        /// shade, never by the game's font. Everything below the symbol
        /// blocks — including the arrows and guillemets the chrome prints
        /// itself — is still demanded.
        /// </summary>
        private static IEnumerable<char> Required()
        {
            var text = new StringBuilder(Chrome);
            foreach (var pair in Loc.Turkish)
            {
                text.Append(pair.Key).Append(pair.Value);
            }

            Loc.Apply("tr");
            foreach (string heading in Loc.WeekdayInitials())
            {
                text.Append(heading);
            }

            for (int month = 1; month <= 12; month++)
            {
                var day = new System.DateTime(2026, month, 1);
                text.Append(Loc.MonthLabel(day)).Append(Loc.ShortDate(day));
            }

            var seen = new HashSet<char>();
            var required = new List<char>();
            foreach (char c in text.ToString())
            {
                if (c > 127 && c < 0x2600 && !char.IsSurrogate(c) && seen.Add(c))
                {
                    required.Add(c);
                }
            }

            return required;
        }
    }
}
