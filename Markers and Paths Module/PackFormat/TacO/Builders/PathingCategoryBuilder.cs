using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Blish_HUD;
using NanoXml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class PathingCategoryBuilder {

        private static readonly Logger Logger = Logger.GetLogger(typeof(PathingCategoryBuilder));

        private const string ELEMENT_CATEGORY = "markercategory";

        private const string MARKERCATEGORY_NAME_ATTR        = "name";
        private const string MARKERCATEGORY_DISPLAYNAME_ATTR = "displayname";
        private const string MARKERCATEGORY_ISSEPARATOR_ATTR = "isseparator";

        public static void UnpackCategory(NanoXmlNode categoryNode, PathingCategory categoryParent) {
            if (!string.Equals(categoryNode.Name, ELEMENT_CATEGORY, StringComparison.OrdinalIgnoreCase)) {
                Logger.Warn("Tried to unpack {categoryNodeName} as a MarkerCategory!", categoryNode.Name);
                return;
            }

            var loadedCategory = FromNanoXmlNode(categoryNode, categoryParent);

            if (loadedCategory == null) return;

            foreach (NanoXmlNode childCategoryNode in categoryNode.SubNodes) {
                UnpackCategory(childCategoryNode, loadedCategory);
            }
        }

        public static PathingCategory FromNanoXmlNode(NanoXmlNode categoryNode, PathingCategory parent) {
            string categoryName = categoryNode.GetAttribute(MARKERCATEGORY_NAME_ATTR)?.Value;

            // Can't define a marker category without a name
            if (string.IsNullOrEmpty(categoryName)) return null;

            var subjCategory = parent.Contains(categoryName)
                                   // We're extending an existing category
                                   ? parent[categoryName]
                                   // We're adding a new category
                                   : parent.GetOrAddCategoryFromNamespace(categoryName);

            subjCategory.DisplayName = categoryNode.GetAttribute(MARKERCATEGORY_DISPLAYNAME_ATTR)?.Value;
            subjCategory.IsSeparator = categoryNode.GetAttribute(MARKERCATEGORY_ISSEPARATOR_ATTR)?.Value == "1";

            subjCategory.SetAttributes(AttributeBuilder.FromNanoXmlNode(categoryNode));

            return subjCategory;
        }

    }

}
