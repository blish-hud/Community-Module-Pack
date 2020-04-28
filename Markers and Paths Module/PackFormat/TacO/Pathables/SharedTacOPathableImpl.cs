using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Contexts;
using Blish_HUD.Pathing;

namespace Markers_and_Paths_Module.PackFormat.TacO.Pathables {
    internal static class SharedTacOPathableImpl {

        public static bool LoadAttrType(this ITacOPathable pathable, PathableAttribute attribute) {
            return (!string.IsNullOrEmpty(pathable.Type = attribute.Value.Trim()));
        }

        public static bool LoadAttrFestival(this ITacOPathable pathable, PathableAttribute attribute) {
            foreach (string festivalName in attribute.Value.Split(',')) {
                pathable.Festivals.Add(FestivalContext.Festival.FromName(festivalName.Trim()));
            }

            pathable.Festivals.TrimExcess();

            return true;
        }

        public static bool LoadAttrProfession(this ITacOPathable pathable, PathableAttribute attribute) {
            if (!InvariantUtil.TryParseInt(attribute.Value, out int fOut)) return false;

            pathable.Profession = fOut;
            return true;
        }

        public static bool LoadAttrSpecialization(this ITacOPathable pathable, PathableAttribute attribute) {
            if (!InvariantUtil.TryParseInt(attribute.Value, out int fOut)) return false;

            pathable.Specialization = fOut;
            return true;
        }

        public static bool LoadAttrRace(this ITacOPathable pathable, PathableAttribute attribute) {
            if (!InvariantUtil.TryParseInt(attribute.Value, out int fOut)) return false;

            pathable.Race = fOut;
            return true;
        }

    }
}
