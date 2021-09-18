using Blish_HUD;
using Blish_HUD.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Compass_Module {
    public class CompassBillboard : IEntity {

        private static VertexPositionTexture[] _verts;
        private static BasicEffect _sharedEffect;

        static CompassBillboard() {
            _verts = new VertexPositionTexture[4];

            _verts[0] = new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 1));
            _verts[1] = new VertexPositionTexture(new Vector3(1, 0, 0), new Vector2(0, 1));
            _verts[2] = new VertexPositionTexture(new Vector3(0, 1, 0), new Vector2(1, 0));
            _verts[3] = new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(0, 0));

            _sharedEffect = new BasicEffect(GameService.Graphics.GraphicsDevice);
            _sharedEffect.TextureEnabled = true;
        }

        public float DrawOrder => Vector3.Distance(GetPosition(), GameService.Gw2Mumble.PlayerCharacter.Position);

        public Vector3 Offset { get; set; }
        public Texture2D Texture { get; set; }
        public float Opacity { get; set; } = 1f;
        public float Scale { get; set; }

        private Vector3 GetPosition() {
            return GameService.Gw2Mumble.PlayerCharacter.Position + this.Offset;
        }

        public CompassBillboard(Texture2D texture) {
            this.Texture = texture;
        }

        public void Update(GameTime gameTime) { /* NOOP */ }

        public void Render(GraphicsDevice graphicsDevice, IWorld world, ICamera camera) {
            _sharedEffect.View = GameService.Gw2Mumble.PlayerCamera.View;
            _sharedEffect.Projection = GameService.Gw2Mumble.PlayerCamera.Projection;
            _sharedEffect.World = Matrix.CreateScale(this.Scale, this.Scale, 1)
                                   * Matrix.CreateTranslation(new Vector3(this.Scale / -2, this.Scale / -2, 0))
                                   * Matrix.CreateBillboard(GetPosition(),
                                                            GameService.Gw2Mumble.PlayerCamera.Position,
                                                            new Vector3(0, 0, 1),
                                                            GameService.Gw2Mumble.PlayerCamera.Forward);

            _sharedEffect.Alpha = this.Opacity;
            _sharedEffect.Texture = this.Texture;

            foreach (var pass in _sharedEffect.CurrentTechnique.Passes) {
                pass.Apply();

                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, _verts, 0, 2);
            }
        }
    }
}
