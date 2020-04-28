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
using Markers_and_Paths_Module.PackFormat.TacO.Pathables;
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Readers {

    public sealed class MarkerPackReader : IDisposable {

        private static readonly Logger Logger = Logger.GetLogger(typeof(MarkerPackReader));

        public readonly PathingCategory Categories = new PathingCategory("root") { Visible = true };

        public readonly SynchronizedCollection<IPathable<Entity>> Pathables = new SynchronizedCollection<IPathable<Entity>>();

        public void RegisterPathable(IPathable<Entity> pathable) {
            if (pathable == null) return;

            this.Pathables.Add(pathable);
        }

        public void UpdatePathableStates() {
            foreach (var pathable in Pathables.ToArray()) {
                ProcessPathableState(pathable);
            }
        }

        private void ProcessPathableState(IPathable<Entity> pathable) {
            if (PathableIsValid(pathable)) {
                pathable.Active = true;
                GameService.Pathing.RegisterPathable(pathable);
            } else if (GameService.Graphics.World.Entities.Contains(pathable.ManagedEntity)) {
                pathable.Active = false;
                GameService.Pathing.UnregisterPathable(pathable);
            }
        }

        private bool PathableIsValid(IPathable<Entity> pathable) {
            // Map check
            if (pathable.MapId != GameService.Gw2Mumble.CurrentMap.Id
             && pathable.MapId != -1)
                return false;

            var tacoPathable = (ITacOPathable)pathable;

            // Festival check
            if (tacoPathable.Festivals.Count > 0
             && !tacoPathable.Festivals.Any(festival => festival.IsActive()))
                return false;

            // Profession check
            if (tacoPathable.Profession > 0
             && tacoPathable.Profession != (int)GameService.Gw2Mumble.PlayerCharacter.Profession)
                return false;

            // Specialization check
            if (tacoPathable.Specialization > 0
             && tacoPathable.Specialization != GameService.Gw2Mumble.PlayerCharacter.Specialization)
                return false;

            // Race check
            if (tacoPathable.Race.HasValue
             && tacoPathable.Race != (int) GameService.Gw2Mumble.PlayerCharacter.Race)
                return false;

            return true;
        }

        public void ReadFromXmlPack(Stream xmlPackStream, PathableResourceManager pathableResourceManager) {
            string xmlPackContents;

            using (var xmlReader = new StreamReader(xmlPackStream)) {
                xmlPackContents = xmlReader.ReadToEnd();
            }

            NanoXml.NanoXmlDocument packDocument = null;

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
                int currentPathablesCount = this.Pathables.Count;

                TryLoadCategories(packDocument);
                TryLoadPOIs(packDocument, pathableResourceManager, Categories);

                Logger.Info("{pathableDelta} pathables were loaded from {pathableResourceManager}.", this.Pathables.Count - currentPathablesCount, pathableResourceManager.DataReader.GetPathRepresentation());
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
                //ref var poisNode = ref poisNodes[pSet];
                var poisNode = poisNodes[pSet];

                Logger.Info("Found {poiCount} POIs to load.", poisNode.SubNodes.Count());

                for (int i = 0; i < poisNode.SubNodes.Count; i++) {
                    Builders.PoiBuilder.UnpackPathable(poisNode.SubNodes[i], pathableResourceManager, rootCategory);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            this.Pathables.Clear();
            this.Categories.Clear();
        }

    }
}
