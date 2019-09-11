using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Loading_Screen_Hints_Module.Controls {
    public class LoadScreenPanel : Container {

        public const int TOP_PADDING = 20;
        public const int RIGHT_PADDING = 40;

        #region Load Static

        private static readonly Texture2D _textureBackgroundLoadScreenPanel;

        static LoadScreenPanel() {
            _textureBackgroundLoadScreenPanel = LoadingScreenHintsModule.ModuleInstance.ContentsManager.GetTexture("background_loadscreenpanel.png");
        }

        #endregion

        public Glide.Tween Fade { get; private set; }

        public Control LoadScreenTip;

        public LoadScreenPanel() {
            UpdateLocation(null, null);

            Graphics.SpriteScreen.Resized += UpdateLocation;
            Disposed += OnDisposed;

            LeftMouseButtonReleased += OnLeftMouseButtonReleased;
            RightMouseButtonReleased += OnRightMouseButtonReleased;
        }
        public void FadeOut() {

            if (Opacity != 1.0f) return;

            float duration = 2.0f;

            if (LoadScreenTip != null) {

                if (LoadScreenTip is GuessCharacter) {

                    GuessCharacter selected = (GuessCharacter)LoadScreenTip;
                    selected.Result = true;
                    duration = duration + 3.0f;

                } else if (LoadScreenTip is Narration) {

                    Narration selected = (Narration)LoadScreenTip;
                    duration = duration + selected.ReadingTime;

                } else if (LoadScreenTip is GamingTip) {

                    GamingTip selected = (GamingTip)LoadScreenTip;
                    duration = duration + selected.ReadingTime;

                }
            }

            Fade = Animation.Tweener.Tween(this, new { Opacity = 0.0f }, duration);
            Fade.OnComplete(() => {
                Dispose();
            });
        }
        private void OnLeftMouseButtonReleased(object sender, MouseEventArgs e) {
            if (Opacity != 1.0f) return;
            AnimationService.Animation.Tweener.Tween(this, new { Opacity = 0.0f }, 0.2f);
        }
        private void OnRightMouseButtonReleased(object sender, MouseEventArgs e) {
            if (Opacity != 0.0f) return;
            AnimationService.Animation.Tweener.Tween(this, new { Opacity = 1.0f }, 0.2f);
        }
        private void OnDisposed(object sender, EventArgs e)
        {
            if (LoadScreenTip != null)
            {
                if (LoadScreenTip is GuessCharacter)
                {
                    GuessCharacter selected = (GuessCharacter)LoadScreenTip;
                    selected.CharacterImage.Dispose();
                }
                LoadScreenTip.Dispose();
            }
        }

        private void UpdateLocation(object sender, EventArgs e) {
            this.Location = new Point((Graphics.SpriteScreen.Width / 2 - this.Width / 2), (Graphics.SpriteScreen.Height  / 2 - this.Height / 2) + 300);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this, _textureBackgroundLoadScreenPanel, bounds);
        }

    }

}
