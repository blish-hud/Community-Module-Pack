using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Pathing;

namespace Markers_and_Paths_Module.PackFormat.TacO {
    public static class AttributeReaderUtil {

        public static bool TryReadInt(PathableAttribute attribute, out int valueOut) {
            return InvariantUtil.TryParseInt(attribute.Value, out valueOut);
        }

        public static bool TryReadFloat(PathableAttribute attribute, out float valueOut) {
            return InvariantUtil.TryParseFloat(attribute.Value, out valueOut);
        }

        public static bool TryReadBool(PathableAttribute attribute, out bool valueOut) {
            valueOut = (attribute.Value == "1");

            return true;
        }

    }
}
