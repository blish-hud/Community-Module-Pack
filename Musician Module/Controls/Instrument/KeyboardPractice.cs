using System;
using System.Collections.Generic;
using Blish_HUD.Controls.Intern;
namespace Musician_Module.Controls.Instrument
{
    public class KeyboardPractice : IKeyboard
    {

        private Conveyor Conveyor;
        public KeyboardPractice()
        {
            GameService.GameIntegration.FocusGw2();
            Conveyor = new Conveyor(){ Parent = ContentService.Graphics.SpriteScreen };
        }

        public void Press(GuildWarsControls key){}

        public void Release(GuildWarsControls key){}
    }
}