using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoXml {
    public class NanoXmlParsingException : Exception {

        public int Line { get; }
        public int Position { get; }

        public NanoXmlParsingException(string message) : base(message) {
        }

        public NanoXmlParsingException(string message, int line, int position) : base($"{message} on line: {line} position: {position}") {
            Line     = line;
            Position = position;
        }

    }
}
