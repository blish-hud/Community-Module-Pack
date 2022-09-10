using Flurl.Http;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Discord_Rich_Presence_Module
{
    public class TaskUtil
    {
        public static bool TryParseJson<T>(string json, out T result)
        {
            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (_, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error
            };
            result = JsonConvert.DeserializeObject<T>(json, settings);
            return success;
        }

        public static async Task<(bool, T)> GetJsonResponse<T>(string request, int timeOutSeconds = 10)
        {
            try
            {
                var rawJson = await request.AllowHttpStatus(HttpStatusCode.NotFound).AllowHttpStatus("200").WithTimeout(timeOutSeconds).GetStringAsync();
                return (TryParseJson<T>(rawJson, out var result), result);
            }
            catch (FlurlHttpTimeoutException ex)
            {
                DiscordRichPresenceModule.Logger.Warn(ex, $"Request '{request}' timed out.");
            }
            catch (FlurlHttpException ex)
            {
                DiscordRichPresenceModule.Logger.Warn(ex, $"Request '{request}' was not successful.");
            }
            catch (JsonReaderException ex)
            {
                DiscordRichPresenceModule.Logger.Warn(ex, $"Failed to deserialize requested content from \"{request}\"\n{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                DiscordRichPresenceModule.Logger.Error(ex, $"Unexpected error while requesting '{request}'.");
            }
            return (false, default);
        }
    }
}
