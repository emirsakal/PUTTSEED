#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// A two-option segmented control for settings rows: the active segment
    /// fills amber with ink text, the inactive one sits as a faint chip. Both
    /// halves are real buttons (sound + press scale included) — tapping a
    /// side SELECTS it, no blind toggling.
    /// </summary>
    public sealed class SegmentedToggle : MonoBehaviour
    {
        public Button? optionAButton;
        public Image? optionABg;
        public Text? optionALabel;
        public Button? optionBButton;
        public Image? optionBBg;
        public Text? optionBLabel;

        /// <summary>Styles the segments for the current value (A = first).</summary>
        public void SetSelected(bool firstSelected)
        {
            Style(optionABg, optionALabel, firstSelected);
            Style(optionBBg, optionBLabel, !firstSelected);
        }

        private static void Style(Image? bg, Text? label, bool active)
        {
            if (bg != null)
            {
                bg.color = active ? UIStyle.Accent : new Color(1f, 1f, 1f, 0.07f);
            }

            if (label != null)
            {
                label.color = active ? UIStyle.AccentInk : UIStyle.CreamDim;
            }
        }
    }
}
