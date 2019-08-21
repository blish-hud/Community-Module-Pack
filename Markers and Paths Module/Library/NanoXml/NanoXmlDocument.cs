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

        private          NanoXmlNode            _rootNode;
        private readonly List<NanoXmlAttribute> _declarations = new List<NanoXmlAttribute>();

        protected NanoXmlDocument(string rawXml) {
            int i = 0;

            while (true) {
                SkipSpaces(rawXml, ref i);

                if (rawXml[i] != '<')
                    throw new NanoXmlParsingException("Unexpected token");

                i++; // skip <

                if (rawXml[i] == '?') // declaration
                {
                    i++; // skip ?
                    ParseAttributes(rawXml, ref i, _declarations, '?', '>');
                    i++; // skip ending ?
                    i++; // skip ending >

                    continue;
                }

                if (rawXml[i] == '!') { // doctype
                    while (rawXml[i] != '>') { // skip doctype
                        i++;
                    }

                    i++; // skip >

                    continue;
                }

                _rootNode = new NanoXmlNode(rawXml, ref i);
                break;
            }
        }


        /// <summary>
        /// Creates a new <see cref="NanoXmlDocument"/> and populates it with the provided <param name="rawXml">raw XML</param>.
        /// </summary>
        /// <param name="rawXml">The XML string to load the document from.</param>
        public static NanoXmlDocument LoadFromXml(string rawXml) {
            return new NanoXmlDocument(rawXml);
        }

        /// <summary>
        /// Root node of the document.
        /// </summary>
        public NanoXmlNode RootNode {
            get => _rootNode;
            set => _rootNode = value;
        }

        /// <summary>
        /// List of all XML Declarations as <see cref="NanoXmlAttribute"/>.
        /// </summary>
        public List<NanoXmlAttribute> Declarations => _declarations;

    }
}
