using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Markers_and_Paths_Module.PackFormat.TacO.Builders {
    public static class PathingCategoryBuilder {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const string ELEMENT_CATEGORY = "markercategory";

        public static void UnpackCategory(XmlNode categoryNode, PathingCategory categoryParent) {
            if (!string.Equals(categoryNode.Name, ELEMENT_CATEGORY, StringComparison.OrdinalIgnoreCase) && categoryNode.NodeType == XmlNodeType.Element) {
                Logger.Warn("Tried to unpack {categoryNodeName} (type: {nodeType}) as category!", categoryNode.Name, categoryNode.NodeType);
                return;
            }

            var loadedCategory = FromXmlNode(categoryNode, categoryParent);

            if (loadedCategory == null) return;

            foreach (XmlNode childCategoryNode in categoryNode) {
                UnpackCategory(childCategoryNode, loadedCategory);
            }
        }

        public static PathingCategory FromXmlNode(XmlNode categoryNode, PathingCategory parent) {
            string categoryName = categoryNode.Attributes["name"]?.Value;

            // Can't define a marker category without a name
            if (string.IsNullOrEmpty(categoryName)) return null;

            var subjCategory = parent.Contains(categoryName)
                                   // We're extending an existing category
                                   ? parent[categoryName]
                                   // We're adding a new category
                                   : parent.GetOrAddCategoryFromNamespace(categoryName);

            subjCategory.DisplayName = categoryNode.Attributes["DisplayName"]?.Value;

            subjCategory.SourceXmlNode = categoryNode;

            return subjCategory;
        }

    }

}
