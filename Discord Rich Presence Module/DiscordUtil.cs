using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Discord_Rich_Presence_Module {
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
