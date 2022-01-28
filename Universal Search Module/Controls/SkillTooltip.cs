using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;

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

            // Poor mans max width implementation
            if (skillDescription.Width > MAX_WIDTH) {
                skillDescription.AutoSizeWidth = false;
                skillDescription.Width = MAX_WIDTH;
                skillDescription.WrapText = true;
                skillDescription.RecalculateLayout();
            }

            Control lastFact = skillDescription;
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
                    CreateRechargeFact(rechargeFact);
                }
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

            // Poor mans max width solution and vertical alignment of an image
            if (factDescription.Width > MAX_WIDTH) {
                factDescription.AutoSizeWidth = false;
                factDescription.Width = MAX_WIDTH;
                factDescription.WrapText = true;
                factDescription.AutoSizeHeight = true;
                factDescription.RecalculateLayout();
                factImage.Location = new Point(0, factDescription.Location.Y + ((factDescription.Height / 2) - (factImage.Height / 2)));
            }

            return factDescription;
        }

        private string GetTextForFact(SkillFact fact) {
            switch (fact) {
                case SkillFactAttributeAdjust attributeAdjust:
                    return $"{attributeAdjust.Text}: {attributeAdjust.Value}";
                case SkillFactBuff buff:
                    var applyCountText = buff.ApplyCount != null ? buff.ApplyCount + "x " : string.Empty;
                    return $"{applyCountText}{buff.Status} ({buff.Duration}s): {buff.Description}";
                case SkillFactComboField comboField:
                    return $"{comboField.Text}: {comboField.FieldType.ToEnumString()}";
                case SkillFactComboFinisher comboFinisher:
                    return $"{comboFinisher.Text}: {comboFinisher.Type} ({comboFinisher.Percent} Chance)";
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
                case SkillFactNoData skillFactNoData: // TODO: Localization
                    return "Combat Only";
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
                case SkillFactStunBreak stunBreak: // TODO: Localization
                    return "Breaks Stun";
                case SkillFactTime skillFactTime:
                    return $"{skillFactTime.Text}: {skillFactTime.Duration}s";
                case SkillFactUnblockable skillFactUnblockable:
                default:
                    return fact.Text;
            }
        }

        private void CreateRechargeFact(SkillFactRecharge skillFactRecharge) {
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
