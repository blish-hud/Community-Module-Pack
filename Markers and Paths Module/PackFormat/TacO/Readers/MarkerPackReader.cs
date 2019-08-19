using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Pathing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Blish_HUD.Pathing.Content;
using Markers_and_Paths_Module.PackFormat.TacO.Prototypes;
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Readers {

    public sealed class MarkerPackReader {

        private static readonly Logger Logger = Logger.GetLogger(typeof(MarkerPackReader));

        public PathingCategory Categories { get; } = new PathingCategory("root") { Visible = true };

        private readonly SynchronizedCollection<IPathable<Entity>> _pathables = new SynchronizedCollection<IPathable<Entity>>();

        private readonly List<PrototypePathable> _prototypePathables = new List<PrototypePathable>();

        private void RegisterPathable(IPathable<Entity> pathable) {
            if (pathable == null) return;

            _pathables.Add(pathable);
        }

        private void RegisterPrototypePathable(PrototypePathable prototypePathable) {
            if (prototypePathable == null) return;

            _prototypePathables.Add(prototypePathable);
        }

        public void UpdatePathableStates() {
            foreach (var pathable in _pathables) {
                this.ProcessPathableState(pathable);
            }
        }

        public void UpdatePrototypePathableStates() {
            UnloadAndUnregisterAllPathables();

            for (int i = 0; i < _prototypePathables.Count; i++) {
                if (_prototypePathables[i].MapId == GameService.Player.MapId || _prototypePathables[i].MapId == -1) {
                    LoadAndRegisterPathable(_prototypePathables[i]);
                }
            }
        }

        private void LoadAndRegisterPathable(PrototypePathable prototypePathable) {
            var loadedPathable = prototypePathable.LoadPathable(this.Categories);

            if (loadedPathable != null) {
                loadedPathable.Active = true;
                GameService.Pathing.RegisterPathable(loadedPathable);
            }
        }

        private void UnloadAndUnregisterAllPathables() {
            for (int i = 0; i < GameService.Pathing.Pathables.Count; i++) {
                GameService.Pathing.UnregisterPathable(GameService.Pathing.Pathables[i]);
            }
        }

        private void ProcessPathableState(IPathable<Entity> pathable) {
            if (pathable.MapId == GameService.Player.MapId || pathable.MapId == -1) {
                pathable.Active = true;
                GameService.Pathing.RegisterPathable(pathable);
            } else if (GameService.Graphics.World.Entities.Contains(pathable.ManagedEntity)) {
                pathable.Active = false;
                GameService.Pathing.UnregisterPathable(pathable);
            }
        }

        public void ReadFromXmlPack(Stream xmlPackStream, PathableResourceManager pathableResourceManager) {
            string xmlPackContents;

            using (var xmlReader = new StreamReader(xmlPackStream)) {
                xmlPackContents = xmlReader.ReadToEnd();
            }

            NanoXmlDocument packDocument = null;

            bool packLoaded = false;

            try {
                packDocument = NanoXmlDocument.LoadFromXml(xmlPackContents);
                packLoaded = true;
            } catch (XmlException ex) {
                Logger.Warn(ex, "Could not load tacO overlay file {pathableResourceManager} at line: {xmlExceptionLine} position: {xmlExceptionPosition} due to an XML error.", pathableResourceManager.DataReader.GetPathRepresentation(), ex.LineNumber, ex.LinePosition);
            } catch (Exception ex) {
                Logger.Warn(ex, "Could not load tacO overlay file {pathableResourceManager} due to an unexpected exception.", pathableResourceManager.DataReader.GetPathRepresentation());
            }

            if (packLoaded) {
                int currentPathablesCount = _prototypePathables.Count;

                TryLoadCategories(packDocument);
                TryLoadPOIs(packDocument, pathableResourceManager, Categories);

                Logger.Info("{pathableDelta} pathables were loaded from {pathableResourceManager}.", _prototypePathables.Count - currentPathablesCount, pathableResourceManager.DataReader.GetPathRepresentation());
            }
        }

        private void TryLoadCategories(NanoXmlDocument packDocument) {
            var categoryNodes = packDocument.RootNode.SelectNodes("markercategory");

            for (int i = 0; i < categoryNodes.Length; i++) {
                Builders.PathingCategoryBuilder.UnpackCategory(categoryNodes[i], Categories);
            }
        }

        private void TryLoadPOIs(NanoXmlDocument packDocument, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            var poisNodes = packDocument.RootNode.SelectNodes("pois");

            for (int pSet = 0; pSet < poisNodes.Length; pSet++) {
                ref var poisNode = ref poisNodes[pSet];

                Logger.Info("Found {poiCount} POIs to load.", poisNode.SubNodes.Count());

                for (int i = 0; i < poisNode.SubNodes.Count; i++) {
                    this.RegisterPrototypePathable(Builders.PoiBuilder.UnpackPathable(poisNode.SubNodes[i], pathableResourceManager, rootCategory));
                }
            }
        }

        public void UnloadPack() {
            this.UnloadAndUnregisterAllPathables();
        }

    }
}
