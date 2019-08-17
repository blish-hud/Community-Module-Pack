using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NanoXml {

    /// <summary>
    /// Element node of document
    /// </summary>
    public class NanoXmlNode : NanoXmlBase {

        private readonly string _value;
        private readonly string _name;

        private readonly List<NanoXmlNode>      _subNodes   = new List<NanoXmlNode>();
        private readonly List<NanoXmlAttribute> _attributes = new List<NanoXmlAttribute>();

        internal NanoXmlNode(string str, ref int i) {
            _name = ParseAttributes(str, ref i, _attributes, '>', '/');

            if (str[i] == '/') { // if this node has nothing inside
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

                while (str[i + 1] != '/') { // parse subnodes
                    i++; // skip <
                    _subNodes.Add(new NanoXmlNode(str, ref i));

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
            } else {  // parse value
                _value = GetValue(str, ref i, '<', '\0', false);
                i++; // skip <

                if (str[i] != '/')
                    throw new NanoXmlParsingException("Invalid ending on tag " + _name);
            }

            i++; // skip /
            SkipSpaces(str, ref i);

            string endName = CleanName(GetValue(str, ref i, '>', '\0', true));
            if (endName != _name)
                throw new NanoXmlParsingException("Start/end tag name mismatch: " + _name + " and " + endName);

            SkipSpaces(str, ref i);

            if (str[i] != '>')
                throw new NanoXmlParsingException("Invalid ending on tag " + _name);

            i++; // skip >
        }

        /// <summary>
        /// Element value
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// Element name
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The subnodes of this node.
        /// </summary>
        public List<NanoXmlNode> SubNodes => _subNodes;

        /// <summary>
        /// The attributes for this node.
        /// </summary>
        public List<NanoXmlAttribute> Attributes => _attributes;

        public NanoXmlNode[] SelectNodes(string nodeName) {
            var matchedNodes    = new NanoXmlNode[_subNodes.Count];
            int numberOfMatches = 0;

            for (int i = 0; i < _subNodes.Count; i++) {
                if (_subNodes[i].Name == nodeName) {
                    matchedNodes[++numberOfMatches] = _subNodes[i];
                }
            }

            Array.Resize(ref matchedNodes, numberOfMatches);

            return matchedNodes;
        }

        /// <summary>
        /// Returns the first subnode with the given name that matches <param name="nodeName"/>.
        /// </summary>
        /// <param name="nodeName">Name of subnode to get.</param>
        /// <returns>First subnode with a name that matches <param name="nodeName"/> or <c>null</c> if there are no matches.</returns>
        public NanoXmlNode SelectNode(string nodeName) {
            for (int i = 0; i < _subNodes.Count; i++) {
                if (_subNodes[i].Name == nodeName) {
                    return _subNodes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first subnode with the given name that matches <param name="nodeName"/>.
        /// </summary>
        /// <param name="nodeName">Name of subnode to get.</param>
        /// <returns>First subnode with a name that matches <param name="nodeName"/>.</returns>
        /// <exception cref="KeyNotFoundException">If no subnode has a name that matches <param name="nodeName"/>, <exception cref="KeyNotFoundException"/> is thrown.</exception>
        public NanoXmlNode this[string nodeName] => SelectNode(nodeName) ?? throw new KeyNotFoundException($"No subnode with the name {nodeName} could be found!");
        
        /// <summary>
        /// Returns attribute by given name
        /// </summary>
        /// <param name="attributeName">Attribute name to get</param>
        /// <returns><see cref="NanoXmlAttribute"/> with given name or <c>null</c> if no such attribute exists.</returns>
        public NanoXmlAttribute GetAttribute(string attributeName) {
            for (int i = 0; i < _attributes.Count; i++) {
                if (_attributes[i].Name == attributeName) {
                    return _attributes[i];
                }
            }

            return null;
        }
    }
}
