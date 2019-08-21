namespace NanoXml {
    /// <summary>
    /// An XML element attribute.
    /// </summary>
    public class NanoXmlAttribute {

        private readonly string _name;
        private readonly string _value;

        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// The value of the attribute.
        /// </summary>
        public string Value => _value;

        internal NanoXmlAttribute(string name, string value) {
            _name  = name;
            _value = value;
        }

    }
}
