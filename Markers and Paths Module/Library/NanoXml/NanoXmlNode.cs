using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoXml {
    /// <summary>
    /// Element node of document
    /// </summary>
    public class NanoXmlNode : NanoXmlBase {
        private string value;
        private string name;

        private List<NanoXmlNode> subNodes = new List<NanoXmlNode>();
        private List<NanoXmlAttribute> attributes = new List<NanoXmlAttribute>();

        internal NanoXmlNode(string str, ref int i) {
            name = ParseAttributes(str, ref i, attributes, '>', '/');

            if (str[i] == '/') // if this node has nothing inside
            {
                i++; // skip /
                i++; // skip >
                return;
            }

            i++; // skip >

            // temporary. to include all whitespaces into value, if any
            int tempI = i;

            SkipSpaces(str, ref tempI);

            if (str[tempI] == '<') {
                i = tempI;

                while (str[i + 1] != '/') // parse subnodes
                {
                    i++; // skip <
                    subNodes.Add(new NanoXmlNode(str, ref i));

                    SkipSpaces(str, ref i);

                    if (i >= str.Length)
                        return; // EOF

                    if (str[i] != '<') {
                        i++;

                        // COMPAT: 'Deroirs Tryhard Marker Pack' has a random character outside of any node (line 297)
                        // throw new NanoXmlParsingException("Unexpected token");
                    }
                }

                i++; // skip <
            } else // parse value
              {
                value = GetValue(str, ref i, '<', '\0', false);
                i++; // skip <

                if (str[i] != '/')
                    throw new NanoXmlParsingException("Invalid ending on tag " + name);
            }

            i++; // skip /
            SkipSpaces(str, ref i);

            string endName = GetValue(str, ref i, '>', '\0', true);
            if (endName != name)
                throw new NanoXmlParsingException("Start/end tag name mismatch: " + name + " and " + endName);
            SkipSpaces(str, ref i);

            if (str[i] != '>')
                throw new NanoXmlParsingException("Invalid ending on tag " + name);

            i++; // skip >
        }
        /// <summary>
        /// Element value
        /// </summary>
        public string Value {
            get { return value; }
        }
        /// <summary>
        /// Element name
        /// </summary>
        public string Name {
            get { return name; }
        }
        /// <summary>
        /// List of subelements
        /// </summary>
        public IEnumerable<NanoXmlNode> SubNodes {
            get { return subNodes; }
        }

        public IEnumerable<NanoXmlNode> SelectNodes(string nodeName) {
            return subNodes.Where(n => string.Equals(n.Name, nodeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// List of attributes
        /// </summary>
        public IEnumerable<NanoXmlAttribute> Attributes {
            get { return attributes; }
        }
        /// <summary>
        /// Returns subelement by given name
        /// </summary>
        /// <param name="nodeName">Name of subelement to get</param>
        /// <returns>First subelement with given name or NULL if no such element</returns>
        public NanoXmlNode this[string nodeName] {
            get {
                foreach (NanoXmlNode nanoXmlNode in subNodes)
                    if (nanoXmlNode.name == nodeName)
                        return nanoXmlNode;

                return null;
            }
        }
        /// <summary>
        /// Returns attribute by given name
        /// </summary>
        /// <param name="attributeName">Attribute name to get</param>
        /// <returns><see cref="NanoXmlAttribute"/> with given name or null if no such attribute</returns>
        public NanoXmlAttribute GetAttribute(string attributeName) {
            foreach (NanoXmlAttribute nanoXmlAttribute in attributes)
                if (nanoXmlAttribute.Name == attributeName)
                    return nanoXmlAttribute;

            return null;
        }
    }
}
