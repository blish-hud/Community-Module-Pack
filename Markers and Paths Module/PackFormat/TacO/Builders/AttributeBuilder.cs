using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Pathing;
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class AttributeBuilder {

        public static PathableAttributeCollection FromNanoXmlNode(NanoXmlNode node) {
            return new PathableAttributeCollection(node.Attributes.Select(a => new PathableAttribute(a.Name, a.Value)));
        }

    }
}
