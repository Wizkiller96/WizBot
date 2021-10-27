﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WizBot.Modules.Nsfw.Common
{
    public abstract class DapiImageDownloader : ImageDownloader<DapiImageObject>
    {
        protected readonly string _baseUrl;

        public DapiImageDownloader(Booru booru, HttpClient http, string baseUrl) : base(booru, http)
        {
            _baseUrl = baseUrl;
        }

        public abstract Task<bool> IsTagValid(string tag, CancellationToken cancel = default);
        protected async Task<bool> AllTagsValid(string[] tags, CancellationToken cancel = default)
        {
            var results = await Task.WhenAll(tags.Select(tag => IsTagValid(tag, cancel)));

            // if any of the tags is not valid, the query is not valid
            foreach (var result in results)
            {
                if (!result)
                    return false;
            }

            return true;
        }

        public override async Task<List<DapiImageObject>> DownloadImagesAsync(string[] tags, int page,
            bool isExplicit = false, CancellationToken cancel = default)
        {
            // up to 2 tags allowed on danbooru
            if (tags.Length > 2)
                return new();

            if (!await AllTagsValid(tags, cancel).ConfigureAwait(false))
                return new();

            var tagString = ImageDownloaderHelper.GetTagString(tags, isExplicit);

            var uri = $"{_baseUrl}/posts.json?limit=200&tags={tagString}&page={page}";
            var imageObjects = await _http.GetFromJsonAsync<DapiImageObject[]>(uri, _serializerOptions, cancel)
                .ConfigureAwait(false);
            if (imageObjects is null)
                return new();
            return imageObjects
                .Where(x => x.FileUrl is not null)
                .ToList();
        }
    }
}