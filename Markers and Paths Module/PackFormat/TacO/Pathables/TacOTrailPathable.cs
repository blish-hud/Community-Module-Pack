using Blish_HUD.Pathing.Format;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Blish_HUD;
using Blish_HUD.Contexts;
using Blish_HUD.Pathing;
using Blish_HUD.Pathing.Content;

namespace Markers_and_Paths_Module.PackFormat.TacO.Pathables {
    public sealed class TacOTrailPathable : LoadedTrailPathable, ITacOPathable {

        private const float DEFAULT_TRAILSCALE = 1f;
        private const float DEFAULT_ANIMATIONSPEED = 0.5f;

        private string          _type;
        private PathingCategory _category;
        private string          _trlFilePath;

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

        public float FadeNear {
            get => this.ManagedEntity.FadeNear;
            set => this.ManagedEntity.FadeNear = value;
        }

        public float FadeFar {
            get => this.ManagedEntity.FadeFar;
            set => this.ManagedEntity.FadeFar = value;
        }

        public string TrlFilePath {
            get => _trlFilePath;
            set => SetProperty(ref _trlFilePath, value);
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

        private readonly PathableAttributeCollection _sourceAttributes;
        private readonly PathingCategory             _rootCategory;

        public TacOTrailPathable(PathableAttributeCollection sourceAttributes, PathableResourceManager pathableResourceManager, PathingCategory rootCategory) : base(pathableResourceManager) {
            _sourceAttributes = sourceAttributes;
            _rootCategory     = rootCategory;

            BeginLoad();
        }

        protected override void BeginLoad() {
            LoadAttributes(_sourceAttributes);
        }

        protected override void PrepareAttributes() {
            base.PrepareAttributes();

            // Type
            RegisterAttribute("type", this.LoadAttrType);

            // [Required] TrailData
            RegisterAttribute("traildata",
                              attribute => (!string.IsNullOrEmpty(this.TrlFilePath = attribute.Value.Trim())),
                              true);

            // Alpha (alias:Opacity)
            RegisterAttribute("alpha", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.Opacity = fOut;
                return true;
            });

            // FadeNear
            RegisterAttribute("fadenear", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.FadeNear = fOut;
                return true;
            });

            // FadeFar
            RegisterAttribute("fadefar", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.FadeFar = fOut;
                return true;
            });

            // AnimationSpeed
            RegisterAttribute("animspeed", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.ManagedEntity.AnimationSpeed = fOut;
                return true;
            });

            // TrailScale
            RegisterAttribute("trailscale", delegate (PathableAttribute attribute) {
                if (!InvariantUtil.TryParseFloat(attribute.Value, out float fOut)) return false;

                this.Scale = fOut;
                return true;
            });

            // Festivals
            RegisterAttribute("festival", this.LoadAttrFestival);

            // Profession + Specialization + Race
            RegisterAttribute("profession",     this.LoadAttrProfession);
            RegisterAttribute("specialization", this.LoadAttrSpecialization);
            RegisterAttribute("race",           this.LoadAttrRace);
        }

        protected override bool FinalizeAttributes(Dictionary<string, LoadedPathableAttributeDescription> attributeLoaders) {
            // Process attributes from type category first
            if (_category != null) {
                ProcessAttributes(_category.Attributes);
            }

            _category?.AddPathable(this);

            // Load trl file
            using (var trlStream = this.PathableManager.DataReader.GetFileStream(this.TrlFilePath)) {
                if (trlStream == null) return false;

                var sectionData = Readers.TrlReader.ReadStream(trlStream);

                if (!sectionData.Any()) return false;

                sectionData.ForEach(t => {
                    this.MapId = t.MapId;
                    this.ManagedEntity.AddSection(t.TrailPoints);
                });
            }

            // Finalize attributes
            if (attributeLoaders.ContainsKey("trailscale")) {
                if (!attributeLoaders["trailscale"].Loaded) {
                    this.Scale = DEFAULT_TRAILSCALE;
                }
            }

            if (attributeLoaders.ContainsKey("animspeed")) {
                if (!attributeLoaders["animspeed"].Loaded) {
                    this.ManagedEntity.AnimationSpeed = DEFAULT_ANIMATIONSPEED;
                }
            }

            // Let base finalize attributes
            return base.FinalizeAttributes(attributeLoaders);
        }

    }
}
