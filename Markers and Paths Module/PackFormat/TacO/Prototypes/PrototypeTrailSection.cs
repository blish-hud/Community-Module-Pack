using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Markers_and_Paths_Module.PackFormat.TacO.Prototypes {
    public struct PrototypeTrailSection {

        private readonly int       _mapId;
        private readonly Vector3[] _trailPoints;

        public int MapId => _mapId;

        public Vector3[] TrailPoints => _trailPoints;

        public PrototypeTrailSection(int mapId, Vector3[] trailPoints) {
            _mapId       = mapId;
            _trailPoints = trailPoints;
        }

    }
}
