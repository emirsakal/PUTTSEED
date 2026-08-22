# Store listing copy

Paste-ready text for Google Play (and later App Store). Character counts are
against Play's limits: title 30, short description 80, full description 4000.

---

## English

**Title** (27/30)

```
PUTTSEED: Daily Golf Puzzle
```

**Short description** (74/80)

```
One hole a day, grown from the date. Same course for everyone, everywhere.
```

**Full description**

```
One hole a day. The same hole for everyone.

Every morning PUTTSEED grows a new mini golf hole out of the date itself.
It is not picked from a list — it is generated, and generated identically on
every phone on earth. Your hole is my hole. Your par is my par. Compare a
score with a friend and you are comparing the same putt.

And before a hole is ever shown to anybody, the game proves it can be
finished. The generator solves every course it makes and throws away the
ones it cannot — so a hole is never unfair, only hard.

WHAT IS IN IT
• Daily hole — one a day, par 2 or 3. Retry as often as you like; your first
  finish is the day's answer.
• Journey — 100 hand-picked levels that teach the whole game, from a bare
  corridor to windmills and portals.
• Practice — endless fresh courses in Easy, Normal or Hard.
• Weekly Gauntlet — a week of holes, back to back, one score.
• Archive — go back and play any day you missed.

BOUNCE OFF EVERYTHING
Bumpers, sand, ice, water, one-way gates, slopes, portals and turning
windmill blades. Rare themed days turn the whole green icy, bouncy or windy —
and the wind is drawn on the course, so you can play around it.

A WHOLE ROUND IN THIRTY CHARACTERS
The physics is fixed-point and exactly reproducible, which means a complete
round fits in a short code. Send it to a friend and they watch your ball take
your line, shot for shot, on their own phone.

NO ADS. NO TRACKING. NO INTERNET.
PUTTSEED has no internet permission — it cannot send anything anywhere. It
asks for vibration, for the tap you feel when the ball hits, and — only if
you switch on the optional daily reminder — notifications. No account, no
analytics, nothing to buy. Your progress lives on your phone and nowhere
else.

Turkish and English. Colourblind palette, reduced motion, and a 60 fps
battery mode.
```

---

## Türkçe

**Başlık** (29/30)

```
PUTTSEED: Günlük Golf Bulmaca
```

**Kısa açıklama** (65/80)

```
Günde tek delik, tarihten üretilir. Herkese, her yerde aynı saha.
```

**Uzun açıklama**

```
Günde tek delik. Herkes için aynı delik.

PUTTSEED her sabah yeni bir mini golf deliğini tarihin kendisinden üretir.
Hazır bir listeden seçilmez — üretilir, ve dünyadaki her telefonda birebir
aynı üretilir. Senin deliğin benim deliğim. Senin parın benim parım. Bir
arkadaşınla skor karşılaştırdığında aynı vuruşu karşılaştırıyorsun.

Üstelik bir delik kimseye gösterilmeden önce oyun onun bitirilebilir
olduğunu kanıtlar. Üretici, yaptığı her sahayı kendisi çözer ve çözemediğini
atar — yani bir delik asla haksız değildir, sadece zordur.

İÇİNDE NE VAR
• Günlük delik — günde bir, par 2 ya da 3. İstediğin kadar tekrar dene;
  günün cevabı ilk bitirişindir.
• Yolculuk — oyunun tamamını öğreten, elle seçilmiş 100 seviye: boş bir
  koridordan yel değirmenlerine ve portallara.
• Antrenman — Kolay, Normal ve Zor'da bitmeyen taze sahalar.
• Haftalık Gauntlet — bir haftanın delikleri arka arkaya, tek skor.
• Arşiv — kaçırdığın günlere dönüp oyna.

HER ŞEYDEN SEKER
Tamponlar, kum, buz, su, tek yönlü kapılar, rampalar, portallar ve dönen yel
değirmeni kanatları. Seyrek görülen temalı günler bütün sahayı buzlu,
zıplak ya da rüzgârlı yapar — rüzgâr sahanın üstünde çizilir, yönüne göre
oynayabilirsin.

BİR TUR, OTUZ KARAKTER
Fizik sabit noktalıdır ve birebir tekrar üretilebilir; bu yüzden koca bir tur
kısa bir koda sığar. Arkadaşına gönder, senin topunun senin çizgini
vuruş vuruş takip edişini kendi telefonunda izlesin.

REKLAM YOK. TAKİP YOK. İNTERNET YOK.
PUTTSEED'in internet izni yoktur — hiçbir şeyi hiçbir yere gönderemez.
İstedikleri: titreşim (topun çarptığında hissettiğin dokunuş) ve — yalnızca
isteğe bağlı günlük hatırlatıcıyı açarsan — bildirim. Hesap yok, analitik
yok, satın alınacak bir şey yok. İlerlemen telefonunda durur, başka hiçbir
yerde.

Türkçe ve İngilizce. Renk körlüğü paleti, hareket azaltma ve 60 fps pil
modu.
```

---

## Console answers (not store copy — for filling the forms)

**App content → Privacy policy:** the URL where `privacy-policy.md` is
published.

**Data safety:** no data collected, no data shared, no data types at all.
Nothing is transmitted off the device — the app has no internet permission,
which is the strongest possible form of that answer. (Verified from the built
APK before the reminder feature: VIBRATE plus an internal AndroidX receiver
permission. The daily-reminder feature adds POST_NOTIFICATIONS, requested at
runtime only when the player opts in — RE-VERIFY with aapt2 on the next APK
before filling the form.) Data is not encrypted in
transit because there is no transit; there is no deletion request mechanism
because there is nothing held to delete.

**Ads:** contains no ads.

**Content rating (IARC questionnaire):** no violence, no sexuality, no
profanity, no gambling simulation, no user-to-user communication, no location
sharing, no purchases. Expect the lowest bracket (PEGI 3 / Everyone).

**Target audience:** general audience, not designed for children — the game
is suitable for all ages but declaring a child audience pulls in the Families
policy and its extra requirements for no benefit here.

**Category:** Games → Sports (or Puzzle — Sports matches the subject, Puzzle
matches the audience; the daily-puzzle crowd is the one that shares codes).

**Countries:** all, unless you want to start smaller.

## Asset checklist

| Asset | Requirement | Status |
|---|---|---|
| App icon | 512×512 PNG | done — `Assets/PuttSeed/Icon/app-icon.png` (rendered diorama) |
| Adaptive icon | 432×432 fg + bg | done — subject measured at 130px from centre, inside the 143px mask |
| Feature graphic | 1024×500 PNG | done — the icon's diorama on felt, `tools/feature-from-artwork.py` |
| Phone screenshots | 2–8, portrait | **TODO** — Play mode, `PuttSeed → Capture Screenshot` |
| Promo video | optional | not planned for launch |

Regenerate the feature graphic with `python tools/feature-from-artwork.py`
(it composes the icon's island onto generated felt, so icon and banner read
as one game). **PuttSeed → Generate Store Art** still draws the flat fallback
banner and the adaptive background; **Generate Fallback Icon** replaces the
drawn icon with the code-drawn stand-in and should only be used deliberately.

Screenshots worth taking, in this order: the daily hole mid-aim with the
power arc drawn, a hole-out with the star reveal, a windy day showing the
vane, the menu with today's course pictured, and the Journey grid.
