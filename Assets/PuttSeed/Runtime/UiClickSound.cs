#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Scene-serializable click hookup: UnityEvent listeners added while
    /// BAKING a scene do not survive serialization, so every generated button
    /// carries this component and wires the click sound at runtime instead.
    /// Close/back buttons mark <see cref="downTone"/> for the descending
    /// tick. Lives in its own file — scene-serialized MonoBehaviours must
    /// match their file name or Unity drops them as missing scripts on load.
    /// </summary>
    public sealed class UiClickSound : MonoBehaviour
    {
        /// <summary>True on closing controls — plays the pitched-down tick.</summary>
        public bool downTone;

        private void Awake()
        {
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    if (downTone)
                    {
                        UiSounds.ClickDown();
                    }
                    else
                    {
                        UiSounds.Click();
                    }
                });
            }
        }
    }
}
