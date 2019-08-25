using System;
using Blish_HUD;
using Blish_HUD.Controls.Intern;
using Musician_Module.Player.Sound;
namespace Musician_Module.Controls.Instrument
{
    public class LutePreview : IInstrumentPreview
    {
        private LuteNote.Octaves _octave = LuteNote.Octaves.Middle;

        private readonly LuteSoundRepository _soundRepository = new LuteSoundRepository();

        public void PlaySoundByKey(GuildWarsControls key)
        {
            switch (key)
            {
                case GuildWarsControls.WeaponSkill1:
                case GuildWarsControls.WeaponSkill2:
                case GuildWarsControls.WeaponSkill3:
                case GuildWarsControls.WeaponSkill4:
                case GuildWarsControls.WeaponSkill5:
                case GuildWarsControls.HealingSkill:
                case GuildWarsControls.UtilitySkill1:
                case GuildWarsControls.UtilitySkill2:
                    AudioPlaybackEngine.Instance.PlaySound(_soundRepository.Get(key, _octave));
                    break;
                case GuildWarsControls.UtilitySkill3:
                    DecreaseOctave();
                    break;
                case GuildWarsControls.EliteSkill:
                    IncreaseOctave();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void IncreaseOctave()
        {
            switch (_octave)
            {
                case LuteNote.Octaves.None:
                    break;
                case LuteNote.Octaves.Low:
                    _octave = LuteNote.Octaves.Middle;
                    break;
                case LuteNote.Octaves.Middle:
                    _octave = LuteNote.Octaves.High;
                    break;
                case LuteNote.Octaves.High:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DecreaseOctave()
        {
            switch (_octave)
            {
                case LuteNote.Octaves.None:
                    break;
                case LuteNote.Octaves.Low:
                    break;
                case LuteNote.Octaves.Middle:
                    _octave = LuteNote.Octaves.Low;
                    break;
                case LuteNote.Octaves.High:
                    _octave = LuteNote.Octaves.Middle;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}