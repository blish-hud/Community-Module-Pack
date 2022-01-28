using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;

namespace Universal_Search_Module.Controls {
    public class TraitTooltip : Tooltip {
        private const int MAX_WIDTH = 400;

        private readonly Trait _trait;

        public TraitTooltip(Trait trait) {
            _trait = trait;

            var traitTitle = new Label() {
                Text = _trait.Name,
                Font = Content.DefaultFont18,
                TextColor = ContentService.Colors.Chardonnay,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Parent = this,
            };

            var traitDescription = new Label() {
                Text = StringUtil.SanitizeTraitDescription( _trait.Description),
                Font = Content.DefaultFont16,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Location = new Point(0, traitTitle.Bottom + 5),
                Parent = this,
            };

            // Poor mans max width implementation
            if (traitDescription.Width > MAX_WIDTH) {
                traitDescription.AutoSizeWidth = false;
                traitDescription.Width = MAX_WIDTH;
                traitDescription.WrapText = true;
                traitDescription.RecalculateLayout();
            }

            Control lastFact = traitDescription;
            if (_trait.Facts != null) {
                TraitFactRecharge rechargeFact = null;
                foreach (var fact in _trait.Facts) {
                    switch (fact) {
                        case TraitFactRecharge traitFactRecharge:
                            rechargeFact = traitFactRecharge;
                            break;
                        default:
                            lastFact = CreateFact(fact, lastFact);
                            break;
                    }
                }

                if (rechargeFact != null) {
                    CreateRechargeFact(rechargeFact);
                }
            }


            // TODO: Find a way to mimic the behaviour in gw2 with multiple tooltips
            //if (_trait.Skills != null) {
            //    Tooltip lastToolTip = this;

            //    foreach (var skill in _trait.Skills) {
                    
            //        var tooltip = new SkillTooltip(ConvertSkill(skill));
            //        lastToolTip.Tooltip = tooltip;
            //        tooltip.Show(AbsoluteBounds.Location + new Point(lastToolTip.Width + 10, 0));
            //        lastToolTip = tooltip;
            //    }
            //}
        }

        

        private Skill ConvertSkill(TraitSkill skill) {
            return new Skill() {
                Id = skill.Id,
                Name = skill.Name,
                Description = skill.Description,
                Icon = skill.Icon,
                ChatLink = skill.ChatLink,
                Flags = skill.Flags,
                Facts = skill.Facts,
                TraitedFacts = skill.TraitedFacts,
                Categories = skill.Categories,
            };
        }

        private Control CreateFact(TraitFact fact, Control lastFact) {
            // Skip Damage fact bc calculation of the actual damage value is rather complicated
            if (fact is TraitFactDamage) {
                return lastFact;
            }

            var icon = fact.Icon;

            if (fact is TraitFactPrefixedBuff prefixedBuff) {
                icon = prefixedBuff.Prefix.Icon;
            }

            var factImage = new Image() {
                Texture = icon != null ? Content.GetRenderServiceTexture(icon) : (AsyncTexture2D)ContentService.Textures.Error,
                Size = new Point(32, 32),
                Location = new Point(0, lastFact.Bottom + 5),
                Parent = this,
            };

            var factDescription = new Label() {
                Text = GetTextForFact(fact),
                Font = Content.DefaultFont16,
                TextColor = new Microsoft.Xna.Framework.Color(161, 161, 161),
                Height = factImage.Height,
                VerticalAlignment = VerticalAlignment.Middle,
                AutoSizeWidth = true,
                Location = new Point(factImage.Width + 5, lastFact.Bottom + 5),
                Parent = this,
            };

            // Poor mans max width solution and vertical alignment of an image
            if (factDescription.Width > MAX_WIDTH - factImage.Width) {
                factDescription.AutoSizeWidth = false;
                factDescription.Width = MAX_WIDTH - factImage.Width;
                factDescription.WrapText = true;
                factDescription.AutoSizeHeight = true;
                factDescription.RecalculateLayout();
                factImage.Location = new Point(0, factDescription.Location.Y + ((factDescription.Height / 2) - (factImage.Height / 2)));
            }

            return factDescription;
        }

        private string GetTextForFact(TraitFact fact) {
            switch (fact) {
                case TraitFactAttributeAdjust attributeAdjust:
                    return $"{attributeAdjust.Text}: {attributeAdjust.Value}";
                case TraitFactBuff buff:
                    var applyCountText = buff.ApplyCount != null && buff.ApplyCount != 1 ? buff.ApplyCount + "x " : string.Empty;
                    var durationText = buff.Duration != 0 ? $" ({buff.Duration}s) " : string.Empty;
                    return $"{applyCountText}{buff.Status}{durationText}: {buff.Description}";
                case TraitFactBuffConversion buffConversion: // TODO: Localization
                    return $"Gain {buffConversion.Target} Based on a Percentage of {buffConversion.Source}: {buffConversion.Percent}%";
                case TraitFactComboField comboField:
                    return $"{comboField.Text}: {comboField.FieldType.ToEnumString()}";
                case TraitFactComboFinisher comboFinisher:
                    return $"{comboFinisher.Text}: {comboFinisher.Type} ({comboFinisher.Percent} Chance)";
                case TraitFactDamage damage: // Skip
                    return $"{damage.Text}({damage.HitCount}x): {damage.Text}";
                case TraitFactDistance distance:
                    return $"{distance.Text}: {distance.Distance}";
                case TraitFactNoData noData: // TODO: Localization
                    return "Combat Only";
                case TraitFactNumber number:
                    return $"{number.Text}: {number.Value}";
                case TraitFactPercent percent:
                    return $"{percent.Text}: {percent.Percent}%";
                case TraitFactPrefixedBuff prefixedBuff:
                    return $"{prefixedBuff.ApplyCount}x {prefixedBuff.Status} ({prefixedBuff.Duration}s): {prefixedBuff.Description}";
                case TraitFactRadius radius:
                    return $"{radius.Text}: {radius.Distance}";
                case TraitFactRange range:
                    return $"{range.Text}: {range.Value}";
                case TraitFactTime time:
                    return $"{time.Text}: {time.Duration}s";
                case TraitFactUnblockable unblockable:
                default:
                    return fact.Text;
            }
        }

        private void CreateRechargeFact(TraitFactRecharge skillFactRecharge) {
            var cooldownImage = new Image() {
                Texture = skillFactRecharge.Icon != null ? Content.GetRenderServiceTexture(skillFactRecharge.Icon) : (AsyncTexture2D)ContentService.Textures.Error,
                Visible = true,
                Size = new Point(16, 16),
                Parent = this,
            };

            cooldownImage.Location = new Point(Width - cooldownImage.Width, 1);

            var cooldownText = new Label() {
                Text = skillFactRecharge.Value.ToString(),
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = this,
            };
            cooldownText.Location = new Point(cooldownImage.Left - cooldownText.Width - 2, 0);
        }
    }
}
