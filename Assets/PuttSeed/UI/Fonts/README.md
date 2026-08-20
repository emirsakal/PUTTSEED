# The typeface

`active.txt` names the face the game is set in — one file name from
`Library/`. Change that line and run **PuttSeed → Rebuild Scenes**; the
title, the stroke count and every button change face at once. A font
dropped loose into this folder wins when `active.txt` names nothing, and
with neither the game falls back to Unity's built-in Liberation Sans.

Six candidates are in `Library/`, all SemiBold, because the UI never sets
bold or italic — the weight of the file *is* the weight of the game.

| Face | Verdict |
|---|---|
| `Outfit-SemiBold` | Active. Geometric and round; the O reads like a ball. |
| `Figtree-SemiBold` | Complete. Friendlier, easier over long tutorial lines. |
| `SpaceGrotesk-SemiBold` | Complete. More character, more opinion. |
| `Inter_24pt-SemiBold` | Complete. Neutral and flawless; adds no identity. |
| `Nunito-SemiBold` | **No right arrow (U+2192)** — the UI prints one everywhere. |
| `Fredoka-SemiBold` | **No ğ Ğ İ ş Ş.** Turkish would render as boxes. |

The last two are kept as evidence, not as options. `UiFontTests` checks
the active face against every character the UI can print — both sides of
the translation table, the chrome punctuation, and the month and weekday
names the culture supplies — so a face that cannot print Turkish fails a
test rather than a player's screen.

Emoji are the exception: the shot log's glyphs (⛳ 💧 🧊 …) are in no text
font and come from the OS fallback, which is where they came from before
this folder existed.

Each font keeps its `OFL-*.txt` beside it. The SIL Open Font License
requires the license to travel with the font, and these ship inside the
build.
