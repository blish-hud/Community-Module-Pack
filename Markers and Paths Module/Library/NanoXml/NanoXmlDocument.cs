using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoXml {
    /// <summary>
    /// Class representing whole DOM XML document
    /// </summary>
    public class NanoXmlDocument : NanoXmlBase {
        private NanoXmlNode rootNode;
        private List<NanoXmlAttribute> declarations = new List<NanoXmlAttribute>();
        /// <summary>
        /// Public constructor. Loads xml document from raw string
        /// </summary>
        /// <param name="xmlString">String with xml</param>
        public NanoXmlDocument(string xmlString) {
            int i = 0;

            while (true) {
                SkipSpaces(xmlString, ref i);

                if (xmlString[i] != '<')
                    throw new NanoXmlParsingException("Unexpected token");

                i++; // skip <

                if (xmlString[i] == '?') // declaration
                {
                    i++; // skip ?
                    ParseAttributes(xmlString, ref i, declarations, '?', '>');
                    i++; // skip ending ?
                    i++; // skip ending >

                    continue;
                }

                if (xmlString[i] == '!') // doctype
                {
                    while (xmlString[i] != '>') // skip doctype
                        i++;

                    i++; // skip >

                    continue;
                }

                rootNode = new NanoXmlNode(xmlString, ref i);
                break;
            }
        }
        /// <summary>
        /// Root document element
        /// </summary>
        public NanoXmlNode RootNode {
            get { return rootNode; }
        }
        /// <summary>
        /// List of XML Declarations as <see cref="NanoXmlAttribute"/>
        /// </summary>
        public IEnumerable<NanoXmlAttribute> Declarations {
            get { return declarations; }
        }
    }
}
