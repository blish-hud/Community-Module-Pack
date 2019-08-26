using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Flurl.Http;
using Microsoft.Xna.Framework.Graphics;
using File = Gw2Sharp.WebApi.V2.Models.File;

namespace Universal_Search_Module {
    public static class WebImgUtil {

        public static async Task<Texture2D> RequestTextureAsync(string textureUrl, CancellationToken cancellationToken = default) {
            var resultTexture = ContentService.Textures.Error;

            try {
                byte[] textureData = await textureUrl.GetBytesAsync(cancellationToken);

                using (var textureStream = new MemoryStream(textureData)) {
                    resultTexture = Texture2D.FromStream(GameService.Graphics.GraphicsDevice, textureStream);
                }
            } catch (Exception ex) {
                // TODO: Log exception
            }

            return resultTexture;
        }

    }
}
