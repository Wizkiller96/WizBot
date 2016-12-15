﻿using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WizBot.Modules.Music.Classes
{
    public class SoundCloud
    {
        private static readonly SoundCloud _instance = new SoundCloud();
        public static SoundCloud Default => _instance;

        static SoundCloud() { }
        public SoundCloud() { }

        public async Task<SoundCloudVideo> ResolveVideoAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(WizBot.Credentials.SoundCloudClientId))
                throw new ArgumentNullException(nameof(WizBot.Credentials.SoundCloudClientId));

            string response = "";

            using (var http = new HttpClient())
            {
                response = await http.GetStringAsync($"http://api.soundcloud.com/resolve?url={url}&client_id={WizBot.Credentials.SoundCloudClientId}").ConfigureAwait(false);

            }
                

            var responseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<SoundCloudVideo>(response);
            if (responseObj?.Kind != "track")
                throw new InvalidOperationException("Url is either not a track, or it doesn't exist.");

            return responseObj;
        }

        public bool IsSoundCloudLink(string url) =>
            System.Text.RegularExpressions.Regex.IsMatch(url, "(.*)(soundcloud.com|snd.sc)(.*)");

        public async Task<SoundCloudVideo> GetVideoByQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            if (string.IsNullOrWhiteSpace(WizBot.Credentials.SoundCloudClientId))
                throw new ArgumentNullException(nameof(WizBot.Credentials.SoundCloudClientId));

            var response = "";
            using (var http = new HttpClient())
            {
                response = await http.GetStringAsync($"http://api.soundcloud.com/tracks?q={Uri.EscapeDataString(query)}&client_id={WizBot.Credentials.SoundCloudClientId}").ConfigureAwait(false);
            }

            var responseObj = JsonConvert.DeserializeObject<SoundCloudVideo[]>(response).Where(s => s.Streamable).FirstOrDefault();
            if (responseObj?.Kind != "track")
                throw new InvalidOperationException("Query yielded no results.");

            return responseObj;
        }
    }

    public class SoundCloudVideo
    {
        public string Kind { get; set; } = "";
        public long Id { get; set; } = 0;
        public SoundCloudUser User { get; set; } = new SoundCloudUser();
        public string Title { get; set; } = "";
        [JsonIgnore]
        public string FullName => User.Name + " - " + Title;
        public bool Streamable { get; set; } = false;
        public int Duration { get; set; }
        [JsonProperty("permalink_url")]
        public string TrackLink { get; set; } = "";
        public string artwork_url { get; set; } = "";
        [JsonIgnore]
        public string StreamLink => $"https://api.soundcloud.com/tracks/{Id}/stream?client_id={WizBot.Credentials.SoundCloudClientId}";
    }
    public class SoundCloudUser
    {
        [Newtonsoft.Json.JsonProperty("username")]
        public string Name { get; set; }
    }
    /*
    {"kind":"track",
    "id":238888167,
    "created_at":"2015/12/24 01:04:52 +0000",
    "user_id":43141975,
    "duration":120852,
    "commentable":true,
    "state":"finished",
    "original_content_size":4834829,
    "last_modified":"2015/12/24 01:17:59 +0000",
    "sharing":"public",
    "tag_list":"Funky",
    "permalink":"18-fd",
    "streamable":true,
    "embeddable_by":"all",
    "downloadable":false,
    "purchase_url":null,
    "label_id":null,
    "purchase_title":null,
    "genre":"Disco",
    "title":"18 Ж",
    "description":"",
    "label_name":null,
    "release":null,
    "track_type":null,
    "key_signature":null,
    "isrc":null,
    "video_url":null,
    "bpm":null,
    "release_year":null,
    "release_month":null,
    "release_day":null,
    "original_format":"mp3",
    "license":"all-rights-reserved",
    "uri":"https://api.soundcloud.com/tracks/238888167",
    "user":{
        "id":43141975,
        "kind":"user",
        "permalink":"mrb00gi",
        "username":"Mrb00gi",
        "last_modified":"2015/12/01 16:06:57 +0000",
        "uri":"https://api.soundcloud.com/users/43141975",
        "permalink_url":"http://soundcloud.com/mrb00gi",
        "avatar_url":"https://a1.sndcdn.com/images/default_avatar_large.png"
        },
    "permalink_url":"http://soundcloud.com/mrb00gi/18-fd",
    "artwork_url":null,
    "waveform_url":"https://w1.sndcdn.com/gsdLfvEW1cUK_m.png",
    "stream_url":"https://api.soundcloud.com/tracks/238888167/stream",
    "playback_count":7,
    "download_count":0,
    "favoritings_count":1,
    "comment_count":0,
    "attachments_uri":"https://api.soundcloud.com/tracks/238888167/attachments"}

    */

}
