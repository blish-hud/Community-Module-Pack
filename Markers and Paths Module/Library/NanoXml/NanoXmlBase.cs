using System.Collections.Generic;

namespace NanoXml {

    /// <summary>
    /// Base class containing calls for all XML class types.
    /// </summary>
    public abstract class NanoXmlBase {

        protected static bool IsSpace(char c) {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }

        protected static void SkipSpaces(string str, ref int i) {
            while (i < str.Length) {
                if (!IsSpace(str[i])) {
                    if (str[i] == '<' && i + 4 < str.Length && str[i + 1] == '!' && str[i + 2] == '-' && str[i + 3] == '-') {
                        i += 4; // skip <!--

                        while (i + 2 < str.Length && !(str[i] == '-' && str[i + 1] == '-'))
                            i++;

                        i += 2; // skip --
                    } else
                        break;
                }

                i++;
            }
        }

        protected static string GetValue(string str, ref int i, char endChar, char endChar2, bool stopOnSpace) {
            int start = i;
            while ((!stopOnSpace || !IsSpace(str[i])) && str[i] != endChar && str[i] != endChar2) i++;

            return str.Substring(start, i - start);
        }

        protected static bool IsQuote(char c) {
            return c == '"' || c == '\'';
        }

        // returns name
        protected static string ParseAttributes(string str, ref int i, List<NanoXmlAttribute> attributes, char endChar, char endChar2) {
            SkipSpaces(str, ref i);
            // name of the element
            string name = CleanName(GetValue(str, ref i, endChar, endChar2, true));

            SkipSpaces(str, ref i);

            while (str[i] != endChar && str[i] != endChar2) {
                string attrName = CleanName(GetValue(str, ref i, '=', '\0', true));

                SkipSpaces(str, ref i);
                i++; // skip '='
                SkipSpaces(str, ref i);

                char quote = str[i];
                if (!IsQuote(quote))
                    throw new NanoXmlParsingException("Unexpected token after " + attrName);

                i++; // skip quote
                string attrValue = CleanValue(GetValue(str, ref i, quote, '\0', false));
                i++; // skip quote

                attributes.Add(new NanoXmlAttribute(attrName, attrValue));

                SkipSpaces(str, ref i);
            }

            return name;
        }

        protected static string CleanName(string str) {
            // COMPAT: Some packs have random invalid characters
            // COMPAT: Tekkit packs have 0 before behavior in some places
            return str.Replace("*", "")
                      .TrimStart('0')
                      .ToLower();
        }

        protected static string CleanValue(string str) {
            return str.Replace("*", "")
                      .Replace("&lt;",   "<")
                      .Replace("&gt;",   ">")
                      .Replace("&amp;",  "&")
                      .Replace("&quot;", "\"")
                      .Replace("&apos;", "'")
                      .Replace("&#xA;", "\n");
        }
    }
}
