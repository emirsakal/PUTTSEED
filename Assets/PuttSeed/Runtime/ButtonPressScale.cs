#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Scene-serializable press feedback: the button dips to 96% while held.
    /// Lives in its own file — scene-serialized MonoBehaviours must match
    /// their file name or Unity drops them as missing scripts on load.
    /// </summary>
    public sealed class ButtonPressScale : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
            => transform.localScale = Vector3.one * 0.96f;

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
            => transform.localScale = Vector3.one;
    }
}
