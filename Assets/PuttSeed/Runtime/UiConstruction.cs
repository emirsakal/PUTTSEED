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

            UIFactory.CreateCircle(canvas.transform, "Deco1",
                new Vector2(-0.25f, 0.62f), new Vector2(0.35f, 0.96f), new Color(1f, 1f, 1f, 0.05f));
            UIFactory.CreateCircle(canvas.transform, "Deco2",
                new Vector2(0.7f, -0.12f), new Vector2(1.35f, 0.25f), new Color(1f, 1f, 1f, 0.05f));

            UIFactory.CreateCircle(canvas.transform, "EmblemHole",
                new Vector2(0.44f, 0.855f), new Vector2(0.56f, 0.885f), new Color(0.05f, 0.09f, 0.06f, 0.9f));
            var pole = UIFactory.CreateRect(canvas.transform, "EmblemPole",
                new Vector2(0.496f, 0.87f), new Vector2(0.504f, 0.955f));
            var poleImage = pole.gameObject.AddComponent<Image>();
            poleImage.color = UIStyle.Cream;
            poleImage.raycastTarget = false;
            UIFactory.CreatePanel(canvas.transform, "EmblemFlag",
                new Vector2(0.504f, 0.915f), new Vector2(0.63f, 0.952f), new Color(0.86f, 0.24f, 0.19f));
            var ballRect = UIFactory.CreateRect(canvas.transform, "EmblemBall",
                new Vector2(0.425f, 0.8765f), new Vector2(0.425f, 0.8765f));
            ballRect.sizeDelta = new Vector2(48f, 48f);
            var ballImage = ballRect.gameObject.AddComponent<Image>();
            ballImage.sprite = UIFactory.CircleSprite();
            ballImage.color = UIStyle.Cream;
            ballImage.raycastTarget = false;

            var title = UIFactory.CreateText(canvas.transform, "Title",
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.85f), 124, TextAnchor.MiddleCenter, shadow: true);
            title.text = "PUTTSEED";

            var tagline = UIFactory.CreateText(canvas.transform, "Tagline",
                new Vector2(0.05f, 0.705f), new Vector2(0.95f, 0.745f), 33, TextAnchor.MiddleCenter);
            tagline.text = "one hole a day · same for everyone";
            tagline.color = UIStyle.CreamDim;

            UIFactory.CreatePanel(canvas.transform, "Card",
                new Vector2(0.06f, 0.27f), new Vector2(0.94f, 0.67f), UIStyle.PanelSoft);

            menu.dailyLabel = UIFactory.CreateButton(canvas.transform, "Play today's hole",
                new Vector2(0.1f, 0.545f), new Vector2(0.9f, 0.635f), NoOp, 44, primary: true);
            menu.dailyButton = menu.dailyLabel.GetComponentInParent<Button>();

            var practiceLabel = UIFactory.CreateButton(canvas.transform, "Practice",
                new Vector2(0.1f, 0.43f), new Vector2(0.6f, 0.52f), NoOp, 44);
            menu.practiceButton = practiceLabel.GetComponentInParent<Button>();

            menu.difficultyLabel = UIFactory.CreateButton(canvas.transform, "Normal",
                new Vector2(0.62f, 0.43f), new Vector2(0.9f, 0.52f), NoOp, 36);
            menu.difficultyButton = menu.difficultyLabel.GetComponentInParent<Button>();

            menu.tutorialLabel = UIFactory.CreateButton(canvas.transform, "Tutorial",
                new Vector2(0.1f, 0.315f), new Vector2(0.9f, 0.405f), NoOp, 44);
            menu.tutorialButton = menu.tutorialLabel.GetComponentInParent<Button>();

            UIFactory.CreatePanel(canvas.transform, "FooterChip",
                new Vector2(0.14f, 0.205f), new Vector2(0.86f, 0.25f), UIStyle.PanelSoft);
            menu.footerText = UIFactory.CreateText(canvas.transform, "Footer",
                new Vector2(0.14f, 0.205f), new Vector2(0.86f, 0.25f), 30, TextAnchor.MiddleCenter);
            menu.footerText.color = UIStyle.CreamDim;
        }

        /// <summary>Builds the in-game HUD under the UI root and wires its references.</summary>
        public static void BuildGameHud(GameUI ui)
        {
            var canvas = UIFactory.CreateCanvas(ui.transform);

            UIFactory.CreatePanel(canvas.transform, "TopBar",
                new Vector2(0.02f, 0.925f), new Vector2(0.98f, 0.985f), UIStyle.PanelSoft);
            ui.counterText = UIFactory.CreateText(canvas.transform, "Counter",
                new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.985f), 40, TextAnchor.MiddleLeft);

            var hintChip = UIFactory.CreatePanel(canvas.transform, "HintChip",
                new Vector2(0.06f, 0.865f), new Vector2(0.94f, 0.915f), UIStyle.PanelSoft);
            ui.hintChip = hintChip.gameObject;
            ui.hintText = UIFactory.CreateText(ui.hintChip.transform, "Hint",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 30, TextAnchor.MiddleCenter);
            ui.hintText.color = UIStyle.Hint;

            ui.statusText = UIFactory.CreateText(canvas.transform, "Status",
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.72f), 76, TextAnchor.MiddleCenter, shadow: true);

            var toastChip = UIFactory.CreatePanel(canvas.transform, "ToastChip",
                new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.3f), UIStyle.PanelDark);
            ui.toastChip = toastChip.gameObject;
            ui.toastText = UIFactory.CreateText(ui.toastChip.transform, "Toast",
                new Vector2(0.02f, 0f), new Vector2(0.98f, 1f), 32, TextAnchor.MiddleCenter);
            ui.toastChip.SetActive(false);

            UIFactory.CreatePanel(canvas.transform, "BottomBar",
                new Vector2(0.01f, 0.008f), new Vector2(0.99f, 0.245f), UIStyle.PanelSoft);

            ui.menuButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Menu",
                new Vector2(0.03f, 0.168f), new Vector2(0.25f, 0.232f), NoOp));
            var next = UIFactory.CreateButton(canvas.transform, "Next lesson",
                new Vector2(0.27f, 0.168f), new Vector2(0.61f, 0.232f), NoOp, 34, primary: true);
            ui.nextLessonButton = ButtonOf(next);

            ui.importField = UIFactory.CreateInputField(canvas.transform,
                new Vector2(0.03f, 0.098f), new Vector2(0.71f, 0.158f), "paste PUTT- code…");
            ui.watchButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Watch",
                new Vector2(0.73f, 0.098f), new Vector2(0.97f, 0.158f), NoOp));

            ui.retryButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Retry",
                new Vector2(0.03f, 0.018f), new Vector2(0.25f, 0.088f), NoOp));
            ui.shareButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Share",
                new Vector2(0.27f, 0.018f), new Vector2(0.49f, 0.088f), NoOp));
            ui.ghostButton = ButtonOf(UIFactory.CreateButton(canvas.transform, "Ghost",
                new Vector2(0.51f, 0.018f), new Vector2(0.73f, 0.088f), NoOp));
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
            image.color = PaletteMaterials.Felt;
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
