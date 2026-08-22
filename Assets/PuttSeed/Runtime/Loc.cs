#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Lightweight localization: the ENGLISH string is the key. Untranslated
    /// text falls through unchanged, so nothing ever renders as a broken key.
    /// The language resolves before the first scene loads (saved preference,
    /// else the device language); adding a language means adding a dictionary.
    /// Share payloads deliberately stay English — they travel to strangers.
    /// </summary>
    public static class Loc
    {
        public enum Language
        {
            English,
            Turkish,
        }

        /// <summary>The active language (resolved at startup, or by Settings).</summary>
        public static Language Current { get; private set; } = Language.English;

        /// <summary>Translates an English string (or format template) or returns it as is.</summary>
        public static string Tr(string english)
            => Current == Language.Turkish && Turkish.TryGetValue(english, out var tr) ? tr : english;

        /// <summary>
        /// The culture dates are rendered in: the UI language, never the
        /// device's. Formatting with the ambient culture used to put a Turkish
        /// month into an English sentence ("Daily Ağu 17") on a Turkish device.
        /// </summary>
        private static System.Globalization.CultureInfo DateCulture
            => Current == Language.Turkish
                ? TurkishCulture
                : System.Globalization.CultureInfo.InvariantCulture;

        private static readonly System.Globalization.CultureInfo TurkishCulture =
            new System.Globalization.CultureInfo("tr-TR");

        /// <summary>
        /// A short date for UI labels, in the UI language. Both halves follow
        /// the language: the month name from the culture, and the ORDER from
        /// convention — English puts the month first ("Aug 17"), Turkish the
        /// day ("17 Ağu"). A fixed pattern would have localized only the word.
        /// Every date shown to the player goes through here.
        /// </summary>
        public static string ShortDate(System.DateTime date)
            => date.ToString(Current == Language.Turkish ? "d MMM" : "MMM d", DateCulture);

        /// <summary>A month and year for the calendar header, in the UI language.</summary>
        public static string MonthLabel(System.DateTime date)
            => date.ToString("MMMM yyyy", DateCulture);

        /// <summary>
        /// The weekday the calendar's first column shows. Cultures disagree —
        /// Turkish weeks start on Monday, English ones on Sunday — so the grid
        /// asks rather than assumes.
        /// </summary>
        public static System.DayOfWeek FirstDayOfWeek => DateCulture.DateTimeFormat.FirstDayOfWeek;

        /// <summary>
        /// The seven column headings, starting at <see cref="FirstDayOfWeek"/>.
        /// Abbreviated, not "shortest": Unity's Turkish data collapses the
        /// shortest names to single letters with only four distinct values
        /// (P, P, S, Ç, P, C, C), which is no heading at all.
        /// </summary>
        public static string[] WeekdayInitials()
        {
            var names = DateCulture.DateTimeFormat.AbbreviatedDayNames;
            var ordered = new string[7];
            int first = (int)FirstDayOfWeek;
            for (int i = 0; i < 7; i++)
            {
                ordered[i] = names[(first + i) % 7];
            }

            return ordered;
        }

        /// <summary>
        /// Applies a saved language code: "en"/"tr" explicit, anything else
        /// follows the device language.
        /// </summary>
        public static void Apply(string savedCode)
        {
            Current = savedCode == "tr" ? Language.Turkish
                : savedCode == "en" ? Language.English
                : Application.systemLanguage == SystemLanguage.Turkish
                    ? Language.Turkish
                    : Language.English;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            // Before ANY scene object wakes: baked labels localize in Awake,
            // so the language must already be resolved here.
            Apply(new StatsStore(MenuBootstrap.StatsPath()).Data.language);
        }

        /// <summary>The Turkish table (exposed for the placeholder-parity test).</summary>
        public static readonly Dictionary<string, string> Turkish = new Dictionary<string, string>
        {
            // Menu & static UI
            ["one hole a day · same for everyone"] = "günde bir delik · herkese aynı",
            ["Play today's hole"] = "Bugünün deliğini oyna",
            ["Practice"] = "Antrenman",
            ["Tutorial"] = "Eğitim",
            ["Tutorial  ·  start here"] = "Eğitim  ·  buradan başla",
            ["Archive"] = "Arşiv",
            // "Hard" itself is translated further down with the difficulty names.
            ["Journey"] = "Yolculuk",
            ["Journey · {0}/{1}"] = "Yolculuk · {0}/{1}",
            ["Level {0}/{1}"] = "Bölüm {0}/{1}",
            ["Next level"] = "Sonraki bölüm",
            ["Next hole"] = "Sonraki delik",
            ["Gauntlet {0}/{1}  ·  {2} total"] = "Gauntlet {0}/{1}  ·  toplam {2}",
            ["Weekly gauntlet"] = "Haftalık gauntlet",
            ["Gauntlet"] = "Gauntlet",
            ["Gauntlet · {0}"] = "Gauntlet · {0}",
            ["Week done — {0} strokes!"] = "Hafta bitti — {0} vuruş!",
            ["{0} stars"] = "{0} yıldız",
            ["Settings"] = "Ayarlar",
            ["Collection"] = "Koleksiyon",
            ["Stats"] = "İstatistikler",
            ["Achievements"] = "Başarımlar",
            ["Close"] = "Kapat",
            ["Older"] = "Eski",
            ["Newer"] = "Yeni",
            ["Random day"] = "Rastgele gün",
            ["Menu"] = "Menü",
            ["Next lesson"] = "Sonraki ders",
            ["Finish tutorial"] = "Eğitimi bitir",
            ["Watch"] = "İzle",
            ["Retry"] = "Tekrar",
            ["Share"] = "Paylaş",
            ["Ghost"] = "Hayalet",
            ["Undo"] = "Geri Al",
            ["paste PUTT- code…"] = "PUTT- kodunu yapıştır…",
            ["paste PUTTSAVE- code…"] = "PUTTSAVE- kodunu yapıştır…",
            ["Out of strokes!"] = "Vuruş hakkın bitti!",
            ["The limit is par + {0} — line up and go again."] = "Limit par + {0} — nişanla ve yeniden dene.",
            ["Sound"] = "Ses",
            ["Haptics"] = "Titreşim",
            ["Aim"] = "Nişan",
            ["Colors"] = "Renkler",
            ["Sling"] = "Sapan",
            ["Direct"] = "Direkt",
            ["On"] = "Açık",
            ["Off"] = "Kapalı",
            ["Export save"] = "Kaydı dışa aktar",
            ["Import"] = "İçe aktar",
            ["Share best"] = "En iyiyi paylaş",

            // Menu, runtime-formatted
            ["Daily {0} — done in {1}"] = "Günlük {0} — {1} vuruşta bitti",
            ["Play today's hole · {0}"] = "Bugünün deliği · {0}",
            ["next hole in {0}"] = "sonraki delik: {0}",
            ["New hole is ready — restart to play!"] = "Yeni delik hazır — yeniden başlat!",
            ["Streak {0}"] = "Seri {0}",
            ["No streak yet"] = "Henüz seri yok",
            [" · Today: {0} attempt(s)"] = " · Bugün: {0} deneme",
            [" · Practice: {0}"] = " · Antrenman: {0}",
            ["{0}  ·  best {1}"] = "{0}  ·  en iyi {1}",
            ["{0}  ·  not played"] = "{0}  ·  oynanmadı",
            ["{0}–{1} days ago"] = "{0}–{1} gün önce",
            ["Streak {0}  (best {1})"] = "Seri {0}  (en iyi {1})",
            ["Par streak {0}  (best {1})"] = "Par serisi {0}  (en iyi {1})",
            ["Dailies finished  {0}"] = "Bitirilen günlük  {0}",
            ["Daily attempts  {0}"] = "Günlük deneme  {0}",
            ["Practice rounds  {0}"] = "Antrenman turu  {0}",
            ["Fewest strokes in practice"] = "Antrenmanda en az vuruş",
            ["  Easy {0}   ·   Normal {1}   ·   Hard {2}"] = "  Kolay {0}   ·   Normal {1}   ·   Zor {2}",
            ["not yet"] = "henüz yok",
            ["Strokes taken"] = "Atılan vuruş",
            ["Copied!"] = "Kopyalandı!",
            ["Sharing…"] = "Paylaşılıyor…",
            ["Invalid code"] = "Geçersiz kod",
            ["Tap to confirm"] = "Onay için tekrar dokun",
            ["Show"] = "Göster",
            ["Balls"] = "Toplar",
            ["Trails"] = "İzler",
            ["Classic"] = "Klasik",
            ["Spark"] = "Kıvılcım",
            ["Frost"] = "Kırağı",
            ["Blaze"] = "Alev",
            ["Aurora"] = "Kutup Işığı",
            ["Prism"] = "Prizma",
            ["{0}  —  equipped"] = "{0}  —  kuşanıldı",
            ["{0}  —  locked: {1}"] = "{0}  —  kilitli: {1}",

            // Game HUD
            ["generating…"] = "üretiliyor…",
            ["Generating course"] = "Kurs üretiliyor",
            ["Daily"] = "Günlük",
            ["Daily · {0}"] = "Günlük · {0}",
            ["{0} · {1}"] = "{0} · {1}",
            ["New course"] = "Yeni kurs",
            ["Colorblind mode"] = "Renk körü modu",
            ["Shifts sand, water and bumpers apart for red-green colour blindness."] =
                "Kum, su ve tamponları kırmızı-yeşil renk körlüğü için birbirinden ayırır.",
            ["Full is recommended. Reduced removes shake, slow motion and confetti."] =
                "Önerilen: Tam. Azalt; sarsıntıyı, ağır çekimi ve konfetiyi kaldırır.",
            ["Welcome"] = "Hoş geldin",
            ["Three quick things. All of them can be changed later in Settings."] =
                "Üç kısa soru. Hepsi sonradan Ayarlar'dan değiştirilebilir.",
            ["Start"] = "Başla",
            ["Comet"] = "Kuyruklu",
            ["Bubbles"] = "Baloncuk",
            ["Racer"] = "Yarışçı",
            ["Domino"] = "Domino",
            ["Par {0} · {1}"] = "Par {0} · {1}",
            ["Reminder"] = "Hatırlatıcı",
            ["Daily hole"] = "Günlük delik",
            ["One nudge when a new hole is ready."] = "Yeni delik hazır olduğunda tek bir dürtme.",
            ["Today's hole is ready ⛳"] = "Bugünün deliği hazır ⛳",
            ["Want a nudge when tomorrow's hole is ready?"] =
                "Yarının deliği hazır olduğunda haber vereyim mi?",
            ["Yes please"] = "Olur",
            ["No thanks"] = "Gerek yok",
            ["Motion"] = "Hareket",
            ["Full"] = "Tam",
            ["Reduced"] = "Azalt",
            ["Icy day"] = "Buzlu gün",
            ["Bouncy day"] = "Zıplak gün",
            ["Windy day"] = "Rüzgârlı gün",
            ["Practice · {0}"] = "Antrenman · {0}",
            ["Tutorial {0}/{1}"] = "Eğitim {0}/{1}",
            ["{0} strokes · best {1}"] = "{0} vuruş · en iyi {1}",
            ["{0} strokes"] = "{0} vuruş",
            ["Streak {0} · par streak {1}"] = "Seri {0} · par serisi {1}",
            ["Next hole in {0}"] = "Sonraki delik: {0}",
            ["Easy"] = "Kolay",
            ["Normal"] = "Normal",
            ["Hard"] = "Zor",
            ["Par — well played!"] = "Par — iyi oynadın!",
            ["Bogey — holed!"] = "Bogey — girdi!",
            ["Holed!"] = "Girdi!",
            ["Finish the hole to share your run."] = "Paylaşmak için deliği bitir.",
            ["Copied to clipboard!"] = "Panoya kopyalandı!",
            ["Sharing course…"] = "Kurs paylaşılıyor…",
            ["Course code copied!"] = "Kurs kodu kopyalandı!",
            ["Author ghost off."] = "Yazar hayaleti kapalı.",
            ["Author ghost on (amber)."] = "Yazar hayaleti açık (kehribar).",
            ["Ghost playing (pink)."] = "Hayalet oynuyor (pembe).",
            ["Not a valid PUTT- code."] = "Geçerli bir PUTT- kodu değil.",
            ["Shot undone."] = "Vuruş geri alındı.",
            ["Achievement — {0}!"] = "Başarım — {0}!",
            ["New practice best — {0}!"] = "Yeni antrenman rekoru — {0}!",
            ["Replay code found in clipboard — tap Watch."] = "Panoda replay kodu bulundu — İzle'ye dokun.",

            // Tutorial hints
            ["Drag anywhere and release to shoot — reach the hole within the stroke limit."] =
                "Herhangi bir yerden sürükleyip bırak — vuruş limiti içinde deliğe ulaş.",
            ["Bumpers boost the ball, sand drags it down — both sit on your way to the cup."] =
                "Tamponlar topu hızlandırır, kum yavaşlatır — ikisi de deliğe giden yolun üstünde.",
            ["Ice barely slows the ball; water costs a stroke and puts it back where it was."] =
                "Buz topu neredeyse hiç yavaşlatmaz; su bir vuruşa mal olup topu geri koyar.",
            ["Arrows show the way: gates pass from one side only, ramps push you downhill."] =
                "Oklar yönü gösterir: kapılar tek taraftan geçirir, rampalar yokuş aşağı iter.",
            ["Portals throw the ball to their twin — and the blades never stop turning."] =
                "Portallar topu ikizine fırlatır — kanatlar ise hiç durmaz.",

            // Achievements
            ["First Putt"] = "İlk Vuruş",
            ["Clean Strike"] = "Temiz Vuruş",
            ["Three Stars"] = "Üç Yıldız",
            ["Seven Days"] = "Yedi Gün",
            ["Regular"] = "Müdavim",
            ["Time Traveler"] = "Zaman Yolcusu",
            ["Range Rat"] = "Antrenman Kurdu",
            ["hole out for the first time"] = "ilk kez deliği bitir",
            ["hole in one"] = "tek vuruşta delik",
            ["hole out without touching a wall"] = "duvara değmeden deliği bitir",
            ["earn three stars on a daily"] = "bir günlükte üç yıldız kazan",
            ["reach a 7-day streak"] = "7 günlük seriye ulaş",
            ["complete 10 different dailies"] = "10 farklı günlük tamamla",
            ["complete an archive day"] = "arşivden bir gün tamamla",
            ["play 25 practice courses"] = "25 antrenman kursu oyna",
            ["Bank Shot"] = "Bandodan",
            ["Untouched"] = "Değmeden",
            ["Millwright"] = "Değirmenci",
            ["Down to the Wire"] = "Son Vuruşta",
            ["Perfectionist"] = "Mükemmeliyetçi",
            ["hole out on a shot off three walls"] = "üç duvara çarpan bir vuruşla deliği bitir",
            ["hole out without touching a hazard"] = "hiçbir engele değmeden deliği bitir",
            ["hole out on a windmill course, blades untouched"] =
                "değirmenli bir kursu kanatlara değmeden bitir",
            ["hole out on your final allowed stroke"] = "son hakkınla deliği bitir",
            ["earn three stars on 10 dailies"] = "10 günlükte üç yıldız kazan",

            // Ball skins
            ["Cream"] = "Krem",
            ["Amber"] = "Kehribar",
            ["Rose"] = "Gül",
            ["Mint"] = "Nane",
            ["Sky"] = "Gök",
            ["Lime"] = "Limon",
            ["Coral"] = "Mercan",
            ["Violet"] = "Menekşe",
            ["Ember"] = "Köz",
            ["Gold"] = "Altın",
            ["complete journey level {0}"] = "{0}. yolculuk bölümünü tamamla",
            ["earn {0} journey stars"] = "{0} yolculuk yıldızı topla",
        };
    }

}
