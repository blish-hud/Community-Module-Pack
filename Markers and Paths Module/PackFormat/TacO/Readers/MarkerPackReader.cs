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
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Readers {

    // TODO: Use XmlReader to speed things up https://stackoverflow.com/a/676280/595437

    public sealed class MarkerPackReader : IDisposable {

        private static readonly Logger Logger = Logger.GetLogger(typeof(MarkerPackReader));

        internal readonly PathingCategory Categories = new PathingCategory("root") { Visible = true };

        internal readonly SynchronizedCollection<IPathable<Entity>> Pathables = new SynchronizedCollection<IPathable<Entity>>();

        public void RegisterPathable(IPathable<Entity> pathable) {
            if (pathable == null) return;

            this.Pathables.Add(pathable);
        }

        public void UpdatePathableStates() {
            foreach (var pathable in Pathables.ToArray()) {
                this.ProcessPathableState(pathable);
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

            NanoXml.NanoXmlDocument packDocument = null;

            bool packLoaded = false;

            try {
                packDocument = new NanoXml.NanoXmlDocument(xmlPackContents);
                packLoaded = true;
            } catch (XmlException ex) {
                Logger.Error(ex, "Could not load tacO overlay file {pathableResourceManager} at line: {xmlExceptionLine} position: {xmlExceptionPosition} due to an XML error.", pathableResourceManager.DataReader.GetPathRepresentation(), ex.LineNumber, ex.LinePosition);
            } catch (Exception ex) {
                Logger.Error(ex, "Could not load tacO overlay file {pathableResourceManager} due to an unexpected exception.", pathableResourceManager.DataReader.GetPathRepresentation());
            }

            if (packLoaded) {
                int currentPathablesCount = this.Pathables.Count;

                TryLoadCategories(packDocument);
                TryLoadPOIs(packDocument, pathableResourceManager, Categories);

                Logger.Info("{pathableDelta} pathables were loaded from {pathableResourceManager}.", this.Pathables.Count - currentPathablesCount, pathableResourceManager.DataReader.GetPathRepresentation());
            }
        }

        private void TryLoadCategories(NanoXmlDocument packDocument) {
            var categoryNodes = packDocument.RootNode.SelectNodes("MarkerCategory");
            if (categoryNodes == null) return;

            foreach (var categoryNode in categoryNodes) {
                Builders.PathingCategoryBuilder.UnpackCategory(categoryNode, Categories);
            }
        }

        private void TryLoadPOIs(NanoXmlDocument packDocument, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            var poiNodes = packDocument.RootNode["POIs"];
            if (poiNodes == null) return;

            Logger.Info("Found {poiCount} POIs to load.", poiNodes.SubNodes.Count());

            foreach (var poiNode in poiNodes.SubNodes) {
                Builders.PoiBuilder.UnpackPathable(poiNode, pathableResourceManager, rootCategory);
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.Pathables.Clear();
            this.Categories.Clear();
        }

    }
}
