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

            var packDocument = new XmlDocument();
            string packSrc = SanitizeXml(xmlPackContents);
            bool packLoaded = false;

            try {
                packDocument.LoadXml(packSrc);
                packLoaded = true;
            } catch (XmlException ex) {
                Logger.Error(ex, "Could not load tacO overlay file {pathableResourceManager} from {xmlPackContentsType} due to an XML error.", pathableResourceManager, xmlPackContents);
            } catch (Exception ex) {
                Logger.Error(ex, "Could not load tacO overlay file {pathableResourceManager} from {xmlPackContentsType} due to an unexpected exception.", pathableResourceManager, xmlPackContents);
            }

            if (packLoaded) {
                TryLoadCategories(packDocument);
                TryLoadPOIs(packDocument, pathableResourceManager, Categories);
            }
        }

        private void TryLoadCategories(XmlDocument packDocument) {
            var categoryNodes = packDocument.DocumentElement?.SelectNodes("/OverlayData/MarkerCategory");
            if (categoryNodes == null) return;

            foreach (XmlNode categoryNode in categoryNodes) {
                Builders.PathingCategoryBuilder.UnpackCategory(categoryNode, Categories);
            }
        }

        private void TryLoadPOIs(XmlDocument packDocument, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) {
            var poiNodes = packDocument.DocumentElement?.SelectSingleNode("/OverlayData/POIs");
            if (poiNodes == null) return;

            Logger.Info("Found {poiCount} markers to load.", poiNodes.ChildNodes.Count);

            foreach (XmlNode poiNode in poiNodes) {
                Builders.PoiBuilder.UnpackPathable(poiNode, pathableResourceManager, rootCategory);
            }
        }

        private string SanitizeXml(string xmlDoc) {
            // TODO: Ask Tekkit (and others) to fix syntax
            // FYI, '>' does not need to be encoded in attribute values
            return xmlDoc
                  .Replace("& ", "&amp; ") // Space added to avoid replacing correctly encoded attribute values
                  .Replace("=\"<", "=\"&lt;")
                  .Replace("*", "")
                  .Replace("0behavior", "behavior");
        }

        /// <inheritdoc />
        public void Dispose() {
            this.Pathables.Clear();
            this.Categories.Clear();
        }

    }
}
