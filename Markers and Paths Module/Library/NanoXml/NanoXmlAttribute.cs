using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoXml {
    /// <summary>
    /// XML element attribute
    /// </summary>
    public class NanoXmlAttribute {
        private string name;
        private string value;
        /// <summary>
        /// Attribute name
        /// </summary>
        public string Name {
            get { return name; }
        }
        /// <summary>
        /// Attribtue value
        /// </summary>
        public string Value {
            get { return value; }
        }

        internal NanoXmlAttribute(string name, string value) {
            this.name = name;
            this.value = value;
        }
    }
}
