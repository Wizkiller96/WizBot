﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WizBot.Modules.Nsfw.Common
{
    public abstract class ImageDownloader<T> : IImageDownloader
        where T : IImageData
    {
        protected readonly HttpClient _http;

        protected JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString,
            
        };
        
        public Booru Booru { get; }

        public ImageDownloader(Booru booru, HttpClient http)
        {
            _http = http;
            this.Booru = booru;
        }

        public abstract Task<List<T>> DownloadImagesAsync(string[] tags, int page, bool isExplicit = false, CancellationToken cancel = default);

        public async Task<List<ImageData>> DownloadImageDataAsync(string[] tags, int page, bool isExplicit = false,
            CancellationToken cancel = default)
        {
            var images = await DownloadImagesAsync(tags, page, isExplicit, cancel).ConfigureAwait(false);
            return images.Select(x => x.ToCachedImageData(Booru)).ToList();
        }
    }
}