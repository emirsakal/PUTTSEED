#nullable enable
using System;
using System.Text;
using UnityEngine;

namespace PuttSeed.Unity
{
    /// <summary>
    /// Device-to-device save transfer without a backend: the whole save JSON
    /// as a PUTTSAVE- base64url code. The prefix never collides with replay
    /// codes (their scanner matches the literal "PUTT-").
    /// </summary>
    public static class SaveCodec
    {
        /// <summary>Code prefix (distinct from PUTT- replay codes).</summary>
        public const string Prefix = "PUTTSAVE-";

        /// <summary>Encodes a save as a shareable code.</summary>
        public static string Export(SaveData data)
        {
            var json = JsonUtility.ToJson(data);
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return Prefix + b64;
        }

        /// <summary>
        /// Decodes a pasted code; false for anything that is not a valid
        /// PUTTSAVE- payload. Scans, so surrounding share text is fine.
        /// </summary>
        public static bool TryImport(string text, out SaveData data)
        {
            data = new SaveData();
            int at = text.IndexOf(Prefix, StringComparison.Ordinal);
            if (at < 0)
            {
                return false;
            }

            int start = at + Prefix.Length;
            int end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            try
            {
                string b64 = text.Substring(start, end - start)
                    .Replace('-', '+').Replace('_', '/');
                switch (b64.Length % 4)
                {
                    case 2: b64 += "=="; break;
                    case 3: b64 += "="; break;
                    case 1: return false;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                var parsed = JsonUtility.FromJson<SaveData>(json);
                if (parsed == null || parsed.days == null)
                {
                    return false;
                }

                data = parsed;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
