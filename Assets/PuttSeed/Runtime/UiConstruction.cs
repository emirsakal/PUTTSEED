#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Builds the full UI hierarchies and assigns the serialized references on
    /// the owning components. Called by the EDITOR scene builder (PuttSeed →
    /// Rebuild Scenes) so the result is saved INTO the scenes — editable in
    /// the Inspector, reskinnable with art assets — and is NOT rebuilt on
    /// Play. Runtime components only bind behavior to these references.
    /// </summary>
    public static class UiConstruction
    {
        /// <summary>Builds the menu scene UI under the menu root and wires its references.</summary>
        public static void BuildMenu(MenuBootstrap menu)
        {
            var canvas = UIFactory.CreateCanvas(menu.transform);

            menu.deco1 = UIFactory.CreateCircle(canvas.transform, "Deco1",
                new Vector2(-0.25f, 0.62f), new Vector2(0.35f, 0.96f), new Color(1f, 1f, 1f, 0.05f))
                .rectTransform;
            menu.deco2 = UIFactory.CreateCircle(canvas.transform, "Deco2",
                new Vector2(0.7f, -0.12f), new Vector2(1.35f, 0.25f), new Color(1f, 1f, 1f, 0.05f))
                .rectTransform;

            UIFactory.CreateCircle(canvas.transform, "EmblemHole",
                new Vector2(0.44f, 0.855f), new Vector2(0.56f, 0.885f), new Color(0.05f, 0.09f, 0.06f, 0.9f));
            var pole = UIFactory.CreateRect(canvas.transform, "EmblemPole",
                new Vector2(0.496f, 0.87f), new Vector2(0.504f, 0.955f));
            var poleImage = pole.gameObject.AddComponent<Image>();
            poleImage.color = UIStyle.Cream;
            poleImage.raycastTarget = false;
            // A pennant, not a rectangle — the same triangle of cloth the cup
            // flies out on the course. Pivoted at the pole edge so it can wave
            // from where it is actually tied (MenuBootstrap drives it).
            var flagRect = UIFactory.CreateRect(canvas.transform, "EmblemFlag",
                new Vector2(0.504f, 0.915f), new Vector2(0.63f, 0.952f));
            flagRect.pivot = new Vector2(0f, 0.5f);
            var flagImage = flagRect.gameObject.AddComponent<Image>();
            flagImage.sprite = UIFactory.PennantSprite();
            flagImage.color = PaletteMaterials.Flag;
            flagImage.raycastTarget = false;
            menu.emblemFlag = flagRect;
            var ballRect = UIFactory.CreateRect(canvas.transform, "EmblemBall",
                new Vector2(0.425f, 0.8765f), new Vector2(0.425f, 0.8765f));
            ballRect.sizeDelta = new Vector2(48f, 48f);
            var ballImage = ballRect.gameObject.AddComponent<Image>();
            ballImage.sprite = UIFactory.CircleSprite();
            ballImage.color = UIStyle.Cream;
            ballImage.raycastTarget = false;
            menu.emblemBall = ballRect; // idle animation target (rolls into the cup)

            var title = UIFactory.CreateText(canvas.transform, "Title",
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.85f), 124, TextAnchor.MiddleCenter, shadow: true);
            title.text = "PUTTSEED";

            var tagline = UIFactory.CreateText(canvas.transform, "Tagline",
                new Vector2(0.05f, 0.705f), new Vector2(0.95f, 0.745f), 33, TextAnchor.MiddleCenter);
            tagline.text = "one hole a day · same for everyone";
            tagline.color = UIStyle.CreamDim;
            menu.taglineText = tagline;

            UIFactory.CreatePanel(canvas.transform, "Card",
                new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.67f), UIStyle.PanelSoft);

            menu.dailyLabel = UIFactory.CreateButton(canvas.transform, "Play today's hole",
                new Vector2(0.1f, 0.545f), new Vector2(0.9f, 0.635f), NoOp, 44, primary: true);
            menu.dailyButton = menu.dailyLabel.GetComponentInParent<Button>();

            // Next-hole countdown; MenuBootstrap fills it once today is done.
            menu.countdownText = UIFactory.CreateText(canvas.transform, "Countdown",
                new Vector2(0.1f, 0.517f), new Vector2(0.9f, 0.545f), 26, TextAnchor.MiddleCenter);
            menu.countdownText.color = UIStyle.CreamDim;
            menu.countdownText.gameObject.SetActive(false);

            // Three rows on ONE grid. They used to drift — the Journey row
            // split its columns at 0.62/0.64 while the two below it split at
            // 0.60/0.62, and the row heights disagreed in the third decimal —
            // which is invisible in code and obvious on a phone.
            const float colLeftMin = 0.10f;
            const float colLeftMax = 0.60f;
            const float colRightMin = 0.62f;
            const float colRightMax = 0.90f;
            const float rowJourney = 0.434f;
            const float rowPractice = 0.3505f;
            const float rowTutorial = 0.267f;
            const float rowHeight = 0.074f;
            const int leftFont = 36;
            const int rightFont = 30;

            menu.journeyLabel = UIFactory.CreateButton(canvas.transform, "Journey",
                new Vector2(colLeftMin, rowJourney), new Vector2(colLeftMax, rowJourney + rowHeight),
                NoOp, leftFont);
            menu.journeyButton = menu.journeyLabel.GetComponentInParent<Button>();

            // The gauntlet is a mode, not an archive tool: it belongs on the
            // menu beside Journey, where a player can find it without first
            // going looking through the calendar.
            menu.gauntletLabel = UIFactory.CreateButton(canvas.transform, "Gauntlet",
                new Vector2(colRightMin, rowJourney), new Vector2(colRightMax, rowJourney + rowHeight),
                NoOp, rightFont);
            menu.gauntletButton = menu.gauntletLabel.GetComponentInParent<Button>();

            var practiceLabel = UIFactory.CreateButton(canvas.transform, "Practice",
                new Vector2(colLeftMin, rowPractice), new Vector2(colLeftMax, rowPractice + rowHeight),
                NoOp, leftFont);
            menu.practiceButton = practiceLabel.GetComponentInParent<Button>();

            menu.difficultyLabel = UIFactory.CreateButton(canvas.transform, "Normal",
                new Vector2(colRightMin, rowPractice), new Vector2(colRightMax, rowPractice + rowHeight),
                NoOp, rightFont);
            menu.difficultyButton = menu.difficultyLabel.GetComponentInParent<Button>();

            menu.tutorialLabel = UIFactory.CreateButton(canvas.transform, "Tutorial",
                new Vector2(colLeftMin, rowTutorial), new Vector2(colLeftMax, rowTutorial + rowHeight),
                NoOp, leftFont);
            menu.tutorialButton = menu.tutorialLabel.GetComponentInParent<Button>();

            var archiveLabel = UIFactory.CreateButton(canvas.transform, "Archive",
                new Vector2(colRightMin, rowTutorial), new Vector2(colRightMax, rowTutorial + rowHeight),
                NoOp, rightFont);
            menu.archiveButton = archiveLabel.GetComponentInParent<Button>();

            UIFactory.CreatePanel(canvas.transform, "FooterChip",
                new Vector2(0.14f, 0.196f), new Vector2(0.86f, 0.241f), UIStyle.PanelSoft);
            menu.footerText = UIFactory.CreateText(canvas.transform, "Footer",
                new Vector2(0.14f, 0.196f), new Vector2(0.86f, 0.241f), 30, TextAnchor.MiddleCenter);
            menu.footerText.color = UIStyle.CreamDim;

            // Invisible hit area: tapping the stats chip opens the stats panel.
            var footerHit = UIFactory.CreateRect(canvas.transform, "FooterHit",
                new Vector2(0.14f, 0.196f), new Vector2(0.86f, 0.241f));
            var footerHitImage = footerHit.gameObject.AddComponent<Image>();
            footerHitImage.color = Color.clear;
            footerHitImage.raycastTarget = true;
            menu.statsButton = footerHit.gameObject.AddComponent<Button>();
            footerHit.gameObject.AddComponent<UiClickSound>();

            // The menu stays clean: everything configurable lives behind two
            // buttons — Settings (toggles) and Collection (ball skins).
            var settingsLabel = UIFactory.CreateButton(canvas.transform, "Settings",
                new Vector2(0.10f, 0.13f), new Vector2(0.48f, 0.185f), NoOp, 30);
            menu.settingsButton = settingsLabel.GetComponentInParent<Button>();
            var collectionLabel = UIFactory.CreateButton(canvas.transform, "Collection",
                new Vector2(0.52f, 0.13f), new Vector2(0.90f, 0.185f), NoOp, 30);
            menu.collectionButton = collectionLabel.GetComponentInParent<Button>();

            // Today's hole, drawn from the day's own seed — the one thing on
            // this screen that is different every morning, and a second way
            // into the daily. It sits in the band under Settings/Collection,
            // the only empty space on the menu, so nothing above it moves.
            var todayCard = UIFactory.CreateRect(canvas.transform, "TodayCard",
                new Vector2(0.27f, 0.022f), new Vector2(0.73f, 0.122f));
            var todayImage = todayCard.gameObject.AddComponent<Image>();
            todayImage.preserveAspect = true; // the hole's own aspect, never stretched
            todayImage.raycastTarget = true;
            menu.todayThumb = todayImage;
            menu.todayThumbButton = todayCard.gameObject.AddComponent<Button>();
            menu.todayThumbButton.targetGraphic = todayImage;
            todayCard.gameObject.AddComponent<UiClickSound>();
            todayCard.gameObject.AddComponent<ButtonPressScale>();
            todayCard.gameObject.SetActive(false); // shown when the render lands

            // Pop-ups mount on the CANVAS root, not the safe-area root: their
            // dim backdrops must cover the whole screen, notch included (the
            // cards themselves are centered, so cutouts never cover content).
            var canvasRoot = canvas.transform.parent;
            BuildArchivePanel(canvasRoot, menu);
            BuildStatsPanel(canvasRoot, menu);
            BuildSettingsPanel(canvasRoot, menu);
            BuildCollectionPanel(canvasRoot, menu);
            BuildJourneyPanel(canvasRoot, menu);
        }

        /// <summary>
        /// The journey level select: a 5x5 grid per page (25 levels each; the
        /// pager steps through as many pages as the campaign needs) — number on
        /// top, three tiny stars underneath; locked cells sit dim and dead
        /// until the previous level is completed.
        /// </summary>
        private static void BuildJourneyPanel(Transform canvas, MenuBootstrap menu)
        {
            var dim = UIFactory.CreateRect(canvas, "JourneyPanel", Vector2.zero, Vector2.one);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.03f, 0.07f, 0.05f, 0.93f);
            dimImage.raycastTarget = true;
            menu.journeyPanel = dim.gameObject;

            UIFactory.CreateFramedCard(dim, "Card",
                new Vector2(0.07f, 0.13f), new Vector2(0.93f, 0.80f));
            var title = UIFactory.CreateText(dim, "Title",
                new Vector2(0.1f, 0.725f), new Vector2(0.9f, 0.785f), 52, TextAnchor.MiddleCenter, shadow: true);
            title.text = "Journey";

            const int columns = 5;
            const int rows = 5;
            menu.journeyCellButtons = new Button[columns * rows];
            menu.journeyCellLabels = new Text[columns * rows];
            menu.journeyCellStars = new Image[columns * rows * 3];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int cell = r * columns + c;
                    float x0 = 0.12f + c * 0.1565f;
                    float yMax = 0.70f - r * 0.082f;
                    var cellLabel = UIFactory.CreateButton(dim, "—",
                        new Vector2(x0, yMax - 0.068f), new Vector2(x0 + 0.134f, yMax), NoOp, 28);
                    var labelRect = cellLabel.rectTransform;
                    labelRect.anchorMin = new Vector2(0f, 0.38f);
                    labelRect.anchorMax = new Vector2(1f, 1f);
                    menu.journeyCellLabels[cell] = cellLabel;
                    menu.journeyCellButtons[cell] = cellLabel.GetComponentInParent<Button>();

                    for (int s = 0; s < 3; s++)
                    {
                        var starRect = UIFactory.CreateRect(menu.journeyCellButtons[cell].transform,
                            $"Star{s + 1}",
                            new Vector2(0.16f + s * 0.26f, 0.08f),
                            new Vector2(0.36f + s * 0.26f, 0.36f));
                        var starImage = starRect.gameObject.AddComponent<Image>();
                        starImage.sprite = UIFactory.StarSprite();
                        starImage.preserveAspect = true;
                        starImage.raycastTarget = false;
                        menu.journeyCellStars[cell * 3 + s] = starImage;
                    }
                }
            }

            // Pager arrows are language-free; MenuBootstrap fills the center
            // label with the visible range and the star total.
            var older = UIFactory.CreateButton(dim, "‹",
                new Vector2(0.12f, 0.235f), new Vector2(0.28f, 0.288f), NoOp, 30);
            menu.journeyPrevButton = older.GetComponentInParent<Button>();
            menu.journeyPageLabel = UIFactory.CreateText(dim, "Page",
                new Vector2(0.29f, 0.235f), new Vector2(0.71f, 0.288f), 26, TextAnchor.MiddleCenter);
            menu.journeyPageLabel.color = UIStyle.CreamDim;
            var newer = UIFactory.CreateButton(dim, "›",
                new Vector2(0.72f, 0.235f), new Vector2(0.88f, 0.288f), NoOp, 30);
            menu.journeyNextButton = newer.GetComponentInParent<Button>();

            var close = UIFactory.CreateButton(dim, "Close",
                new Vector2(0.3f, 0.155f), new Vector2(0.7f, 0.215f), NoOp, 30, primary: true);
            menu.journeyCloseButton = close.GetComponentInParent<Button>();
            menu.journeyCloseButton.GetComponent<UiClickSound>().downTone = true;

            menu.journeyPanel.SetActive(false);
        }

        /// <summary>The settings overlay: every toggle chip in one place.</summary>
        private static void BuildSettingsPanel(Transform canvas, MenuBootstrap menu)
        {
            var dim = UIFactory.CreateRect(canvas, "SettingsPanel", Vector2.zero, Vector2.one);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.03f, 0.07f, 0.05f, 0.93f);
            dimImage.raycastTarget = true;
            menu.settingsPanel = dim.gameObject;

            UIFactory.CreateFramedCard(dim, "Card",
                new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.83f));
            var title = UIFactory.CreateText(dim, "Title",
                new Vector2(0.1f, 0.745f), new Vector2(0.9f, 0.805f), 52, TextAnchor.MiddleCenter, shadow: true);
            title.text = "Settings";

            menu.soundToggle = CreateSettingRow(dim, 0.71f, "Sound", "On", "Off");
            menu.hapticsToggle = CreateSettingRow(dim, 0.632f, "Haptics", "On", "Off");
            menu.aimToggle = CreateSettingRow(dim, 0.554f, "Aim", "Sling", "Direct");
            menu.colorblindToggle = CreateSettingRow(dim, 0.476f, "Colors", "Std", "Vivid");
            menu.batteryToggle = CreateSettingRow(dim, 0.398f, "FPS", "120", "60");
            menu.languageToggle = CreateSettingRow(dim, 0.32f, "Language", "EN", "TR");

            var close = UIFactory.CreateButton(dim, "Close",
                new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.24f), NoOp, 30, primary: true);
            menu.settingsCloseButton = close.GetComponentInParent<Button>();
            menu.settingsCloseButton.GetComponent<UiClickSound>().downTone = true;

            menu.settingsPanel.SetActive(false);
        }

        /// <summary>
        /// One settings row: the label on the left, a two-segment selector on
        /// the right. Tapping a segment SELECTS that option — the active side
        /// fills amber, so the current state is readable at a glance.
        /// </summary>
        private static SegmentedToggle CreateSettingRow(RectTransform dim, float yMax,
            string label, string optionA, string optionB)
        {
            var row = UIFactory.CreateRect(dim, $"Row{label}",
                new Vector2(0.15f, yMax - 0.058f), new Vector2(0.85f, yMax));
            var toggle = row.gameObject.AddComponent<SegmentedToggle>();

            var rowLabel = UIFactory.CreateText(row, "Label",
                new Vector2(0.02f, 0f), new Vector2(0.40f, 1f), 30, TextAnchor.MiddleLeft);
            rowLabel.text = label;

            toggle.optionALabel = UIFactory.CreateButton(row, optionA,
                new Vector2(0.44f, 0.06f), new Vector2(0.70f, 0.94f), NoOp, 26);
            toggle.optionAButton = toggle.optionALabel.GetComponentInParent<Button>();
            toggle.optionABg = toggle.optionAButton.GetComponent<Image>();

            toggle.optionBLabel = UIFactory.CreateButton(row, optionB,
                new Vector2(0.72f, 0.06f), new Vector2(0.98f, 0.94f), NoOp, 26);
            toggle.optionBButton = toggle.optionBLabel.GetComponentInParent<Button>();
            toggle.optionBBg = toggle.optionBButton.GetComponent<Image>();

            return toggle;
        }

        /// <summary>
        /// The collection overlay: ball skins as a square grid — big swatch on
        /// top, name underneath. The equipped cell wears an accent ring;
        /// locked cells sit dim and tapping one explains its unlock in the
        /// hint line under the grid.
        /// </summary>
        private static void BuildCollectionPanel(Transform canvas, MenuBootstrap menu)
        {
            var dim = UIFactory.CreateRect(canvas, "CollectionPanel", Vector2.zero, Vector2.one);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.03f, 0.07f, 0.05f, 0.93f);
            dimImage.raycastTarget = true;
            menu.collectionPanel = dim.gameObject;

            UIFactory.CreateFramedCard(dim, "Card",
                new Vector2(0.09f, 0.12f), new Vector2(0.91f, 0.80f));
            var title = UIFactory.CreateText(dim, "Title",
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.78f), 52, TextAnchor.MiddleCenter, shadow: true);
            title.text = "Collection";

            const int columns = 4;
            // Balls and trails share one grid, swapped by a segmented tab —
            // a second grid would not fit the card, and paging a dozen cells
            // is worse than one clear switch.
            menu.collectionTab = CreateSettingRow(dim, 0.695f, "Show", "Balls", "Trails");

            int count = Mathf.Max(BallSkins.All.Length, BallTrails.All.Length);
            menu.collectionCellButtons = new Button[count];
            menu.collectionCellLabels = new Text[count];
            menu.collectionCellSwatches = new Image[count];
            menu.collectionCellRings = new Image[count];
            for (int i = 0; i < count; i++)
            {
                int r = i / columns;
                int c = i % columns;
                float x0 = 0.135f + c * 0.19f;
                float yMax = 0.625f - r * 0.115f;
                var cellLabel = UIFactory.CreateButton(dim, "—",
                    new Vector2(x0, yMax - 0.101f), new Vector2(x0 + 0.175f, yMax), NoOp, 20);
                var labelRect = cellLabel.rectTransform;
                labelRect.anchorMin = new Vector2(0.03f, 0.04f);
                labelRect.anchorMax = new Vector2(0.97f, 0.30f);
                menu.collectionCellLabels[i] = cellLabel;
                menu.collectionCellButtons[i] = cellLabel.GetComponentInParent<Button>();

                // Accent ring behind the swatch: visible only on the equipped
                // cell (MenuBootstrap toggles it).
                var ring = UIFactory.CreateRect(menu.collectionCellButtons[i].transform, "Ring",
                    new Vector2(0.26f, 0.33f), new Vector2(0.74f, 0.95f));
                var ringImage = ring.gameObject.AddComponent<Image>();
                ringImage.sprite = UIFactory.CircleSprite();
                ringImage.preserveAspect = true;
                ringImage.raycastTarget = false;
                ringImage.color = UIStyle.Accent;
                menu.collectionCellRings[i] = ringImage;

                var swatch = UIFactory.CreateRect(menu.collectionCellButtons[i].transform, "Swatch",
                    new Vector2(0.31f, 0.38f), new Vector2(0.69f, 0.90f));
                var swatchImage = swatch.gameObject.AddComponent<Image>();
                swatchImage.sprite = UIFactory.CircleSprite();
                swatchImage.preserveAspect = true;
                swatchImage.raycastTarget = false;
                menu.collectionCellSwatches[i] = swatchImage;
            }

            // Status line: equipped skin, or the unlock hint of a tapped
            // locked cell.
            // Clears the third grid row (bottom 0.294) and the Close button
            // (top 0.21) — the panel's tightest sandwich.
            menu.collectionHintText = UIFactory.CreateText(dim, "Hint",
                new Vector2(0.12f, 0.222f), new Vector2(0.88f, 0.286f), 24, TextAnchor.MiddleCenter);
            menu.collectionHintText.color = UIStyle.CreamDim;

            var close = UIFactory.CreateButton(dim, "Close",
                new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.21f), NoOp, 30, primary: true);
            menu.collectionCloseButton = close.GetComponentInParent<Button>();
            menu.collectionCloseButton.GetComponent<UiClickSound>().downTone = true;

            menu.collectionPanel.SetActive(false);
        }

        /// <summary>
        /// The stats overlay: number lines from the save plus the achievement
        /// catalog (rich-text lines, unlocked bright / locked dim).
        /// </summary>
        private static void BuildStatsPanel(Transform canvas, MenuBootstrap menu)
        {
            var dim = UIFactory.CreateRect(canvas, "StatsPanel", Vector2.zero, Vector2.one);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.03f, 0.07f, 0.05f, 0.93f);
            dimImage.raycastTarget = true;
            menu.statsPanel = dim.gameObject;

            UIFactory.CreateFramedCard(dim, "Card",
                new Vector2(0.07f, 0.10f), new Vector2(0.93f, 0.84f));
            var title = UIFactory.CreateText(dim, "Title",
                new Vector2(0.1f, 0.765f), new Vector2(0.9f, 0.82f), 52, TextAnchor.MiddleCenter, shadow: true);
            title.text = "Stats";

            menu.statsBlock = UIFactory.CreateText(dim, "StatsBlock",
                new Vector2(0.13f, 0.545f), new Vector2(0.64f, 0.76f), 26, TextAnchor.UpperLeft);

            // Stroke distribution: a titled column of bullet bars. Narrow, so
            // the stats column has room to spell its labels out.
            menu.histogramBlock = UIFactory.CreateText(dim, "Histogram",
                new Vector2(0.66f, 0.545f), new Vector2(0.88f, 0.76f), 24, TextAnchor.UpperLeft);
            menu.histogramBlock.color = UIStyle.CreamDim;

            var achTitle = UIFactory.CreateText(dim, "AchTitle",
                new Vector2(0.13f, 0.515f), new Vector2(0.87f, 0.56f), 36, TextAnchor.MiddleLeft);
            achTitle.text = "Achievements";
            achTitle.color = UIStyle.Hint;

            menu.achievementsBlock = UIFactory.CreateText(dim, "AchBlock",
                new Vector2(0.13f, 0.26f), new Vector2(0.87f, 0.51f), 26, TextAnchor.UpperLeft);

            // Save transfer: export copies a PUTTSAVE- code; import reads one
            // pasted into the field (double-tap confirms the overwrite).
            menu.saveField = UIFactory.CreateInputField(dim,
                new Vector2(0.12f, 0.20f), new Vector2(0.55f, 0.25f), "paste PUTTSAVE- code…");
            menu.importSaveLabel = UIFactory.CreateButton(dim, "Import",
                new Vector2(0.58f, 0.20f), new Vector2(0.88f, 0.25f), NoOp, 24);
            menu.importSaveButton = menu.importSaveLabel.GetComponentInParent<Button>();

            menu.exportSaveLabel = UIFactory.CreateButton(dim, "Export save",
                new Vector2(0.12f, 0.135f), new Vector2(0.42f, 0.19f), NoOp, 24);
            menu.exportSaveButton = menu.exportSaveLabel.GetComponentInParent<Button>();
            menu.shareBestLabel = UIFactory.CreateButton(dim, "Share best",
                new Vector2(0.45f, 0.135f), new Vector2(0.72f, 0.19f), NoOp, 24);
            menu.shareBestButton = menu.shareBestLabel.GetComponentInParent<Button>();

            var close = UIFactory.CreateButton(dim, "Close",
                new Vector2(0.75f, 0.135f), new Vector2(0.88f, 0.19f), NoOp, 26, primary: true);
            menu.statsCloseButton = close.GetComponentInParent<Button>();
            menu.statsCloseButton.GetComponent<UiClickSound>().downTone = true;

            menu.statsPanel.SetActive(false);
        }

        /// <summary>
        /// The archive overlay: seven past-day rows plus paging. Any past date
        /// regenerates its course from the date alone, so browsing needs no
        /// storage — MenuBootstrap only fills labels from the local records.
        /// </summary>
        /// <summary>
        /// The archive as a month calendar: a 7x6 grid where every square is a
        /// day, played days wear their stars, and the gaps are the point — an
        /// unplayed square is a course still waiting, and any past date
        /// regenerates from the date alone.
        /// </summary>
        private static void BuildArchivePanel(Transform canvas, MenuBootstrap menu)
        {
            var dim = UIFactory.CreateRect(canvas, "ArchivePanel", Vector2.zero, Vector2.one);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0.03f, 0.07f, 0.05f, 0.93f);
            dimImage.raycastTarget = true; // swallow clicks under the panel
            menu.archivePanel = dim.gameObject;

            UIFactory.CreateFramedCard(dim, "Card",
                new Vector2(0.07f, 0.12f), new Vector2(0.93f, 0.84f));
            var title = UIFactory.CreateText(dim, "Title",
                new Vector2(0.1f, 0.755f), new Vector2(0.9f, 0.815f), 52, TextAnchor.MiddleCenter, shadow: true);
            title.text = "Archive";

            // Month header: arrows either side of the month name.
            var prev = UIFactory.CreateButton(dim, "\u2039",
                new Vector2(0.12f, 0.695f), new Vector2(0.24f, 0.745f), NoOp, 30);
            menu.archivePrevMonthButton = prev.GetComponentInParent<Button>();
            menu.archiveMonthLabel = UIFactory.CreateText(dim, "Month",
                new Vector2(0.26f, 0.695f), new Vector2(0.74f, 0.745f), 30, TextAnchor.MiddleCenter);
            var next = UIFactory.CreateButton(dim, "\u203a",
                new Vector2(0.76f, 0.695f), new Vector2(0.88f, 0.745f), NoOp, 30);
            menu.archiveNextMonthButton = next.GetComponentInParent<Button>();

            const int columns = 7;
            const int rows = 6;
            const float gridLeft = 0.10f;
            const float columnStep = 0.11428f;
            const float cellWidth = 0.104f;
            const float gridTop = 0.645f;
            const float rowStep = 0.074f;
            const float cellHeight = 0.066f;

            // Weekday initials, filled at refresh so a language switch reorders
            // them (Monday-first in Turkish, Sunday-first in English).
            menu.archiveWeekdayLabels = new Text[columns];
            for (int c = 0; c < columns; c++)
            {
                float x0 = gridLeft + c * columnStep;
                var day = UIFactory.CreateText(dim, $"Weekday{c}",
                    new Vector2(x0, 0.652f), new Vector2(x0 + cellWidth, 0.682f), 20,
                    TextAnchor.MiddleCenter);
                day.color = UIStyle.CreamDim;
                menu.archiveWeekdayLabels[c] = day;
            }

            menu.archiveCellButtons = new Button[columns * rows];
            menu.archiveCellLabels = new Text[columns * rows];
            menu.archiveCellStars = new Image[columns * rows * 3];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int cell = r * columns + c;
                    float x0 = gridLeft + c * columnStep;
                    float yMax = gridTop - r * rowStep;
                    var cellLabel = UIFactory.CreateButton(dim, "—",
                        new Vector2(x0, yMax - cellHeight), new Vector2(x0 + cellWidth, yMax), NoOp, 22);
                    var labelRect = cellLabel.rectTransform;
                    labelRect.anchorMin = new Vector2(0f, 0.40f);
                    labelRect.anchorMax = new Vector2(1f, 1f);
                    menu.archiveCellLabels[cell] = cellLabel;
                    menu.archiveCellButtons[cell] = cellLabel.GetComponentInParent<Button>();

                    for (int st = 0; st < 3; st++)
                    {
                        var starRect = UIFactory.CreateRect(menu.archiveCellButtons[cell].transform,
                            $"Star{st + 1}",
                            new Vector2(0.14f + st * 0.25f, 0.10f),
                            new Vector2(0.32f + st * 0.25f, 0.36f));
                        var starImage = starRect.gameObject.AddComponent<Image>();
                        starImage.sprite = UIFactory.StarSprite();
                        starImage.preserveAspect = true;
                        starImage.raycastTarget = false;
                        starRect.gameObject.SetActive(false);
                        menu.archiveCellStars[cell * 3 + st] = starImage;
                    }
                }
            }

            var randomLabel = UIFactory.CreateButton(dim, "Random day",
                new Vector2(0.12f, 0.135f), new Vector2(0.48f, 0.19f), NoOp, 26);
            menu.archiveRandomButton = randomLabel.GetComponentInParent<Button>();

            var close = UIFactory.CreateButton(dim, "Close",
                new Vector2(0.52f, 0.135f), new Vector2(0.88f, 0.19f), NoOp, 30, primary: true);
            menu.archiveCloseButton = close.GetComponentInParent<Button>();
            menu.archiveCloseButton.GetComponent<UiClickSound>().downTone = true;

            menu.archivePanel.SetActive(false);
        }

        /// <summary>Builds the in-game HUD under the UI root and wires its references.</summary>
        public static void BuildGameHud(GameUI ui)
        {
            var canvas = UIFactory.CreateCanvas(ui.transform);

            UIFactory.CreatePanel(canvas.transform, "TopBar",
                new Vector2(0.02f, 0.925f), new Vector2(0.98f, 0.985f), UIStyle.PanelSoft);

            // Golf's own hierarchy. The bar used to be one string at one size,
            // so the word "Daily" shouted as loudly as the score: the stroke
            // count is the number being played against, the chip beside it is
            // where that stands against par — the thing a golfer actually
            // reads — and mode, par and streak are small print on the right.
            ui.strokeText = UIFactory.CreateText(canvas.transform, "Strokes",
                new Vector2(0.05f, 0.925f), new Vector2(0.25f, 0.985f), 58, TextAnchor.MiddleLeft);

            var parChip = UIFactory.CreatePanel(canvas.transform, "ParChip",
                new Vector2(0.255f, 0.938f), new Vector2(0.365f, 0.974f), UIStyle.PanelDark);
            ui.parChip = parChip.gameObject;
            ui.parChipText = UIFactory.CreateText(parChip.transform, "ParChipText",
                Vector2.zero, Vector2.one, 27, TextAnchor.MiddleCenter);

            ui.counterText = UIFactory.CreateText(canvas.transform, "Counter",
                new Vector2(0.38f, 0.925f), new Vector2(0.95f, 0.985f), 25, TextAnchor.MiddleRight);
            ui.counterText.color = UIStyle.CreamDim;

            var hintChip = UIFactory.CreatePanel(canvas.transform, "HintChip",
                new Vector2(0.06f, 0.865f), new Vector2(0.94f, 0.915f), UIStyle.PanelSoft);
            ui.hintChip = hintChip.gameObject;
            ui.hintText = UIFactory.CreateText(ui.hintChip.transform, "Hint",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 30, TextAnchor.MiddleCenter);
            ui.hintText.color = UIStyle.Hint;

            ui.statusText = UIFactory.CreateText(canvas.transform, "Status",
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f), 76, TextAnchor.MiddleCenter, shadow: true);

            // Star row (GDD scoring: 3 at par or better / 2 one over / 1 within limit) —
            // shown by GameUI on hole-out, dimmed stars stay as empty slots.
            var starsRow = UIFactory.CreateRect(canvas.transform, "StarsRow",
                new Vector2(0.32f, 0.72f), new Vector2(0.68f, 0.79f));
            ui.starsRow = starsRow.gameObject;
            ui.starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var starRect = UIFactory.CreateRect(starsRow, $"Star{i + 1}",
                    new Vector2(i / 3f + 0.03f, 0f), new Vector2((i + 1) / 3f - 0.03f, 1f));
                var starImage = starRect.gameObject.AddComponent<Image>();
                starImage.sprite = UIFactory.StarSprite();
                starImage.preserveAspect = true;
                starImage.raycastTarget = false;
                var starShadow = starRect.gameObject.AddComponent<Shadow>();
                starShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
                starShadow.effectDistance = new Vector2(0f, -3f);
                ui.starImages[i] = starImage;
            }

            ui.starsRow.SetActive(false);

            // Fail panel: a proper card for the stroke-limit fail state (the
            // status line alone was easy to miss). GameUI toggles it.
            var failPanel = UIFactory.CreatePanel(canvas.transform, "FailPanel",
                new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.62f), UIStyle.PanelDark);
            ui.failPanel = failPanel.gameObject;
            var failTitle = UIFactory.CreateText(failPanel.transform, "FailTitle",
                new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.94f), 56, TextAnchor.MiddleCenter, shadow: true);
            failTitle.text = "Out of strokes!";
            var failSub = UIFactory.CreateText(failPanel.transform, "FailSubtitle",
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.6f), 28, TextAnchor.MiddleCenter);
            failSub.text = "The limit is par + 3 — line up and go again.";
            failSub.color = UIStyle.CreamDim;
            ui.failRetryButton = ButtonOf(UIFactory.CreateButton(failPanel.transform, "Retry",
                new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.36f), NoOp, 36, primary: true));
            ui.failPanel.SetActive(false);

            // The day's closing card. The ritual had no end: the result, the
            // streak, the countdown and the share button lived on three
            // different screens, and the last thing a player saw after holing
            // out was the same course they had just finished. It sits clear of
            // the status line above (0.55) and the toast below (0.30).
            var dailyCard = UIFactory.CreatePanel(canvas.transform, "DailyCard",
                new Vector2(0.10f, 0.31f), new Vector2(0.90f, 0.54f), UIStyle.PanelDark);
            ui.dailyCard = dailyCard.gameObject;
            ui.dailyCardResult = UIFactory.CreateText(dailyCard.transform, "Result",
                new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.95f), 32, TextAnchor.MiddleCenter);
            ui.dailyCardStreak = UIFactory.CreateText(dailyCard.transform, "Streaks",
                new Vector2(0.05f, 0.46f), new Vector2(0.95f, 0.66f), 25, TextAnchor.MiddleCenter);
            ui.dailyCardStreak.color = UIStyle.CreamDim;
            ui.dailyCardCountdown = UIFactory.CreateText(dailyCard.transform, "Countdown",
                new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.46f), 24, TextAnchor.MiddleCenter);
            ui.dailyCardCountdown.color = UIStyle.CreamDim;
            ui.dailyShareButton = ButtonOf(UIFactory.CreateButton(dailyCard.transform, "Share",
                new Vector2(0.26f, 0.05f), new Vector2(0.74f, 0.26f), NoOp, 30, primary: true));
            ui.dailyCard.SetActive(false);

            var toastChip = UIFactory.CreatePanel(canvas.transform, "ToastChip",
                new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.3f), UIStyle.PanelDark);
            ui.toastChip = toastChip.gameObject;
            ui.toastText = UIFactory.CreateText(ui.toastChip.transform, "Toast",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 32, TextAnchor.MiddleCenter);
            ui.toastChip.SetActive(false);

            // Compact control bar: ONE row of six buttons — the old triple-row
            // bar ate a quarter of the screen and collided with tall courses.
            UIFactory.CreatePanel(canvas.transform, "BottomBar",
                new Vector2(0.01f, 0.008f), new Vector2(0.99f, 0.085f), UIStyle.PanelSoft);

            ui.menuButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Menu",
                new Vector2(0.02f, 0.016f), new Vector2(0.17f, 0.077f), NoOp, 25));
            ui.retryButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Retry",
                new Vector2(0.182f, 0.016f), new Vector2(0.332f, 0.077f), NoOp, 25));
            ui.shareButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Share",
                new Vector2(0.344f, 0.016f), new Vector2(0.494f, 0.077f), NoOp, 25));
            ui.ghostButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Ghost",
                new Vector2(0.506f, 0.016f), new Vector2(0.656f, 0.077f), NoOp, 25));
            ui.watchButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Watch",
                new Vector2(0.668f, 0.016f), new Vector2(0.818f, 0.077f), NoOp, 25));
            ui.undoButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Undo",
                new Vector2(0.83f, 0.016f), new Vector2(0.98f, 0.077f), NoOp, 25));

            // The paste field only exists while wanted: Watch opens this chip,
            // a second tap (or a successful import) closes it again.
            var importChip = UIFactory.CreatePanel(canvas.transform, "ImportChip",
                new Vector2(0.03f, 0.095f), new Vector2(0.97f, 0.152f), UIStyle.PanelDark);
            ui.importRow = importChip.gameObject;
            ui.importField = UIFactory.CreateInputField(importChip.transform,
                new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.92f), "paste PUTT- code…");
            ui.importRow.SetActive(false);

            // The advance button sits low and centred, where the thumb
            // already is — it used to ride the top-right corner, a reach away
            // from every other control. It clears the import chip (top 0.152)
            // and the toast (bottom 0.25), the two things that share this band.
            var next = UIFactory.CreateButton(canvas.transform, "Next lesson",
                new Vector2(0.30f, 0.16f), new Vector2(0.70f, 0.22f), NoOp, 30, primary: true);
            ui.nextLessonButton = ButtonOf(next);
        }

        /// <summary>Builds the loading cover under the overlay root and wires its references.</summary>
        public static void BuildLoadingOverlay(LoadingOverlay overlay)
        {
            var canvasGo = new GameObject("LoadingCanvas");
            canvasGo.transform.SetParent(overlay.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var cover = UIFactory.CreateRect(canvasGo.transform, "Cover", Vector2.zero, Vector2.one);
            var image = cover.gameObject.AddComponent<Image>();
            // The rough, not the felt: the cover lifts onto a course whose
            // ground is a mat on a slightly darker surround, and the two must
            // meet without a step in brightness.
            image.color = PaletteMaterials.Rough;
            image.raycastTarget = true;

            UIFactory.CreateCircle(cover, "Deco1",
                new Vector2(-0.2f, 0.7f), new Vector2(0.4f, 1.05f), new Color(1f, 1f, 1f, 0.05f));
            UIFactory.CreateCircle(cover, "Deco2",
                new Vector2(0.72f, -0.1f), new Vector2(1.3f, 0.22f), new Color(1f, 1f, 1f, 0.05f));

            // The putt vignette: cup on the right, ball on the left, club above
            // the ball pivoting at its grip. LoadingOverlay animates these.
            var holeRect = UIFactory.CreateRect(cover, "Hole",
                new Vector2(0.5f, 0.585f), new Vector2(0.5f, 0.585f));
            holeRect.sizeDelta = new Vector2(90f, 90f);
            holeRect.anchoredPosition = new Vector2(270f, -2f);
            var holeImage = holeRect.gameObject.AddComponent<Image>();
            holeImage.sprite = UIFactory.CircleSprite();
            holeImage.color = new Color(0.05f, 0.09f, 0.06f, 0.95f);
            holeImage.raycastTarget = false;
            overlay.hole = holeRect;

            var ballRect = UIFactory.CreateRect(cover, "Ball",
                new Vector2(0.5f, 0.585f), new Vector2(0.5f, 0.585f));
            ballRect.sizeDelta = new Vector2(44f, 44f);
            ballRect.anchoredPosition = new Vector2(-270f, 0f);
            var ballImage = ballRect.gameObject.AddComponent<Image>();
            ballImage.sprite = UIFactory.CircleSprite();
            ballImage.color = UIStyle.Cream;
            ballImage.raycastTarget = false;
            overlay.ball = ballRect;

            var clubRoot = UIFactory.CreateRect(cover, "Club",
                new Vector2(0.5f, 0.585f), new Vector2(0.5f, 0.585f));
            clubRoot.sizeDelta = new Vector2(10f, 10f);
            clubRoot.anchoredPosition = new Vector2(-296f, 168f); // grip: pivot of the swing
            overlay.club = clubRoot;

            var shaft = UIFactory.CreateRect(clubRoot, "Shaft", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            shaft.pivot = new Vector2(0.5f, 1f);
            shaft.sizeDelta = new Vector2(9f, 172f);
            shaft.anchoredPosition = Vector2.zero;
            var shaftImage = shaft.gameObject.AddComponent<Image>();
            shaftImage.color = new Color(0.85f, 0.83f, 0.78f);
            shaftImage.raycastTarget = false;

            var head = UIFactory.CreateRect(clubRoot, "Head", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            head.sizeDelta = new Vector2(46f, 20f);
            head.anchoredPosition = new Vector2(14f, -180f);
            var headImage = head.gameObject.AddComponent<Image>();
            headImage.sprite = UIFactory.RoundedSprite();
            headImage.type = Image.Type.Sliced;
            headImage.color = new Color(0.32f, 0.3f, 0.34f);
            headImage.raycastTarget = false;

            clubRoot.localEulerAngles = new Vector3(0f, 0f, -42f); // baked wind-up: head behind the ball

            overlay.label = UIFactory.CreateText(cover, "Label",
                new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f), 52, TextAnchor.MiddleCenter, shadow: true);
            overlay.root = canvasGo;
            canvasGo.SetActive(false);
        }


        private static Button ButtonOf(Text label) => label.GetComponentInParent<Button>(true);

        private static void NoOp()
        {
        }
    }
}
