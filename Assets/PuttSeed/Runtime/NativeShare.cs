#nullable enable
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Opens the Android share sheet for a text payload. Editor and other
    /// platforms return false so callers fall back to the clipboard.
    /// </summary>
    public static class NativeShare
    {
        /// <summary>True when a native share sheet was opened.</summary>
        public static bool Share(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intent = new AndroidJavaObject("android.content.Intent"))
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intent.Call<AndroidJavaObject>("setType", "text/plain");
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share your run"))
                {
                    activity.Call("startActivity", chooser);
                }
            }

            return true;
#else
            _ = text;
            return false;
#endif
        }
    }
}
