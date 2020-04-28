using System.Collections.Generic;
using Blish_HUD.Contexts;
using Gw2Sharp.Models;
using Gw2Sharp.WebApi.V2.Models;

namespace Markers_and_Paths_Module.PackFormat.TacO.Pathables {
    public interface ITacOPathable {

        string Type { get; set; }

        PathingCategory Category { get; set; }

        List<FestivalContext.Festival> Festivals { get; }

        int Profession { get; set; }

        int Specialization { get; set; }

        int? Race { get; set; }

    }

}
