using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Contexts;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;
using Blish_HUD.Pathing.Entities;
using Blish_HUD.Pathing.Format;
using Markers_and_Paths_Module.PackFormat.TacO.Behavior;
using Microsoft.Xna.Framework;

namespace Markers_and_Paths_Module.PackFormat.TacO.Pathables {
    public sealed class TacOMarkerPathable : LoadedMarkerPathable, ITacOPathable {

        private const float DEFAULT_HEIGHTOFFSET = 1.5f;
        private const float DEFAULT_ICONSIZE = 2f;

        private string          _type;
        private PathingCategory _category;
        private int             _resetLength;
        private bool            _autoTrigger  = false;
        private bool            _hasCountdown = false;
        private float           _triggerRange = 2.0f;
        private int             _tacOBehaviorId;

        private TacOBehavior _tacOBehavior;

        private int  _profession;
        private int  _specialization;
        private int? _race;

        public string Type {
            get => _type;
            set {
                if (SetProperty(ref _type, value)) {
                    _category = _rootCategory.GetOrAddCategoryFromNamespace(_type);
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        public PathingCategory Category {
            get => _category;
            set {
                if (SetProperty(ref _category, value)) {
                    _type = _category.Namespace;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        public int ResetLength {
            get => _resetLength;
            set => SetProperty(ref _resetLength, value);
        }

        public bool AutoTrigger {
            get => _autoTrigger;
            set => SetProperty(ref _autoTrigger, value);
        }

        public bool HasCountdown {
            get => _hasCountdown;
            set => SetProperty(ref _hasCountdown, value);
        }
        public float HeightOffset {
            get => this.ManagedEntity.VerticalOffset;
            set { this.ManagedEntity.VerticalOffset = value; OnPropertyChanged(); }
        }

        public float TriggerRange {
            get => _triggerRange;
            set => SetProperty(ref _triggerRange, value);
        }

        public List<FestivalContext.Festival> Festivals { get; } = new List<FestivalContext.Festival>();

        public int Profession {
            get => _profession;
            set => SetProperty(ref _profession, value);
        }

        public int Specialization {
            get => _specialization;
            set => SetProperty(ref _specialization, value);
        }

        public int? Race {
            get => _race;
            set => SetProperty(ref _race, value);
        }

        public int TacOBehaviorId {
            get => _tacOBehaviorId;
            set {
                if (SetProperty(ref _tacOBehaviorId, value)) {
                    this.Behavior.Remove(_tacOBehavior);

                    _tacOBehavior = TacOBehavior.FromBehaviorId(this, (TacOBehaviorId)_tacOBehaviorId);

                    if (_tacOBehavior != null) {
                        this.Behavior.Add(_tacOBehavior);
                    }
                }
            }
        }

        private readonly PathableAttributeCollection _sourceAttributes;
        private readonly PathingCategory             _rootCategory;

        public TacOMarkerPathable(PathableAttributeCollection sourceAttributes, PathableResourceManager packContext, PathingCategory rootCategory) : base(packContext) {
            _sourceAttributes = sourceAttributes;
            _rootCategory     = rootCategory;

            this.PropertyChanged += TacOMarkerPathable_PropertyChanged;

            BeginLoad();
        }

        private void TacOMarkerPathable_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            _tacOBehavior?.PushPathableStateChange(e.PropertyName);
        }

        protected override void BeginLoad() {
            LoadAttributes(_sourceAttributes);
        }

        protected override void PrepareAttributes() {
            // Type
            RegisterAttribute("type", this.LoadAttrType);

            // Alpha (alias:Opacity)
            RegisterAttribute("alpha", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.Opacity = fOut;
                return true;
            });

            // FadeNear
            RegisterAttribute("fadeNear", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.ManagedEntity.FadeNear = fOut;
                return true;
            });

            // FadeFar
            RegisterAttribute("fadeFar", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.ManagedEntity.FadeFar = fOut;
                return true;
            });

            // IconSize
            RegisterAttribute("iconSize", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.ManagedEntity.AutoResize = false;
                this.ManagedEntity.Size = new Vector2(fOut * 2f);
                return true;
            });

            // HeightOffset
            RegisterAttribute("heightOffset", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.HeightOffset = fOut;
                return true;
            });

            // ResetLength
            RegisterAttribute("resetLength", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseInt(attribute.Value, out int iOut)) return false;

                this.ResetLength = iOut;
                return true;
            });

            // AutoTrigger
            RegisterAttribute("autoTrigger", delegate (PathableAttribute attribute) {
                this.AutoTrigger = (attribute.Value == "1");
                return true;
            });

            // HasCountdown
            RegisterAttribute("hasCountdown", delegate (PathableAttribute attribute) {
                this.HasCountdown = (attribute.Value == "1");
                return true;
            });

            // TriggerRange
            RegisterAttribute("triggerRange", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.TriggerRange = fOut;
                return true;
            });

            // TacO Behavior
            RegisterAttribute("behavior", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseInt(attribute.Value, out int iOut)) return false;

                this.TacOBehaviorId = iOut;
                return true;
            });

            // Festivals
            RegisterAttribute("festival", this.LoadAttrFestival);

            // Profession + Specialization
            RegisterAttribute("profession",     this.LoadAttrProfession);
            RegisterAttribute("specialization", this.LoadAttrSpecialization);
            RegisterAttribute("race",           this.LoadAttrRace);

            base.PrepareAttributes();
        }

        protected override bool FinalizeAttributes(Dictionary<string, LoadedPathableAttributeDescription> attributeLoaders) {
            // Process attributes from type category first
            if (_category != null) {
                ProcessAttributes(_category.Attributes);
            }

            _category?.AddPathable(this);

            // Finalize attributes
            if (attributeLoaders.ContainsKey("heightoffset")) {
                if (!attributeLoaders["heightoffset"].Loaded) {
                    this.HeightOffset = DEFAULT_HEIGHTOFFSET;
                    this.ManagedEntity.VerticalConstraint = BillboardVerticalConstraint.CameraPosition;
                }
            }

            if (attributeLoaders.ContainsKey("iconsize")) {
                if (!attributeLoaders["iconsize"].Loaded) {
                    this.ManagedEntity.Size = new Vector2(DEFAULT_ICONSIZE);
                }
            }

            // Let base finalize attributes
            return base.FinalizeAttributes(attributeLoaders);
        }

    }
}
