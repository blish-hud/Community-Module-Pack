using Gw2Sharp.WebApi.V2.Models;

namespace Discord_Rich_Presence_Module
{
    public static class MapExtensions
    {
        /// <summary>
        /// Gets a hash of the map's continent rectangle which can be used to identify copies of the same map.
        /// </summary>
        /// <param name="map">The map to get the hash of.</param>
        /// <returns>
        /// The first 8 characters of the SHA1 hash of the following string<br/>
        /// <c>SHA1(&lt;continent_id&gt;&lt;continent_rect[0][0]&gt;&lt;continent_rect[0][1]&gt;&lt;continent_rect[1][0]&gt;&lt;continent_rect[1][1]&gt;)</c>
        /// </returns>
        public static string GetHash(this Map map)
        {
            var rpcHash = $"{map.ContinentId}{map.ContinentRect.TopLeft.X}{map.ContinentRect.TopLeft.Y}{map.ContinentRect.BottomRight.X}{map.ContinentRect.BottomRight.Y}";
            return rpcHash.ToSHA1Hash().Substring(0, 8);
        }
    }
}
