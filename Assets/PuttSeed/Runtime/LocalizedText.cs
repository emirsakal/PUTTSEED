#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Localizes a baked Text in place: whatever English the scene serialized
    /// becomes its own key at Awake. Attached to every generated Text, so all
    /// static labels translate without per-label bookkeeping; runtime-written
    /// labels are overwritten later by their controllers (which call Loc.Tr).
    /// Lives in its own file — scene-serialized MonoBehaviours must match
    /// their file name or Unity drops them as missing scripts on load.
    /// </summary>
    public sealed class LocalizedText : MonoBehaviour
    {
        private void Awake()
        {
            var text = GetComponent<Text>();
            if (text != null && text.text.Length > 0)
            {
                text.text = Loc.Tr(text.text);
            }
        }
    }
}
