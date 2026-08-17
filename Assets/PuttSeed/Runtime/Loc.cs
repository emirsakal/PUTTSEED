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
            ["Journey"] = "Yolculuk",
            ["Journey · {0}/{1}"] = "Yolculuk · {0}/{1}",
            ["Level {0}/{1}"] = "Bölüm {0}/{1}",
            ["Next level"] = "Sonraki bölüm",
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
            ["Watch"] = "İzle",
            ["Retry"] = "Tekrar",
            ["Share"] = "Paylaş",
            ["Ghost"] = "Hayalet",
            ["Undo"] = "Geri Al",
            ["paste PUTT- code…"] = "PUTT- kodunu yapıştır…",
            ["paste PUTTSAVE- code…"] = "PUTTSAVE- kodunu yapıştır…",
            ["Out of strokes!"] = "Vuruş hakkın bitti!",
            ["The limit is par + 3 — line up and go again."] = "Limit par + 3 — nişanla ve yeniden dene.",
            ["Sound"] = "Ses",
            ["Haptics"] = "Titreşim",
            ["Aim"] = "Nişan",
            ["Colors"] = "Renkler",
            ["Sling"] = "Sapan",
            ["Direct"] = "Direkt",
            ["Vivid"] = "Canlı",
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
            ["Dailies completed  {0}"] = "Tamamlanan günlük  {0}",
            ["3-star {0}  ·  2-star {1}  ·  1-star {2}"] = "3 yıldız {0}  ·  2 yıldız {1}  ·  1 yıldız {2}",
            ["Daily attempts  {0}  ·  Practice  {1}"] = "Günlük deneme  {0}  ·  Antrenman  {1}",
            ["Practice best   E {0}  ·  N {1}  ·  H {2}"] = "Antrenman rekoru   K {0}  ·  N {1}  ·  Z {2}",
            ["Copied!"] = "Kopyalandı!",
            ["Sharing…"] = "Paylaşılıyor…",
            ["Invalid code"] = "Geçersiz kod",
            ["Tap to confirm"] = "Onay için tekrar dokun",
            ["{0}  —  equipped"] = "{0}  —  kuşanıldı",
            ["{0}  —  locked: {1}"] = "{0}  —  kilitli: {1}",

            // Game HUD
            ["generating…"] = "üretiliyor…",
            ["Generating course"] = "Kurs üretiliyor",
            ["Daily"] = "Günlük",
            ["Daily · {0}"] = "Günlük · {0}",
            ["Practice · {0}"] = "Antrenman · {0}",
            ["Tutorial {0}/{1}"] = "Eğitim {0}/{1}",
            ["{0}   Strokes {1}/{2}   Par {3}{4}"] = "{0}   Vuruş {1}/{2}   Par {3}{4}",
            ["   Streak {0}"] = "   Seri {0}",
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
            ["Pink bumpers boost your ball. Bounce off them — or steer clear."] =
                "Pembe tamponlar topu hızlandırır. Onlardan sek — ya da uzak dur.",
            ["Sand kills your speed. Power through it or roll around."] =
                "Kum hızını öldürür. Ya güçlü vur ya da etrafından dolan.",
            ["Ice barely slows the ball — ease off and plan for the long slide."] =
                "Buz topu neredeyse hiç yavaşlatmaz — yumuşak vur, uzun kaymayı hesapla.",

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
