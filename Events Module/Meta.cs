using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Modules.Managers;
using Events_Module.Properties;
using Humanizer;
using Newtonsoft.Json;

namespace Events_Module {

    [JsonObject]
    public class Meta {

        private static readonly Logger Logger = Logger.GetLogger<Meta>();

        [JsonObject]
        public struct Phase {
            public string Name     { get; set; }
            public int    Duration { get; set; }
        }

        public event EventHandler<EventArgs> OnNextRunTimeChanged;

        public static List<Meta> Events;

        public string   Name       { get; set; }
        public string   Colloquial { get; set; }
        public string   Category   { get; set; }
        public DateTime Offset     { get; set; }
        public string   Difficulty { get; set; }
        public string   Location   { get; set; }
        public string   Waypoint   { get; set; }

        public string Wiki
        {
            get
            {
                var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                return _wikiLinks?.ContainsKey(lang) == true ? _wikiLinks[lang] : _wikiEn;
            }
            set => _wikiEn = value;
        }

        public int?     Duration   { get; set; }

        [JsonProperty(PropertyName = "Alert")]
        public int? Reminder { get; set; }

        [JsonProperty(PropertyName = "Repeat")]
        public TimeSpan? RepeatInterval { get; set; }

        protected List<DateTime>          _times = new List<DateTime>();
        public    IReadOnlyList<DateTime> Times => _times;

        public Phase[] Phases { get; set; }

        private DateTime _nextTime;
        public DateTime NextTime {
            get => _nextTime;
            protected set {
                if (_nextTime == value) return;

                _nextTime = value;

                this.OnNextRunTimeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [JsonIgnore]
        public bool IsWatched = false;

        [JsonIgnore]
        protected bool HasAlerted = false;

        private string _icon;
        private string _wikiEn;
        private Dictionary<string, string> _wikiLinks;

        public string Icon {
            get => _icon;
            set {
                if (_icon == value) return;

                _icon = value;

                if (!string.IsNullOrEmpty(_icon)) {
                    this.Texture = GameService.Content.GetRenderServiceTexture(_icon);
                }
            }
        }

        [JsonIgnore]
        public AsyncTexture2D Texture { get; private set; } = new AsyncTexture2D(GameService.Content.GetTexture(@"102377"));

        public static void UpdateEventSchedules() {
            if (Events == null) return;

            var tsNow = DateTime.Now.ToLocalTime().TimeOfDay;

            foreach (var e in Events) {
                TimeSpan[] justTimes = e.Times.Select(time => time.ToLocalTime().TimeOfDay).OrderBy(time => time.TotalSeconds).ToArray();
                var nextTime = justTimes.FirstOrDefault(ts => ts.TotalSeconds >= tsNow.TotalSeconds);

                if (nextTime.Ticks == 0) { // Timespan default is Ticks == 0
                    e.NextTime = DateTime.Today.AddDays(1) + justTimes[0];
                } else {
                    e.NextTime = DateTime.Today + nextTime;
                }

                double timeUntil = (e.NextTime - DateTime.Now).TotalMinutes;
                if (timeUntil < (e.Reminder ?? -1) && e.IsWatched) {
                    if (!e.HasAlerted && EventsModule.ModuleInstance.NotificationsEnabled) {
                        EventNotification.ShowNotification(
                            Resources.ResourceManager.GetString(e.Name) ?? e.Name,
                            e.Texture,
                            string.Format(Resources.Starts_in__0_, timeUntil.Minutes().Humanize()),
                            10f,
                            e.Waypoint
                        );
                        e.HasAlerted = true;
                    }
                } else {
                    e.HasAlerted = false;
                }
            }
        }

        public static async Task Load(ContentsManager cm) {
            List<Meta> metas = null;

            try {
                using (var eventsReader = new StreamReader(cm.GetFileStream(@"events.json"))) {
                    metas = JsonConvert.DeserializeObject<List<Meta>>(await eventsReader.ReadToEndAsync());
                }
            } catch (Exception e) {
                Logger.Error(e, Resources.Failed_to_load_metas_from_events_json_);
            }

            if (metas == null) {
                return;
            }

            var uniqueEvents = new List<Meta>();

            var wikiTasks = new List<Task>();

            foreach (var meta in metas) {
                meta._times.Add(meta.Offset);

                if (meta.RepeatInterval != null && meta.RepeatInterval.Value.TotalSeconds > 0) {
                    // Subtract the repeat interval to ensure that the start time isn't included twice
                    double dailyMinutes = 60 * 24 - meta.RepeatInterval.Value.TotalMinutes;
                    var lastTime = meta.Offset;

                    while (dailyMinutes > 0) {
                        var intervalTime = lastTime.Add(meta.RepeatInterval.Value);

                        meta._times.Add(intervalTime);

                        lastTime = intervalTime;

                        dailyMinutes -= meta.RepeatInterval.Value.TotalMinutes;
                    }
                }

                var rootEvent = uniqueEvents.Find(m => m.Name == meta.Name && m.Category == meta.Category);

                if (rootEvent != null) {
                    rootEvent._times.AddRange(meta.Times);
                } else {
                    uniqueEvents.Add(meta);
                }

                if (!string.IsNullOrEmpty(meta._wikiEn)) {
                    var pageEn = new Uri(meta._wikiEn).Segments.Last();
                    var task = GetInterwikiLinks(pageEn).ContinueWith(async v => meta._wikiLinks = await v);
                    wikiTasks.Add(task);
                }
            }

            await Task.WhenAll(wikiTasks.ToArray());

            Events = uniqueEvents;

            Logger.Info(@"Loaded {eventCount} events.", Events.Count);

            UpdateEventSchedules();
        }

        [Localizable(false)]
        private static async Task<Dictionary<string, string>> GetInterwikiLinks(string page) {
            var url = "https://wiki.guildwars2.com"
                     .AppendPathSegment("api.php")
                     .SetQueryParams(new {
                          action = "query",
                          format = "json",
                          prop = "langlinks",
                          titles = page,
                          redirects = 1,
                          converttitles = 1,
                          formatversion = 2,
                          llprop = "url"
                      });
            var json = await url.GetJsonAsync();
            var wikiPage = json.query.pages[0];
            if (((IDictionary<string, object>)wikiPage).ContainsKey("langlinks")) {
                var links = new Dictionary<string, string>();
                foreach (var link in wikiPage.langlinks) {
                    links.Add(link.lang, ((string)link.url).Replace("http://", "https://"));
                }
                return links;
            }
            
            return null;
        }
    }

}
