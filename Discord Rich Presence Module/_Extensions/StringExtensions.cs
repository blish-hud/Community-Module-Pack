using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Discord_Rich_Presence_Module
{
    internal static class StringExtensions
    {
        public static string ToSHA1Hash(this string input, bool lowerCase = true)
        {
            using var sha1 = new SHA1Managed();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(hash.Select(b => b.ToString(lowerCase ? "x2" : "X2")));
        }
    }
}
