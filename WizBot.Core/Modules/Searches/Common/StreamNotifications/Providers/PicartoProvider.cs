using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WizBot.Core.Services.Database.Models;
using Newtonsoft.Json;
using NLog;

#nullable enable
namespace WizBot.Core.Modules.Searches.Common.StreamNotifications.Providers
{
    public class PicartoProvider : Provider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Logger _log;

        private static Regex Regex { get; } = new Regex(@"picarto.tv/(?<name>.+[^/])/?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override FollowedStream.FType Platform => FollowedStream.FType.Picarto;

        public PicartoProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _log = LogManager.GetCurrentClassLogger();
        }

        public override Task<bool> IsValidUrl(string url)
        {
            var match = Regex.Match(url);
            if (!match.Success)
                return Task.FromResult(false);

            // var username = match.Groups["name"].Value;
            return Task.FromResult(true);
        }

        public override Task<StreamData?> GetStreamDataByUrlAsync(string url)
        {
            var match = Regex.Match(url);
            if (match.Success)
            {
                var name = match.Groups["name"].Value;
                return GetStreamDataAsync(name);
            }

            return Task.FromResult<StreamData?>(null);
        }

        public override async Task<StreamData?> GetStreamDataAsync(string id)
        {
            var data = await GetStreamDataAsync(new List<string> { id });

            return data.FirstOrDefault();
        }

        public async override Task<List<StreamData>> GetStreamDataAsync(List<string> logins)
        {
            if (logins.Count == 0)
                return new List<StreamData>();

            using (var http = _httpClientFactory.CreateClient())
            {
                var toReturn = new List<StreamData>();
                foreach (var login in logins)
                {
                    try
                    {
                        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        // get id based on the username
                        var res = await http.GetAsync($"https://api.picarto.tv/v1/channel/name/{login}");

                        if (!res.IsSuccessStatusCode)
                            continue;

                        var userData = JsonConvert.DeserializeObject<PicartoChannelResponse>(await res.Content.ReadAsStringAsync());

                        toReturn.Add(ToStreamData(userData));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Something went wrong retreiving {Platform} streams.");
                        _log.Warn(ex.ToString());
                        return new List<StreamData>();
                    }
                }

                return toReturn;
            }
        }

        private StreamData ToStreamData(PicartoChannelResponse stream)
        {
            return new StreamData()
            {
                StreamType = FollowedStream.FType.Twitch,
                Name = stream.Name,
                UniqueName = stream.Name,
                Viewers = stream.Viewers,
                Title = stream.Title,
                IsLive = stream.Online,
                Preview = stream.Thumbnails.Web,
                Game = stream.Category,
                StreamUrl = $"https://picarto.tv/{stream.Name}",
                AvatarUrl = stream.Avatar
            };
        }
    }
}
