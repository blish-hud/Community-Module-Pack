using System.Text.RegularExpressions;
namespace Discord_Rich_Presence_Module.Utils {
    public static class DiscordUtil {

        public static string TruncateLength(string value, int maxLength) {
            if (string.IsNullOrEmpty(value)) return "";

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string GetDiscordSafeString(string text) {
            return Regex.Replace(text.Replace(":", "").Trim(), @"[^a-zA-Z]+", "_").ToLowerInvariant();
        }

    }
}
