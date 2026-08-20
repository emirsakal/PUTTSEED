# Drop a typeface here

Put ONE `.ttf` or `.otf` in this folder and run **PuttSeed → Rebuild Scenes**.
Every label in the game — the title, the stroke count, every button — is set
in it from then on. With the folder empty the game falls back to Unity's
built-in Liberation Sans, which is the loudest "prototype" signal a UI can
send.

Two requirements:

- **A license that allows shipping.** SIL Open Font License is the safe
  default (Inter, Outfit, Space Grotesk, Bricolage Grotesque, Figtree). The
  font is embedded in the build, so a desktop-only or personal-use license is
  not enough.
- **Turkish coverage.** The UI uses ı, İ, ğ, ş, ç, ö and ü. A face missing
  the dotless ı will show tofu on half the Turkish strings.

The game reads whichever font it finds first, so keep exactly one here.
