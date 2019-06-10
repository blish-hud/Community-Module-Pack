using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markers_and_Paths_Module.PackFormat.TacO.Pathables {
    public interface ITacOPathable {

        string Type { get; set; }

        PathingCategory Category { get; set; }

    }

}
