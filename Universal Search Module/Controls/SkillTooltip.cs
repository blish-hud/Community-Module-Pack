using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Gw2Sharp;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Universal_Search_Module.Controls {
    public class SkillTooltip : Tooltip {
        private const int MAX_WIDTH = 400;

        private readonly Skill _skill;

        public SkillTooltip(Skill skill) {
            _skill = skill;

            var skillTitle = new Label() {
                Text = _skill.Name,
                Font = Content.DefaultFont18,
                TextColor = ContentService.Colors.Chardonnay,
                AutoSizeHeight = true,
                AutoSizeWidth = true,
                Parent = this,
            };

            Label categoryText = null;
            var description = _skill.Description;

            if (_skill.Categories != null) {
                categoryText = new Label() {
                    Text = string.Join(", ", skill.Categories),
                    Font = Content.DefaultFont16,
                    AutoSizeHeight = true,
                    AutoSizeWidth = true,
                    Location = new Point(0, skillTitle.Bottom + 5),
                    TextColor = ContentService.Colors.ColonialWhite,
                    Parent = this,
                };

                description = description.Substring(description.IndexOf(".") + 1).Trim();
            }



            var skillDescription = new Label() {
                Text = description,
                Font = Content.DefaultFont16,
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Location = new Point(0, (categoryText == null ? skillTitle.Bottom : categoryText.Bottom) + 5),
                Parent = this,
            };

            LabelUtil.HandleMaxWidth(skillDescription, MAX_WIDTH);

            Control lastFact = skillDescription;
            Control lastTopRightCornerControl = null;
            if (_skill.Facts != null) {
                SkillFactRecharge rechargeFact = null;
                foreach (var fact in _skill.Facts) {
                    switch (fact) {
                        case SkillFactRecharge skillFactRecharge:
                            rechargeFact = skillFactRecharge;
                            break;
                        default:
                            lastFact = CreateFact(fact, lastFact);
                            break;
                    }
                }

                if (rechargeFact != null) {
                    lastTopRightCornerControl = CreateRechargeFact(rechargeFact);
                }
            }

            if (skill.Professions.Contains("Revenant") && skill.Cost != null) {
                lastTopRightCornerControl = CreateEnergyDisplay(lastTopRightCornerControl);
            }

            if (skill.Professions.Contains("Thief") && skill.Initiative != null) {
                lastTopRightCornerControl = CreateInitiativeDisplay(lastTopRightCornerControl);
            }

            if(skill.Flags.ToArray().FirstOrDefault(x => x.ToEnum() == SkillFlag.NoUnderwater) != null) { 
                lastTopRightCornerControl = CreateNonUnderwaterDisplay(lastTopRightCornerControl);
            }
        }

        private Control CreateFact(SkillFact fact, Control lastFact) {
            // Skip Damage fact bc calculation of the actual damage value is rather complicated
            if (fact is SkillFactDamage) {
                return lastFact;
            }

            var icon = fact.Icon;

            if (fact is SkillFactPrefixedBuff prefixedBuff) {
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

            LabelUtil.HandleMaxWidth(
                factDescription, 
                MAX_WIDTH, 
                offset: factImage.Width,
                afterRecalculate: () => {
                    factDescription.AutoSizeHeight = true;
                    factDescription.RecalculateLayout();
                    factImage.Location = new Point(0, factDescription.Location.Y + ((factDescription.Height / 2) - (factImage.Height / 2)));
                });

            return factDescription;
        }

        private string GetTextForFact(SkillFact fact) {
            switch (fact) {
                case SkillFactAttributeAdjust attributeAdjust:
                    return $"{attributeAdjust.Text}: {attributeAdjust.Value}";
                case SkillFactBuff buff:
                    var applyCountText = buff.ApplyCount != null && buff.ApplyCount != 1 ? buff.ApplyCount + "x " : string.Empty;
                    var durationText = buff.Duration != 0 ? $" ({buff.Duration}s) " : string.Empty;
                    return $"{applyCountText}{buff.Status}{durationText}: {buff.Description}";
                case SkillFactComboField comboField:
                    return $"{comboField.Text}: {comboField.FieldType.ToEnumString()}";
                case SkillFactComboFinisher comboFinisher:
                    return $"{comboFinisher.Text}: {comboFinisher.Type} ({comboFinisher.Percent}% {Strings.Common.SkillTooltip_Chance})";
                case SkillFactDamage damage: // Skip
                    return $"{damage.Text}({damage.HitCount}x): {damage.Text}";
                case SkillFactDistance distance:
                    return $"{distance.Text}: {distance.Distance}";
                case SkillFactDuration duration:
                    return $"{duration.Text}: {duration.Duration}s";
                case SkillFactHeal heal:
                    return $"{heal.HitCount}x {heal.Text}";
                case SkillFactHealingAdjust healingAdjust:
                    return $"{healingAdjust.HitCount}x {healingAdjust.Text}";
                case SkillFactNoData skillFactNoData:
                    return Strings.Common.SkillTooltip_CombatOnly;
                case SkillFactNumber skillFactNumber:
                    return $"{skillFactNumber.Text}: {skillFactNumber.Value}";
                case SkillFactPercent skillFactPercent:
                    return $"{skillFactPercent.Text}: {skillFactPercent.Percent}%";
                case SkillFactPrefixedBuff skillFactPrefixedBuff:
                    return $"{skillFactPrefixedBuff.ApplyCount}x {skillFactPrefixedBuff.Status} ({skillFactPrefixedBuff.Duration}s): {skillFactPrefixedBuff.Description}";
                case SkillFactRadius skillFactRadius:
                    return $"{skillFactRadius.Text}: {skillFactRadius.Distance}";
                case SkillFactRange skillFactRange:
                    return $"{skillFactRange.Text}: {skillFactRange.Value}";
                case SkillFactStunBreak stunBreak:
                    return Strings.Common.SkillTooltip_BreaksStun;
                case SkillFactTime skillFactTime:
                    return $"{skillFactTime.Text}: {skillFactTime.Duration}s";
                case SkillFactUnblockable skillFactUnblockable:
                default:
                    return fact.Text;
            }
        }

        private Control CreateRechargeFact(SkillFactRecharge skillFactRecharge) {
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

            return cooldownText;
        }

        private Control CreateInitiativeDisplay(Control lastControl) {
            var initiativeImage = new Image() {
                Texture = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156649.png"),
                Visible = true,
                Size = new Point(16, 16),
                Parent = this,
            };

            if (lastControl == null) {
                initiativeImage.Location = new Point(Width - initiativeImage.Width, 1);
            } else {
                initiativeImage.Location = new Point(lastControl.Left - initiativeImage.Width - 5, 0);
            }

            var initiativeText = new Label() {
                Text = _skill.Initiative.ToString(),
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = this,
            };
            initiativeText.Location = new Point(initiativeImage.Left - initiativeText.Width - 2, 0);
            return initiativeText;
        }

        private Control CreateEnergyDisplay(Control lastControl) {
            var energyImage = new Image() {
                Texture = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156647.png"),
                Visible = true,
                Size = new Point(16, 16),
                Parent = this,
            };

            if (lastControl == null) {
                energyImage.Location = new Point(Width - energyImage.Width, 1);
            } else {
                energyImage.Location = new Point(lastControl.Left - energyImage.Width - 5, 0);
            }

            var energyText = new Label() {
                Text = _skill.Cost.ToString(),
                AutoSizeWidth = true,
                AutoSizeHeight = true,
                Parent = this,
            };
            energyText.Location = new Point(energyImage.Left - energyText.Width - 2, 0);
            return energyText;
        }

        private Control CreateNonUnderwaterDisplay(Control lastControl) {
            var underwaterImage = new Image() {
                Texture = UniversalSearchModule.ModuleInstance.ContentsManager.GetTexture(@"textures\358417.png"),
                Visible = true,
                Size = new Point(16, 16),
                Parent = this,
            };

            if (lastControl == null) {
                underwaterImage.Location = new Point(Width - underwaterImage.Width, 1);
            } else {
                underwaterImage.Location = new Point(lastControl.Left - underwaterImage.Width - 5, 0);
            }

            return underwaterImage;
        }
    }
}
