using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;
using Blish_HUD.Pathing.Entities;
using Blish_HUD.Pathing.Format;
using Markers_and_Paths_Module.PackFormat.TacO.Behavior;
using Microsoft.Xna.Framework;

namespace Markers_and_Paths_Module.PackFormat.TacO.Pathables
{
    public sealed class TacORoutePathable : LoadedMarkerPathable, ITacOPathable
    {

        private const float DEFAULT_HEIGHTOFFSET = 1.5f;
        private const float DEFAULT_ICONSIZE = 2f;

        private string _type;
        private PathingCategory _category;
        private string _name;
        private bool _backwardDirection;

        private TacOBehavior _tacOBehavior;

        public string Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    _category = _rootCategory.GetOrAddCategoryFromNamespace(_type);
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        public PathingCategory Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    _type = _category.Namespace;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public bool BackwardDirection
        {
            get => _backwardDirection;
            set => SetProperty(ref _backwardDirection, value);
        }

        private readonly PathableAttributeCollection _sourceAttributes;
        private readonly PathingCategory _rootCategory;

        public TacORoutePathable(PathableAttributeCollection sourceAttributes, PathableResourceManager packContext, PathingCategory rootCategory) : base(packContext)
        {
            _sourceAttributes = sourceAttributes;
            _rootCategory = rootCategory;

            this.PropertyChanged += TacOMarkerPathable_PropertyChanged;

            BeginLoad();
        }

        private void TacOMarkerPathable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _tacOBehavior?.PushPathableStateChange(e.PropertyName);
        }

        protected override void BeginLoad()
        {
            LoadAttributes(_sourceAttributes);
        }

        protected override void PrepareAttributes()
        {
            // Type
            RegisterAttribute("type", attribute => (!string.IsNullOrEmpty(this.Type = attribute.Value.Trim())));

            // Name
            RegisterAttribute("Name", attribute => (!string.IsNullOrEmpty(this.Name = attribute.Value.Trim())));

            // BackwardDiraction
            RegisterAttribute("BackwardDirection", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseInt(attribute.Value, out int fOut)) return false;

                this.BackwardDirection = fOut == 1;
                return true;
            });

            base.PrepareAttributes();
        }

        protected override bool FinalizeAttributes(Dictionary<string, LoadedPathableAttributeDescription> attributeLoaders)
        {
            // Process attributes from type category first
            if (_category != null)
            {
                ProcessAttributes(_category.Attributes);
            }

            _category?.AddPathable(this);

            // Let base finalize attributes
            return base.FinalizeAttributes(attributeLoaders);
        }

    }
}
